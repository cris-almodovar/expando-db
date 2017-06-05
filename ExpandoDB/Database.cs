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
using ExpandoDB.Search;

namespace ExpandoDB
{
    /// <summary>
    /// Represents a collection of <see cref="Collection"/> objects. 
    /// </summary>
    /// <remarks>
    /// This class is analogous to a MongoDB database.
    /// </remarks>
    public class Database : IDisposable
    {
        internal const string DATA_DIRECTORY_NAME = "data";        
        internal const string INDEX_DIRECTORY_NAME = "index";    
        private readonly ConcurrentDictionary<string, Collection> _collections;
        private readonly Timer _schemaPersistenceTimer;          
        private readonly ILog _log = LogManager.GetLogger(typeof(Database).Name);

        /// <summary>
        /// Gets the path to the data files.
        /// </summary>
        /// <value>
        /// The data path.
        /// </value>
        public string DataPath { get; private set; }

        /// <summary>
        /// Gets the index base path; Collection-specific indexes are created as sub-directories under this path.
        /// </summary>
        /// <value>
        /// The index base path.
        /// </value>
        public string IndexBasePath { get; private set; }

        /// <summary>
        /// Gets the IDocumentStorage used by this instance.
        /// </summary>
        /// <value>
        /// The document storage.
        /// </value>
        public IDocumentStorage DocumentStorage { get; private set; }

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

            DataPath = dataPath;            
            EnsureDataDirectoryExists(DataPath);
            _log.Info($"Data Path: {DataPath}");

            IndexBasePath = Path.Combine(DataPath, INDEX_DIRECTORY_NAME);
            EnsureIndexDirectoryExists(IndexBasePath);
            _log.Info($"Index Base Path: {IndexBasePath}");
           
            DocumentStorage = new LightningDocumentStorage(DataPath);
            _collections = new ConcurrentDictionary<string, Collection>();

            var persistedSchemas = DocumentStorage.GetAllAsync(Schema.COLLECTION_NAME).Result.Select(d => d.ToSchema());
            foreach (var schema in persistedSchemas)
            {
                var collection = new Collection(schema, this);
                _collections.TryAdd(schema.Name, collection);
            }

            var schemaPersistenceIntervalSeconds = Double.Parse(ConfigurationManager.AppSettings["Schema.PersistenceIntervalSeconds"] ?? "1");
            _schemaPersistenceTimer = new Timer(_ => Task.Run(async () => await PersistSchemas().ConfigureAwait(false)), null, TimeSpan.FromSeconds(schemaPersistenceIntervalSeconds), TimeSpan.FromSeconds(schemaPersistenceIntervalSeconds));
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
        /// Gets the <see cref="Collection"/> with the specified name.
        /// </summary>
        /// <value>
        /// The <see cref="Collection"/> with the specified name.
        /// </value>
        /// <param name="name">The name of the Document Collection.</param>
        /// <returns></returns>
        public Collection this [string name]
        {
            get
            {
                if (String.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("name cannot be null or blank");

                Collection collection = null;
                if (!_collections.ContainsKey(name))
                {
                    lock (_collections)
                    {
                        if (!_collections.ContainsKey(name))
                        {
                            collection = new Collection(name, this);
                            _collections.TryAdd(name, collection);
                        }
                    }
                }

                _collections.TryGetValue(name, out collection);
                if (collection == null || collection.IsDropped)
                    throw new InvalidOperationException($"The Document Collection '{name}' does not exist.");

                return collection;
            }
        }

        /// <summary>
        /// Gets the names of all Document Collections in the Database.
        /// </summary>
        /// <value>
        /// The name of all Document Collections in the Database.
        /// </value>
        public IEnumerable<string> GetCollectionNames()
        {
            return _collections.Keys.ToList();
        }

        /// <summary>
        /// Drops the Document Collection with the specified name.
        /// </summary>
        /// <param name="collectionName">The name of the Document Collection to drop.</param>
        /// <returns></returns>
        public async Task<bool> DropCollectionAsync(string collectionName)
        {
            if (!_collections.ContainsKey(collectionName))
                return false;

            if (collectionName == Schema.COLLECTION_NAME)
                throw new InvalidOperationException($"Cannot drop the {Schema.COLLECTION_NAME} collection.");

            var isSuccessful = false;
            Collection collection = null;
            
            lock (_collections)
            {
                if (_collections.ContainsKey(collectionName))             
                    _collections.TryRemove(collectionName, out collection); 
            }

            if (collection == null)
                return false;            

            isSuccessful = await collection.DropAsync().ConfigureAwait(false);
            if (isSuccessful)
            {
                var schemaId = collection.Schema._id ?? Guid.Empty;
                if (schemaId != Guid.Empty)
                    isSuccessful = await this[Schema.COLLECTION_NAME].DeleteAsync(schemaId).ConfigureAwait(false) == 1;
            }

            return isSuccessful;
        }

        /// <summary>
        /// Determines whether the Database contains a Document Collection with the specified name.
        /// </summary>
        /// <param name="collectionName">The name of the Document Collection.</param>
        /// <returns></returns>
        public bool ContainsCollection(string collectionName)
        {
            return _collections.ContainsKey(collectionName);
        }
        
        private readonly SemaphoreSlim _persistSchemasLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Persists Schema objects to a special _schemas collection.
        /// </summary>
        /// <returns></returns>
        private async Task PersistSchemas()
        {            
            if (await _persistSchemasLock.WaitAsync(500))
            {
                try
                {
                    foreach (var collectionName in GetCollectionNames())
                    {
                        var collection = this[collectionName];
                        if (collection.IsDropped || collection.IsDisposed)
                            continue;

                        await PersistSchema(collection).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
                finally
                {
                    _persistSchemasLock.Release();
                }
            }
        }

        /// <summary>
        /// Persists the Schema of the specified Collection to a special _schemas Collection.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns></returns>
        private async Task PersistSchema(Collection collection)
        {
            try
            {                
                var liveSchemaDocument = collection.Schema.ToDocument();                
                var savedSchemaDocument = await DocumentStorage.GetAsync(Schema.COLLECTION_NAME, liveSchemaDocument._id.Value);

                var isSchemaUpdated = false;
                if (savedSchemaDocument == null)
                {
                    await DocumentStorage.InsertAsync(Schema.COLLECTION_NAME, liveSchemaDocument);
                    isSchemaUpdated = true;         
                }
                else
                {
                    if (liveSchemaDocument != savedSchemaDocument)
                    {
                        await DocumentStorage.UpdateAsync(Schema.COLLECTION_NAME, liveSchemaDocument);
                        isSchemaUpdated = true;
                    }
                }

                if (isSchemaUpdated)
                {
                    savedSchemaDocument = await DocumentStorage.GetAsync(Schema.COLLECTION_NAME, liveSchemaDocument._id.Value);
                    collection.Schema._id = savedSchemaDocument._id;
                    collection.Schema._createdTimestamp = savedSchemaDocument._createdTimestamp;
                    collection.Schema._modifiedTimestamp = savedSchemaDocument._modifiedTimestamp;
                }

            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
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
                    _schemaPersistenceTimer.Dispose();
                    _collections.Values.ToList().ForEach(item => item.Dispose());
                    
                    DocumentStorage.Dispose();
                }

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Closes the database and deallocates resources.
        /// </summary>
        public void Dispose()
        {            
            Dispose(true);         
        }

        #endregion
    }   
}
