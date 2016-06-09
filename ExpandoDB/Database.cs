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
        private readonly LightningStorageEngine _storageEngine;
        private readonly LightningDocumentStorage _documentStorage;      
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

            _storageEngine = new LightningStorageEngine(_dataPath);
            _documentStorage = new LightningDocumentStorage(_storageEngine);
            _collections = new Dictionary<string, Collection>();

            var persistedSchemas = _documentStorage.GetAllAsync(Schema.COLLECTION_NAME).Result.Select(d => new Schema().PopulateWith(d.AsDictionary()));  
            foreach (var schema in persistedSchemas)
            {
                var collection = new Collection(schema, _documentStorage);
                _collections.Add(schema.Name, collection);
            }           
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
            return _collections.Keys;
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

            isSuccessful = await collection.DropAsync().ConfigureAwait(false);

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

        #region IDisposable Support

        private bool _isDisponsed = false;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisponsed)
            {
                if (disposing)
                {
                    foreach (var collection in _collections.Values)
                        collection.Dispose();

                    _storageEngine.Dispose();
                }

                _isDisponsed = true;
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
