using Common.Logging;
using Jil;
using LightningDB;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// Encapsulates the Lightning memory-mapped database engine.
    /// </summary>    
    public class LightningStorageEngine : IDisposable
    {
        private const long MAX_MAP_SIZE = 1000000000000;  // Limit on the size of the memory mapped file = 1TB.
        private const int MAX_DATABASES = 100;            // Limit on the number of named Ligtning databases. NOTE: In Lightning, a "database" is analogous to an RDBMS "table".
        private const int MAX_READERS = 32768;            // Limit on the number of reader threads. 
        private readonly LightningEnvironment _environment;
        private readonly ConcurrentDictionary<string, LightningDatabase> _openDatabases;        
        private readonly BlockingCollection<WriteOperation> _writeOperationsQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;
        private readonly ILog _log = LogManager.GetLogger(typeof(LightningStorageEngine).Name);
        
        /// <summary>
        /// Gets the path where data files are stored.
        /// </summary>
        /// <value>
        /// The data path.
        /// </value>
        public string DataPath { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightningStorageEngine"/> class.
        /// </summary>
        /// <param name="dataPath">The database path.</param>
        public LightningStorageEngine(string dataPath)
        {
            DataPath = dataPath;            

            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);

            _log.Info($"Data Path: {DataPath}");
            _log.Info($"Compression Option: {LightningStorageUtils._compressionOption}");

            var config = new EnvironmentConfiguration
            {
                MaxDatabases = MAX_DATABASES,               
                MapSize = MAX_MAP_SIZE,                
                MaxReaders = MAX_READERS                
            };
           
            _environment = new LightningEnvironment(DataPath, config);

            var openFlags = EnvironmentOpenFlags.WriteMap | 
                            EnvironmentOpenFlags.NoMetaSync | 
                            EnvironmentOpenFlags.MapAsync | 
                            EnvironmentOpenFlags.NoThreadLocalStorage;

            _environment.Open(openFlags); 

            _openDatabases = new ConcurrentDictionary<string, LightningDatabase>();                  
            _writeOperationsQueue = new BlockingCollection<WriteOperation>();
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            Task.Factory.StartNew(() => BackgroundWriter(), TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Initializes the specified database. NOTE: A LightningDB 'database' acts like an RDBMS 'table'.
        /// </summary>
        /// <param name="database">The database.</param>
        public void InitializeDatabase(string database)
        {
            if (_openDatabases.ContainsKey(database))
                return;

            using (var txn = _environment.BeginTransaction())
            {
                var dbConfig = new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create };
                var db = txn.OpenDatabase(database, dbConfig);
                _openDatabases.TryAdd(database, db);

                txn.Commit();
            }            
        }

        private LightningDatabase OpenDatabase(string database)
        { 
            if (_openDatabases.ContainsKey(database))
                return _openDatabases[database];
            else            
                throw new InvalidOperationException($"LightningDB database does not exist: '{database}'");              
        }

        /// <summary>
        /// Inserts the Lightning key-value pairs into the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="kvItems">The key-value pairs.</param>
        /// <returns></returns>
        public async Task<int> InsertAsync(string database, IEnumerable<LightningKeyValuePair> kvItems)
        {
            if (_cancellationToken.IsCancellationRequested)
                return 0;

            var operation = new WriteOperation
            {
                Type = WriteOperationType.Insert,
                Database = database,
                KeyValuePairs = kvItems,
                TaskCompletionSource = new TaskCompletionSource<int>()
            };

            if (!_writeOperationsQueue.IsCompleted)
                _writeOperationsQueue.Add(operation);
            else
                operation.TaskCompletionSource.TrySetCanceled();

            var result = await operation.TaskCompletionSource.Task.ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Inserts the Lightning key-value pair into the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="kv">The key-value pairs.</param>
        /// <returns></returns>
        public async Task<int> InsertAsync(string database, LightningKeyValuePair kv)
        {
            return await InsertAsync(database, new[] { kv }).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates specified Lightning key-value pairs, in the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="kvItems">The key-value pairs.</param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(string database, IEnumerable<LightningKeyValuePair> kvItems)
        {
            if (_cancellationToken.IsCancellationRequested)
                return 0;

            var operation = new WriteOperation
            {
                Type = WriteOperationType.Update,
                Database = database,
                KeyValuePairs = kvItems,
                TaskCompletionSource = new TaskCompletionSource<int>()
            };

            if (!_writeOperationsQueue.IsCompleted)
                _writeOperationsQueue.Add(operation);
            else
                operation.TaskCompletionSource.TrySetCanceled();

            var result = await operation.TaskCompletionSource.Task.ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Updates the specified Lightning key-value pair, in the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="kv">The key-value pair.</param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(string database, LightningKeyValuePair kv)
        {
            return await UpdateAsync(database, new[] { kv }).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the Lightning key-value pairs identified by the given set of keys, from the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="keys">The keys that uniquely identify the key-value pairs to be deleted.</param>
        /// <returns></returns>
        public async Task<int> DeleteAsync(string database, IEnumerable<byte[]> keys)
        {
            if (_cancellationToken.IsCancellationRequested)
                return 0;

            var operation = new WriteOperation
            {
                Type = WriteOperationType.Delete,
                Database = database,
                KeyValuePairs = keys.Select(k => new LightningKeyValuePair { Key = k }).ToList() ,
                TaskCompletionSource = new TaskCompletionSource<int>()
            };

            if (!_writeOperationsQueue.IsCompleted)
                _writeOperationsQueue.Add(operation);
            else
                operation.TaskCompletionSource.TrySetCanceled();

            var result = await operation.TaskCompletionSource.Task.ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Deletes the Lightning key-value pair identified by the given key, from the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="key">The key that uniquely identify the key-value pair to be deleted.</param>
        /// <returns></returns>
        public async Task<int> DeleteAsync(string database, byte[] key)
        {            
            return await DeleteAsync(database, new[] { key }).ConfigureAwait(false);
        }

        /// <summary>
        /// Drops the specified Lightning database.
        /// </summary>
        /// <param name="database">The Lightning database to drop.</param>
        /// <returns></returns>
        public async Task<int> DropAsync(string database)
        {
            if (_cancellationToken.IsCancellationRequested)
                return 0;

            var operation = new WriteOperation
            {
                Type = WriteOperationType.DropDatabase,
                Database = database,               
                TaskCompletionSource = new TaskCompletionSource<int>()
            };

            if (!_writeOperationsQueue.IsCompleted)
                _writeOperationsQueue.Add(operation);
            else
                operation.TaskCompletionSource.TrySetCanceled();
            
            var result = await operation.TaskCompletionSource.Task.ConfigureAwait(false);

            var tryCount = 0;
            while (tryCount < 3)
            {
                tryCount += 1;
                
                LightningDatabase db = null;
                _openDatabases.TryRemove(database, out db);

                await Task.Delay(TimeSpan.FromSeconds(0.5)).ConfigureAwait(false);

                if (!_openDatabases.ContainsKey(database))
                    break; // We are successful
            }

            if (_openDatabases.ContainsKey(database))
                throw new Exception($"Unable to remove the LightningDB database: {database}");

            return result;
        }

        /// <summary>
        /// Truncates the specified Lightning database.
        /// </summary>
        /// <param name="database">The Lightning database to truncate.</param>
        /// <returns></returns>
        public async Task<int> TruncateAsync(string database)
        {
            if (_cancellationToken.IsCancellationRequested)
                return 0;

            var operation = new WriteOperation
            {
                Type = WriteOperationType.TruncateDatabase,
                Database = database,
                TaskCompletionSource = new TaskCompletionSource<int>()
            };

            if (!_writeOperationsQueue.IsCompleted)
                _writeOperationsQueue.Add(operation);
            else
                operation.TaskCompletionSource.TrySetCanceled();

            var result = await operation.TaskCompletionSource.Task.ConfigureAwait(false);       
            return result;
        }

        private void BackgroundWriter()
        {
            try
            {
                foreach (var operation in _writeOperationsQueue.GetConsumingEnumerable(_cancellationToken))
                {
                    using (var txn = _environment.BeginTransaction())
                    {
                        try
                        {
                            var db = OpenDatabase(operation.Database);
                            var result = 0;

                            switch (operation.Type)
                            {
                                case WriteOperationType.Insert:
                                    var insertedCount = 0;
                                    using (var cur = txn.CreateCursor(db))
                                    {
                                        foreach (var kv in operation.KeyValuePairs)
                                        {
                                            cur.Put(kv.Key, kv.Value, CursorPutOptions.NoOverwrite);
                                            insertedCount += 1;
                                        }
                                    }
                                    result = insertedCount;
                                    break;

                                case WriteOperationType.Update:
                                    var updatedCount = 0;
                                    using (var cur = txn.CreateCursor(db))
                                    {
                                        foreach (var kv in operation.KeyValuePairs)
                                            if (cur.MoveTo(kv.Key))
                                            {
                                                cur.Put(kv.Key, kv.Value, CursorPutOptions.Current);
                                                updatedCount += 1;
                                            }
                                    }
                                    result = updatedCount;
                                    break;

                                case WriteOperationType.Delete:
                                    var deletedCount = 0;
                                    using (var cur = txn.CreateCursor(db))
                                    {
                                        foreach (var kv in operation.KeyValuePairs)
                                            if (cur.MoveTo(kv.Key))
                                            {
                                                cur.Delete();
                                                deletedCount += 1;
                                            }
                                    }
                                    result = deletedCount;
                                    break;

                                case WriteOperationType.DropDatabase:
                                    txn.DropDatabase(db);
                                    result = 1;
                                    break;

                                case WriteOperationType.TruncateDatabase:
                                    txn.TruncateDatabase(db);
                                    result = 1;
                                    break;

                                default:
                                    throw new InvalidOperationException($"Invalid Lightning DbOperationType: {operation.Type}");
                            }

                            txn.Commit();
                            operation.TaskCompletionSource.SetResult(result);

                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex);
                            txn.Abort();
                            operation.TaskCompletionSource.SetException(ex);
                        }

                    }
                }
            }
            catch (OperationCanceledException)
            {
                _log.Debug("The BackgroundWriter thread was canceled.");
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        
        private ThreadLocal<LightningTransaction> _readonlyTransaction = new ThreadLocal<LightningTransaction>(true);
        
        // Optimization - readonly transactions can remain open; we just need to reset and then renew it.
        private LightningTransaction GetReadonlyTransaction()
        {
            if (_readonlyTransaction.IsValueCreated)
            {
                if (_readonlyTransaction.Value.State == LightningTransactionState.Reseted)
                    _readonlyTransaction.Value.Renew();
                else
                {
                    if (_readonlyTransaction.Value.State != LightningTransactionState.Active)
                    {
                        _readonlyTransaction.Value.Dispose();
                        _readonlyTransaction.Value = _environment.BeginTransaction(TransactionBeginFlags.ReadOnly);
                    }
                }
            }
            else
            {
                _readonlyTransaction.Value = _environment.BeginTransaction(TransactionBeginFlags.ReadOnly);
            }

            return _readonlyTransaction.Value;
        }

        /// <summary>
        /// Gets the key-value pair identified by the specified key, from the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public async Task<LightningKeyValuePair> GetAsync(string database, byte[] key)
        {
            var result = await Task.FromResult(Get(database, key));
            return result;        
        }

        /// <summary>
        /// Gets the key-value pair identified by the specified key, from the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private LightningKeyValuePair Get(string database, byte[] key)
        {
            var txn = GetReadonlyTransaction();
            var db = OpenDatabase(database);
            var kv = new LightningKeyValuePair { Key = key };

            kv.Value = txn.Get(db, key);
            if (txn.State == LightningTransactionState.Active)
                txn.Reset();

            return kv;
        }

        /// <summary>
        /// Gets the key-value pairs identified by the specified set of keys, from the specified database. 
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="keys">The keys.</param>
        /// <returns></returns>
        public async Task<IEnumerable<LightningKeyValuePair>> GetAsync(string database, IEnumerable<byte[]> keys)
        {
            var result = await Task.FromResult(Get(database, keys));
            return result;
        }

        private IEnumerable<LightningKeyValuePair> Get(string database, IEnumerable<byte[]> keys)
        {
            var list = new List<LightningKeyValuePair>();
            var txn = GetReadonlyTransaction();

            var db = OpenDatabase(database);
            using (var cur = txn.CreateCursor(db))
            {
                foreach (var key in keys)
                {
                    if (cur.MoveTo(key))
                    {
                        var kv = new LightningKeyValuePair
                        {
                            Key = key,
                            Value = cur.GetCurrent().Value
                        };

                        list.Add(kv);
                    }
                }
            }

            if (txn.State == LightningTransactionState.Active)
                txn.Reset();

            return list;
        }

        /// <summary>
        /// Gets all the key-value pairs from the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <returns></returns>
        public async Task<IEnumerable<LightningKeyValuePair>> GetAllAsync(string database)
        {
            var result = await Task.FromResult(GetAll(database));
            return result;
        }

        private IEnumerable<LightningKeyValuePair> GetAll(string database)
        {
            var list = new List<LightningKeyValuePair>();
            var txn = GetReadonlyTransaction();

            var db = OpenDatabase(database);
            using (var cursor = txn.CreateCursor(db))
            {
                foreach (var item in cursor)
                {
                    var kv = new LightningKeyValuePair { Key = item.Key, Value = item.Value };
                    list.Add(kv);
                }
            }

            if (txn.State == LightningTransactionState.Active)
                txn.Reset();

            return list;
        }

        /// <summary>
        /// Determines if a key-value pair with the specified key exists in the specified database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public async Task<bool> ExistsAsync(string database, byte[] key)
        {
            var result = await Task.FromResult(Exists(database, key));
            return result;
        }

        private bool Exists(string database, byte[] key)
        {
            var exists = false;
            var txn = GetReadonlyTransaction();

            var db = OpenDatabase(database);
            exists = txn.ContainsKey(db, key);

            if (txn.State == LightningTransactionState.Active)
                txn.Reset();

            return exists;
        }

        #region IDisposable Support
        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _writeOperationsQueue.CompleteAdding();
                    _cancellationTokenSource.Cancel();

                    foreach (var txn in _readonlyTransaction.Values)
                        txn.Dispose();

                    foreach (var db in _openDatabases.Values)
                        db.Dispose();

                    _readonlyTransaction.Dispose();
                    _environment.Dispose();
                    _writeOperationsQueue.Dispose();
                    _cancellationTokenSource.Dispose();
                }

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {            
            Dispose(true);         
        }
        #endregion
    }

    /// <summary>
    /// A wrapper class for KeyValuePair whose key/value are both byte arrays.
    /// </summary>
    public class LightningKeyValuePair
    {
        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public byte[] Key { get; set; }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public byte[] Value { get; set; }
    }

    /// <summary>
    /// Represents a LightningDB write operation.
    /// </summary>
    public class WriteOperation
    {
        /// <summary>
        /// Gets or sets the database.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        public string Database { get; set; }
        /// <summary>
        /// Gets or sets the type of write operation.
        /// </summary>
        /// <value>
        /// The type of write operation.
        /// </value>
        public WriteOperationType Type { get; set; }
        /// <summary>
        /// Gets or sets the key value pairs.
        /// </summary>
        /// <value>
        /// The key value pairs.
        /// </value>
        public IEnumerable<LightningKeyValuePair> KeyValuePairs { get; set; }
        /// <summary>
        /// Gets or sets the task completion source.
        /// </summary>
        /// <value>
        /// The task completion source.
        /// </value>
        public TaskCompletionSource<int> TaskCompletionSource { get; set; }
    }

    /// <summary>
    /// Specifies the type of a LightningDB write operation.
    /// </summary>
    public enum WriteOperationType
    {
        /// <summary>
        /// The insert
        /// </summary>
        Insert,
        /// <summary>
        /// The update
        /// </summary>
        Update,
        /// <summary>
        /// The delete
        /// </summary>
        Delete,
        /// <summary>
        /// The drop database
        /// </summary>
        DropDatabase,
        /// <summary>
        /// The truncate database
        /// </summary>
        TruncateDatabase
    }
}
