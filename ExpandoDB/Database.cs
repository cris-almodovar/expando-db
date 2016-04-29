using Common.Logging;
using ExpandoDB.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ExpandoDB
{
    /// <summary>
    /// Represents a collection of <see cref="DocumentCollection"/> objects. 
    /// </summary>
    /// <remarks>
    /// This class is analogous to a MongoDB database.
    /// </remarks>
    public class Database : IDisposable
    {
        internal const string DATA_DIRECTORY_NAME = "data";
        internal const string DB_DIRECTORY_NAME = "db";
        internal const string INDEX_DIRECTORY_NAME = "index";
        private readonly string _dataPath;                
        private readonly string _indexPath;
        private readonly ConcurrentDictionary<string, DocumentCollection> _documentCollections;
        private readonly LightningStorageEngine _storageEngine;
        private readonly ISchemaStorage _schemaStorage;
        private readonly ConcurrentDictionary<string, DocumentCollectionSchema> _schemaCache;
        private readonly Timer _schemaPersistenceTimer;
        private readonly double _schemaPersistenceIntervalSeconds;
        private readonly object _schemaPersistenceLock = new object();
        private readonly ILog _log = LogManager.GetLogger(typeof(Database).Name);

        /// <summary>
        /// Initializes a new instance of the <see cref="Database" /> class.
        /// </summary>
        /// <param name="dataPath">The database directory path.</param>
        public Database(string dataPath = null)
        {
            if (String.IsNullOrWhiteSpace(dataPath))
            {
                var appPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                dataPath = Path.Combine(appPath, DATA_DIRECTORY_NAME);
            }

            _dataPath = dataPath;            
            EnsureDataDirectoryExists(_dataPath);

            _indexPath = Path.Combine(_dataPath, INDEX_DIRECTORY_NAME);
            EnsureIndexDirectoryExists(_indexPath);

            _storageEngine = new LightningStorageEngine(_dataPath);
            _schemaStorage = new LightningSchemaStorage(_storageEngine);
            _documentCollections = new ConcurrentDictionary<string, DocumentCollection>();            

            var schemas = _schemaStorage.GetAllAsync().Result.ToDictionary(cs => cs.Name);
            if (schemas != null && schemas.Count > 0)
                _schemaCache = new ConcurrentDictionary<string, DocumentCollectionSchema>(schemas);
            else
                _schemaCache = new ConcurrentDictionary<string, DocumentCollectionSchema>();

            foreach (var schema in _schemaCache.Values)
            {
                var collection = new DocumentCollection(schema, _storageEngine);
                _documentCollections.TryAdd(schema.Name, collection);
            }

            _schemaPersistenceIntervalSeconds = Double.Parse(ConfigurationManager.AppSettings["SchemaPersistenceIntervalSeconds"] ?? "1");
            _schemaPersistenceTimer = new Timer( o => PersistSchemas(), null, TimeSpan.FromSeconds(_schemaPersistenceIntervalSeconds*2), TimeSpan.FromSeconds(_schemaPersistenceIntervalSeconds));            
        }

        private void PersistSchemas()
        {
            // TODO: Don't use a Timer - use an async WaitHandle instead. Move schema persistence logic to DocumentCollection.

            Task.Run(
                async () =>
                {
                    if (Monitor.TryEnter(_schemaPersistenceLock, TimeSpan.FromMilliseconds(10)) == false)
                        return;
                    try
                    {
                        var newSchemasToInsert = _documentCollections.Keys.Except(_schemaCache.Keys);
                        foreach (var schemaName in newSchemasToInsert)
                        {
                            if (_documentCollections[schemaName] == null)
                                continue;

                            var newSchema = _documentCollections[schemaName].GetSchema();
                            _schemaCache.TryAdd(newSchema.Name, newSchema);
                            await _schemaStorage.InsertAsync(newSchema).ConfigureAwait(false);
                        }

                        var schemasToUpdate = from schemaName in _documentCollections.Keys.Intersect(_schemaCache.Keys)
                                              let persistedSchema = _schemaCache[schemaName]
                                              let liveSchema = _documentCollections[schemaName].GetSchema()
                                              where persistedSchema != liveSchema
                                              select schemaName;

                        foreach (var schemaName in schemasToUpdate)
                        {
                            if (_documentCollections[schemaName] == null)
                                continue;

                            var updatedSchema = _documentCollections[schemaName].GetSchema();
                            _schemaCache[schemaName] = updatedSchema;
                            await _schemaStorage.UpdateAsync(updatedSchema).ConfigureAwait(false);
                        }

                        var schemasToRemove = from schemaName in _schemaCache.Keys.Except(_documentCollections.Keys)
                                              select schemaName;

                        foreach (var schemaName in schemasToRemove)
                        {
                            DocumentCollectionSchema removedSchema = null;
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
           ).ConfigureAwait(false);            
        }

        /// <summary>
        /// Ensures the data directory exists.
        /// </summary>
        /// <param name="dataPath">The database path.</param>
        private static void EnsureDataDirectoryExists(string dataPath)
        {
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);            
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
        /// Gets the <see cref="DocumentCollection"/> with the specified name.
        /// </summary>
        /// <value>
        /// The <see cref="DocumentCollection"/> with the specified name.
        /// </value>
        /// <param name="name">The name of the DocumentCollection.</param>
        /// <returns></returns>
        public DocumentCollection this [string name]
        {
            get
            {
                if (String.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("name cannot be null or blank");

                DocumentCollection collection = null;
                if (!_documentCollections.TryGetValue(name, out collection))
                {
                    collection = new DocumentCollection(name, _storageEngine);
                    _documentCollections.TryAdd(name, collection);
                }

                return collection;
            }
        }

        /// <summary>
        /// Gets the names of all DocumentCollections in the Database.
        /// </summary>
        /// <value>
        /// The name of all DocumentCollections in the Database.
        /// </value>
        internal IEnumerable<string> GetCollectionNames()
        {
            return _documentCollections.Keys;
        }

        /// <summary>
        /// Drops the DocumentCollection with the specified name.
        /// </summary>
        /// <param name="collectionName">The name of the DocumentCollection to drop.</param>
        /// <returns></returns>
        public async Task<bool> DropCollectionAsync(string collectionName)
        {
            if (!_documentCollections.ContainsKey(collectionName))
                return false;            

            var isSuccessful = false;
            DocumentCollection collection = null;

            if (_documentCollections.TryRemove(collectionName, out collection))            
                isSuccessful = await collection.DropAsync().ConfigureAwait(false);

            if (isSuccessful)
                PersistSchemas();

            return isSuccessful;
        }

        /// <summary>
        /// Determines whether the Database contains a DocumentCollection with the specified name.
        /// </summary>
        /// <param name="collectionName">The name of the DocumentCollection.</param>
        /// <returns></returns>
        public bool ContainsCollection(string collectionName)
        {
            return _documentCollections.ContainsKey(collectionName);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>        
        public void Dispose()
        {
            foreach (var collection in _documentCollections.Values)            
                collection.Dispose();

            _storageEngine.Dispose();       
        }
    }
}
