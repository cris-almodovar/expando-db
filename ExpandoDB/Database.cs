using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

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
        public string DbPath { get { return _dbPath; } }

        private readonly string _dbFilePath;
        public string DbFilePath { get { return _dbFilePath; } }

        private readonly string _indexPath;
        public string IndexPath { get { return _indexPath; } }

        private readonly ConcurrentDictionary<string, ContentCollection> _contentCollections;        

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

            // Load collections from metadata
                
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
