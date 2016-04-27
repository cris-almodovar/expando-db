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
        private readonly LightningDatabase _mainDatabase;
        private readonly BlockingCollection<WriteOperation> _writeOperationsQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _cancellationToken;
        private readonly ILog _log = LogManager.GetLogger(typeof(LightningStorageEngine).Name);
        public readonly string Path;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightningStorageEngine"/> class.
        /// </summary>
        /// <param name="dbPath">The database path.</param>
        public LightningStorageEngine(string dbPath)
        {
            Path = dbPath;

            var config = new EnvironmentConfiguration
            {
                MaxDatabases = MAX_DATABASES,               
                MapSize = MAX_MAP_SIZE,                
                MaxReaders = MAX_READERS
            };

            // NOTE: LightningEnvironment will auto-create 
            // the dbPath directory if it doesn't exist.
            _environment = new LightningEnvironment(dbPath, config);
            _environment.Open();

            _openDatabases = new ConcurrentDictionary<string, LightningDatabase>(); 
            using (var tx = _environment.BeginTransaction(TransactionBeginFlags.ReadOnly))
            {
               _mainDatabase = tx.OpenDatabase();
                tx.Commit();
            }         

            _writeOperationsQueue = new BlockingCollection<WriteOperation>();
            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;

            Task.Factory.StartNew(() => BackgroundWriter(), TaskCreationOptions.LongRunning);
        }

        private LightningDatabase OpenDatabase(LightningTransaction tx, string database, DatabaseConfiguration dbConfig = null)
        {
            if (!_openDatabases.ContainsKey(database))
            {
                var dbKey = database.ToByteArray();
                if (!tx.ContainsKey(_mainDatabase, dbKey) && dbConfig == null)
                    throw new InvalidOperationException($"Invalid LightningDB database: '{database}'");

                var db = tx.OpenDatabase(database, dbConfig);
                _openDatabases.TryAdd(database, db);
            }

            return _openDatabases[database];
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
            foreach (var operation in _writeOperationsQueue.GetConsumingEnumerable(_cancellationToken))
            {
                var tcs = operation.TaskCompletionSource;                
                try
                {
                    using (var tx = _environment.BeginTransaction())
                    {
                        try
                        {
                            var db = OpenDatabase(tx, operation.Database, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create });                                                            
                            var result = 0;

                            switch (operation.Type)
                            {
                                case WriteOperationType.Insert:
                                    var insertedCount = 0;
                                    foreach (var item in operation.KeyValuePairs)
                                    {
                                        if (tx.ContainsKey(db, item.Key))
                                            throw new InvalidOperationException("Duplicate key");

                                        tx.Put(db, item.Key, item.Value);
                                        insertedCount += 1;                                            
                                    }
                                    result = insertedCount;
                                    break;

                                case WriteOperationType.Update:
                                    var updatedCount = 0;
                                    foreach (var item in operation.KeyValuePairs)
                                    {
                                        if (!tx.ContainsKey(db, item.Key))
                                            throw new InvalidOperationException("Key does not exist in database.");

                                        tx.Put(db, item.Key, item.Value);
                                        updatedCount += 1;                                            
                                    }
                                    result = updatedCount;
                                    break;

                                case WriteOperationType.Delete:
                                    var deletedCount = 0;
                                    foreach (var item in operation.KeyValuePairs)
                                        if (tx.ContainsKey(db, item.Key))
                                        {
                                            tx.Delete(db, item.Key);
                                            deletedCount += 1;
                                        };

                                    result = deletedCount;
                                    break;

                                case WriteOperationType.DropDatabase:
                                    tx.DropDatabase(db);
                                    result = 1;
                                    break;

                                default:
                                    throw new InvalidOperationException($"Invalid Lightning DbOperationType: {operation.Type}");
                            }

                            tx.Commit();
                            tcs.SetResult(result);
                            
                        }
                        catch 
                        {                            
                            tx.Abort();
                            throw;
                        }
                        Thread.Sleep(10);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                    tcs.SetException(ex);
                }
            }
        }

        private ThreadLocal<LightningTransaction> _readonlyTransaction = new ThreadLocal<LightningTransaction>(true);
        public async Task<LightningKeyValuePair> GetAsync(string database, byte[] key)
        {
            var result = await Task.Run(() =>
            {
                var tx = GetReadonlyTransaction();
                var db = OpenDatabase(tx, database);

                var kv = new LightningKeyValuePair
                {
                    Key = key,
                    Value = tx.Get(db, key)
                };                

                if (tx.State == LightningTransactionState.Active)
                    tx.Reset();

                return kv;
            }
            ).ConfigureAwait(false);

            return result;        
        }

        public async Task<IEnumerable<LightningKeyValuePair>> GetAsync(string database, IEnumerable<byte[]> keys)
        {
            var result = await Task.Run(() =>
            {   
                var list = new List<LightningKeyValuePair>();
                var tx = GetReadonlyTransaction();

                if (DbExists(tx, database))
                {
                    var db = OpenDatabase(tx, database);

                    foreach (var key in keys)
                    {
                        var kv = new LightningKeyValuePair
                        {
                            Key = key,
                            Value = tx.Get(db, key)
                        };

                        list.Add(kv);
                    }

                }

                if (tx.State == LightningTransactionState.Active)
                    tx.Reset();

                return list;
            }
            ).ConfigureAwait(false);

            return result;
        }

        public async Task<IEnumerable<LightningKeyValuePair>> GetAllAsync(string database)
        {
            var result = await Task.Run(() =>
            {    
                var list = new List<LightningKeyValuePair>();
                var tx = GetReadonlyTransaction();

                if (DbExists(tx, database))
                {
                    var db = OpenDatabase(tx, database);
                    using (var cursor = tx.CreateCursor(db))
                    {
                        foreach (var item in cursor)
                        {
                            var kv = new LightningKeyValuePair { Key = item.Key, Value = item.Value };
                            list.Add(kv);
                        }
                    }
                }

                if (tx.State == LightningTransactionState.Active)
                    tx.Reset();

                return list;
            }
            ).ConfigureAwait(false);

            return result;
        }

        public async Task<bool> ExistsAsync(string database, byte[] key)
        {
            var result = await Task.Run(() =>
            { 
                var exists = false;

                var tx = GetReadonlyTransaction();
                if (DbExists(tx, database))
                {
                    var db = OpenDatabase(tx, database);
                    exists = tx.ContainsKey(db, key);
                }

                if (tx.State == LightningTransactionState.Active)
                    tx.Reset();

                return exists;
            }
            ).ConfigureAwait(false);

            return result;
        }

        private bool DbExists(LightningTransaction tx, string database)
        {
            bool dbExists = _openDatabases.ContainsKey(database);
            if (!dbExists)
            {
                var dbKey = database.ToByteArray();
                dbExists = tx.ContainsKey(_mainDatabase, dbKey);
            }  

            return dbExists;
        }

        private LightningTransaction GetReadonlyTransaction()
        {
            if (_readonlyTransaction.IsValueCreated)
            {
                if (_readonlyTransaction.Value.State == LightningTransactionState.Reseted)
                    _readonlyTransaction.Value.Renew();
                else
                {
                    _readonlyTransaction.Value.Dispose();
                    _readonlyTransaction.Value = _environment.BeginTransaction(TransactionBeginFlags.ReadOnly);
                }
            }
            else
            {
                _readonlyTransaction.Value = _environment.BeginTransaction(TransactionBeginFlags.ReadOnly);
            }

            return _readonlyTransaction.Value;
        }


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();

            foreach (var tx in _readonlyTransaction.Values)
                tx.Dispose();

            foreach (var db in _openDatabases.Values.Union(new[] { _mainDatabase }))
                db.Dispose();

            _readonlyTransaction.Dispose();            
            _environment.Dispose();
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
