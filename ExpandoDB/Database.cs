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
        internal const string DB_DIRECTORY_NAME = "db";
        internal const string INDEX_DIRECTORY_NAME = "index";
        private readonly string _dataPath;                
        private readonly string _indexPath;
        private readonly IDictionary<string, Collection> _collections;       
        private readonly IDocumentStorage _documentStorage;
        private readonly Timer _schemaPersistenceTimer;          
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
            _log.Info($"Data Path: {_dataPath}");

            _indexPath = Path.Combine(_dataPath, INDEX_DIRECTORY_NAME);
            EnsureIndexDirectoryExists(_indexPath);
            _log.Info($"Index Path: {_indexPath}");
           
            _documentStorage = new LightningDocumentStorage(_dataPath);
            _collections = new Dictionary<string, Collection>();

            var persistedSchemas = _documentStorage.GetAllAsync(Schema.COLLECTION_NAME).Result.Select(d => d.ToSchema());
            foreach (var schema in persistedSchemas)
            {
                var collection = new Collection(schema, _documentStorage);
                _collections.Add(schema.Name, collection);
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
                            collection = new Collection(name, _documentStorage);
                            _collections.Add(name, collection);
                        }
                    }
                }

                collection = _collections[name];
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
                {
                    collection = _collections[collectionName];
                    _collections.Remove(collectionName);                   
                }                    
            }

            if (collection == null)
                return false;
            
            isSuccessful = await collection.DropAsync().ConfigureAwait(false) &&
                           await this[Schema.COLLECTION_NAME].DeleteAsync(collection.Schema._id.Value).ConfigureAwait(false) == 1;

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

        private readonly object _schemaPersistenceLock = new object();

        /// <summary>
        /// Persists Schema objects to a special _schemas collection.
        /// </summary>
        /// <returns></returns>
        private async Task PersistSchemas()
        {
            var isLockTaken = false;
            try
            {
                isLockTaken = Monitor.TryEnter(_schemaPersistenceLock);
                if (!isLockTaken)
                    return;

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
                if (isLockTaken)
                    Monitor.Exit(_schemaPersistenceLock);
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
                var savedSchemaDocument = await this[Schema.COLLECTION_NAME].GetAsync(liveSchemaDocument._id.Value);

                var isSchemaUpdated = false;
                if (savedSchemaDocument == null)
                {
                    await this[Schema.COLLECTION_NAME].InsertAsync(liveSchemaDocument);
                    isSchemaUpdated = true;         
                }
                else
                {
                    if (liveSchemaDocument != savedSchemaDocument)
                    {
                        await this[Schema.COLLECTION_NAME].UpdateAsync(liveSchemaDocument);
                        isSchemaUpdated = true;
                    }
                }

                if (isSchemaUpdated)
                {
                    savedSchemaDocument = await this[Schema.COLLECTION_NAME].GetAsync(liveSchemaDocument._id.Value);
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

                    var collections = _collections.Values.ToList();
                    foreach (var collection in collections)
                        collection.Dispose();

                    _documentStorage.Dispose();
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
}
