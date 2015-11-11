using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace ExpandoDB
{
    public class Database
    {
        private const string DB_FILENAME = "expando-db.dat";
        private const string INDEX_DIR_NAME = "index";

        private readonly string _dbPath;
        public string DbPath { get { return _dbPath; } }

        private readonly string _dbFilePath;
        public string DbFilePath { get { return _dbFilePath; } }

        private readonly string _indexPath;
        public string IndexPath { get { return _indexPath; } }

        private readonly ConcurrentDictionary<string, ContentCollection> _collections;
               
        
        public Database(string dbPath)
        {
            if (String.IsNullOrWhiteSpace(dbPath))
                throw new ArgumentException("dbPath cannot be null or empty");

            _dbPath = dbPath;
            if (!Directory.Exists(_dbPath))
                Directory.CreateDirectory(_dbPath);

            _dbFilePath = Path.Combine(_dbPath, DB_FILENAME);
            EnsureDatabaseExists(_dbFilePath);

            _indexPath = Path.Combine(Path.GetDirectoryName(_dbFilePath), INDEX_DIR_NAME);
            EnsureIndexDirectoryExists(_indexPath);

            _collections = new ConcurrentDictionary<string, ContentCollection>();

            // Load collections from metadata
                
        }

        /// <summary>
        /// Ensures the SQLite database file exists.
        /// </summary>
        private static void EnsureDatabaseExists(string dbFilePath)
        {
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
    }
}
