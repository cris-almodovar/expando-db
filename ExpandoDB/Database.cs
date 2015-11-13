using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using ExpandoDB.Storage;

namespace ExpandoDB
{
    /// <summary>
    /// 
    /// </summary>
    public class Database : IDisposable
    {
        internal const string DB_FILENAME = "expando.db";
        internal const string DB_DIR_NAME = "db";
        internal const string INDEX_DIR_NAME = "index";

        private readonly string _dbPath;        
        private readonly string _dbFilePath;
        private readonly string _indexPath;
        private readonly ConcurrentDictionary<string, ContentCollection> _contentCollections;
        private readonly ISchemaStorage _schemaStorage;
        private readonly IDictionary<string, ContentCollectionSchema> _contentCollectionSchemas;
        private readonly Timer _schemaPersistenceTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="Database"/> class.
        /// </summary>
        /// <param name="dbPath">The database path.</param>
        public Database(string dbPath = null)
        {
            if (String.IsNullOrWhiteSpace(dbPath))
            {
                var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                dbPath = Path.Combine(appPath, DB_DIR_NAME);
            }

            _dbPath = dbPath;
            _dbFilePath = Path.Combine(_dbPath, DB_FILENAME);
            EnsureDatabaseExists(_dbPath, _dbFilePath);

            _indexPath = Path.Combine(_dbPath, INDEX_DIR_NAME);
            EnsureIndexDirectoryExists(_indexPath);

            _contentCollections = new ConcurrentDictionary<string, ContentCollection>();
            
            _schemaStorage = new SQLiteSchemaStorage(_dbFilePath);
            var savedSchemas = _schemaStorage.GetAllAsync().Result.ToDictionary(cs => cs.Name);
            if (savedSchemas == null)
                _contentCollectionSchemas = new Dictionary<string, ContentCollectionSchema>();
            else
                _contentCollectionSchemas = savedSchemas;

            foreach (var schema in _contentCollectionSchemas.Values)
            {
                var collection = new ContentCollection(schema.Name, _dbPath, schema.IndexSchema);
                _contentCollections.TryAdd(schema.Name, collection);
            }

            _schemaPersistenceTimer = new Timer( async (o) =>  await PersistSchema(), null, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));

        }

        private async Task PersistSchema()
        {
            if (Monitor.TryEnter(_contentCollections, TimeSpan.FromMilliseconds(10)) == false)
                return;

            try
            {
                foreach (var collectionName in _contentCollections.Keys)
                {
                    var schema = _contentCollections[collectionName].GetSchema();

                    if (!_contentCollectionSchemas.ContainsKey(schema.Name))
                    {
                        // Save the schema of a newly created ContentCollection
                        _contentCollectionSchemas.Add(schema.Name, schema);
                        await _schemaStorage.InsertAsync(schema);
                    }

                    var savedSchema = _contentCollectionSchemas[schema.Name];
                    if (savedSchema != schema)
                    {
                        _contentCollectionSchemas[savedSchema.Name] = schema;
                        await _schemaStorage.UpdateAsync(schema);
                    }
                }

                var schemasToRemove = from schemaName in _contentCollectionSchemas.Keys
                                      where !(_contentCollections.ContainsKey(schemaName))
                                      select schemaName;

                foreach (var schemaName in schemasToRemove)
                {
                    _contentCollectionSchemas.Remove(schemaName);
                    await _schemaStorage.DeleteAsync(schemaName);
                }
            }
            finally
            {
                Monitor.Exit(_contentCollections);
            }       
        }

        /// <summary>
        /// Ensures the SQLite database file exists.
        /// </summary>
        /// <param name="dbPath">The database path.</param>
        /// <param name="dbFilePath">The database file path.</param>
        private static void EnsureDatabaseExists(string dbPath, string dbFilePath)
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
                isSuccessful = await collection.DropAsync();

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
