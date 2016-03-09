using Common.Logging;
using ExpandoDB.Storage;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ExpandoDB
{
    /// <summary>
    /// Represents a collection of <see cref="ContentCollection"/> objects. 
    /// </summary>
    /// <remarks>
    /// This class is analogous to a MongoDB database.
    /// </remarks>
    public class Database : IDisposable
    {
        internal const string DB_FILENAME = "expando-db.s3db";
        internal const string DB_DIRECTORY_NAME = "db";
        internal const string INDEX_DIRECTORY_NAME = "index";

        private readonly string _dbPath;        
        private readonly string _dbFilePath;
        private readonly string _indexPath;
        private readonly ConcurrentDictionary<string, ContentCollection> _contentCollections;
        private readonly ISchemaStorage _schemaStorage;
        private readonly ConcurrentDictionary<string, ContentCollectionSchema> _schemaCache;
        private readonly Timer _schemaPersistenceTimer;
        private readonly double _schemaPersistenceIntervalSeconds;
        private readonly object _schemaPersistenceLock = new object();
        private readonly ILog _log = LogManager.GetLogger(typeof(Database).Name);

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="dbPath">The database directory path.</param>
        public Database(string dbPath = null)
        {
            if (String.IsNullOrWhiteSpace(dbPath))
            {
                var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                dbPath = Path.Combine(appPath, DB_DIRECTORY_NAME);
            }

            _dbPath = dbPath;
            _dbFilePath = Path.Combine(_dbPath, DB_FILENAME);
            EnsureDatabaseDirectoryExists(_dbPath, _dbFilePath);

            _indexPath = Path.Combine(_dbPath, INDEX_DIRECTORY_NAME);
            EnsureIndexDirectoryExists(_indexPath);

            _contentCollections = new ConcurrentDictionary<string, ContentCollection>();
            _schemaStorage = new SQLiteSchemaStorage(_dbFilePath);

            var schemas = _schemaStorage.GetAllAsync().Result.ToDictionary(cs => cs.Name);
            if (schemas != null && schemas.Count > 0)
                _schemaCache = new ConcurrentDictionary<string, ContentCollectionSchema>(schemas);
            else
                _schemaCache = new ConcurrentDictionary<string, ContentCollectionSchema>();

            foreach (var schema in _schemaCache.Values)
            {
                var collection = new ContentCollection(schema, _dbPath);
                _contentCollections.TryAdd(schema.Name, collection);
            }

            _schemaPersistenceIntervalSeconds = Double.Parse(ConfigurationManager.AppSettings["SchemaPersistenceIntervalSeconds"] ?? "1");
            _schemaPersistenceTimer = new Timer( async (o) => await PersistSchemas(), null, TimeSpan.FromSeconds(_schemaPersistenceIntervalSeconds), TimeSpan.FromSeconds(_schemaPersistenceIntervalSeconds));

        }

        private async Task PersistSchemas()
        {
            if (Monitor.TryEnter(_schemaPersistenceLock, TimeSpan.FromMilliseconds(10)) == false)
                return;

            try
            {
                var newSchemasToInsert = _contentCollections.Keys.Except(_schemaCache.Keys);
                foreach (var schemaName in newSchemasToInsert)
                {
                    if (_contentCollections[schemaName] == null)
                        continue;

                    var newSchema = _contentCollections[schemaName].GetSchema();
                    _schemaCache.TryAdd(newSchema.Name, newSchema);
                    await _schemaStorage.InsertAsync(newSchema).ConfigureAwait(false);
                }

                var schemasToUpdate = from schemaName in _contentCollections.Keys.Intersect(_schemaCache.Keys)
                                      let persistedSchema = _schemaCache[schemaName]
                                      let liveSchema = _contentCollections[schemaName].GetSchema()
                                      where persistedSchema != liveSchema                                      
                                      select schemaName;

                foreach (var schemaName in schemasToUpdate)
                {
                    if (_contentCollections[schemaName] == null)
                        continue;

                    var updatedSchema = _contentCollections[schemaName].GetSchema();
                    _schemaCache[schemaName] = updatedSchema;
                    await _schemaStorage.UpdateAsync(updatedSchema).ConfigureAwait(false);
                }

                var schemasToRemove = from schemaName in _schemaCache.Keys.Except(_contentCollections.Keys)                                      
                                      select schemaName;

                foreach (var schemaName in schemasToRemove)
                {
                    ContentCollectionSchema removedSchema = null;
                    _schemaCache.TryRemove(schemaName, out removedSchema);
                    await _schemaStorage.DeleteAsync(schemaName).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }     
            finally
            {
                Monitor.Exit(_schemaPersistenceLock);                
            }       
        }

        /// <summary>
        /// Ensures the SQLite database file exists.
        /// </summary>
        /// <param name="dbPath">The database path.</param>
        /// <param name="dbFilePath">The database file path.</param>
        private static void EnsureDatabaseDirectoryExists(string dbPath, string dbFilePath)
        {
            if (!Directory.Exists(dbPath))
                Directory.CreateDirectory(dbPath);

            if (!File.Exists(dbFilePath))
                SQLiteConnection.CreateFile(dbFilePath);
        }

        /// <summary>
        /// Ensures the Lucene index directory exists.
        /// </summary>
        private static void EnsureIndexDirectoryExists(string indexPath)
        {
            if (!Directory.Exists(indexPath))
                Directory.CreateDirectory(indexPath);
        }

        /// <summary>
        /// Gets the <see cref="ContentCollection"/> with the specified name.
        /// </summary>
        /// <value>
        /// The <see cref="ContentCollection"/> with the specified name.
        /// </value>
        /// <param name="name">The name of the ContentCollection.</param>
        /// <returns></returns>
        public ContentCollection this [string name]
        {
            get
            {
                if (String.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("name cannot be null or blank");

                ContentCollection collection = null;
                if (!_contentCollections.TryGetValue(name, out collection))
                {
                    collection = new ContentCollection(name, _dbPath);
                    _contentCollections.TryAdd(name, collection);
                }

                return collection;
            }
        }

        /// <summary>
        /// Drops the ContentCollection with the specified name.
        /// </summary>
        /// <param name="collectionName">The name of the ContentCollection to drop.</param>
        /// <returns></returns>
        public async Task<bool> DropCollectionAsync(string collectionName)
        {
            if (!_contentCollections.ContainsKey(collectionName))
                return false;

            var isSuccessful = false;
            ContentCollection collection = null;

            if (_contentCollections.TryRemove(collectionName, out collection))            
                isSuccessful = await collection.DropAsync().ConfigureAwait(false);

            if (isSuccessful)
                await PersistSchemas().ConfigureAwait(false);

            return isSuccessful;
        }

        /// <summary>
        /// Determines whether the Database contains a ContentCollection with the specified name.
        /// </summary>
        /// <param name="collectionName">The name of the ContentCollection.</param>
        /// <returns></returns>
        public bool ContainsCollection(string collectionName)
        {
            return _contentCollections.ContainsKey(collectionName);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>        
        public void Dispose()
        {
            foreach (var collection in _contentCollections.Values)            
                collection.Dispose();

            SQLiteConnection.ClearAllPools();
        }
    }
}
