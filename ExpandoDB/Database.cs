using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace ExpandoDB
{
    
    public class Database
    {
        private readonly string _dbFilePath;
        internal string DbFilePath { get { return _dbFilePath; } }

        private readonly string _indexPath;
        internal string IndexPath { get { return _indexPath; } }

        private readonly ConcurrentDictionary<string, Collection> _collections;
        public ConcurrentDictionary<string, Collection> Collections { get { return _collections; } }

        public Database(string dbFilePath)
        {
            _dbFilePath = dbFilePath;

            _indexPath = Path.Combine(Path.GetDirectoryName(_dbFilePath), "index");
            if (!Directory.Exists(_indexPath))
                Directory.CreateDirectory(_indexPath);

            _collections = new ConcurrentDictionary<string, Collection>();

            // Load collections from metadata
                
        }  
    }
}
