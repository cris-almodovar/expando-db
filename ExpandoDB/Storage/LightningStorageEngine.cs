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
        private const long MAX_MAP_SIZE = 1000000000000;  // 1 terabyte
        private const int MAX_DATABASES = 100;  // 100 collections/tables; NOTE: Lightning database = RDBMS table
        private const int MAX_READERS = 512;
        private readonly LightningEnvironment _environment;
        private readonly ConcurrentDictionary<string, LightningDatabase> _openDatabases;        
        private readonly BlockingCollection<WriteOperation> _writeOperationsQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;
        private readonly ILog _log = LogManager.GetLogger(typeof(LightningStorageEngine).Name);
        public string DataPath { get; private set; }
        public string DbPath { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LightningStorageEngine"/> class.
        /// </summary>
        /// <param name="dataPath">The database path.</param>
        public LightningStorageEngine(string dataPath)
        {
            DataPath = dataPath;
            DbPath = Path.Combine(dataPath, Database.DB_DIRECTORY_NAME);            

            if (!Directory.Exists(DbPath))
                Directory.CreateDirectory(DbPath);

            var config = new EnvironmentConfiguration
            {
                MaxDatabases = MAX_DATABASES,               
                MapSize = MAX_MAP_SIZE,                
                MaxReaders = MAX_READERS                
            };            

            _environment = new LightningEnvironment(DbPath, config);

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
        /// Initializes the specified database.
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
        /// Inserts the Lightning key-value pairs into the database.
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
        /// Inserts the Lightning key-value pair into the database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="kv">The key-value pairs.</param>
        /// <returns></returns>
        public async Task<int> InsertAsync(string database, LightningKeyValuePair kv)
        {
            return await InsertAsync(database, new[] { kv }).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates specified Lightning key-value pairs.
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
        /// Updates specified Lightning key-value pair.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="kv">The key-value pair.</param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(string database, LightningKeyValuePair kv)
        {
            return await UpdateAsync(database, new[] { kv }).ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the Lightning key-value pairs identified by the given list of keys.
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
        /// Deletes the Lightning key-value pair identified by the given key.
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

        public async Task<LightningKeyValuePair> GetAsync(string database, byte[] key)
        {
            var result = await Task.FromResult(Get(database, key));
            return result;        
        }

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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
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
    }

    public class LightningKeyValuePair
    {
        public byte[] Key { get; set; }
        public byte[] Value { get; set; }
    }

    public class WriteOperation
    {
        public string Database { get; set; }
        public WriteOperationType Type { get; set; }
        public IEnumerable<LightningKeyValuePair> KeyValuePairs { get; set; }        
        public TaskCompletionSource<int> TaskCompletionSource { get; set; }
    }

    public enum WriteOperationType
    {        
        Insert,        
        Update,
        Delete,
        DropDatabase
    }
}
