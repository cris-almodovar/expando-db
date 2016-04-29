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
            _documentCollections = new ConcurrentDictionary<string, DocumentCollection>(); 
                       
            var persistedSchemas = new LightningSchemaStorage(_storageEngine).GetAllAsync().Result;             
            foreach (var schema in persistedSchemas)
            {
                var collection = new DocumentCollection(schema, _storageEngine);
                _documentCollections.TryAdd(schema.Name, collection);
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
