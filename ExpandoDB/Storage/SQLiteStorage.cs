using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Dapper;
using System.Dynamic;
using Jil;
using System.Collections.Specialized;

namespace PostItDB.Storage
{
    /// <summary>
    /// 
    /// </summary>
    public class SQLiteStorage : IDynamicStorage
    {
        private const string CONN_STRING_TEMPLATE = "Data Source={0}; Version=3; Pooling=true; Max Pool Size=100; DateTimeKind=UTC; Enlist=N; Compress=True";
        private readonly string _dbFilePath;
        private readonly string _connectionString;
        private readonly string _collectionName;
        private readonly string _createTableSql;
        private readonly string _insertSql;
        private readonly string _selectOneSql;
        private readonly string _selectManySql;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteStorage"/> class.
        /// </summary>
        /// <param name="dbFilePath">The database file path.</param>
        /// <param name="collectionName">Name of the collection.</param>
        /// <exception cref="ArgumentException">
        /// dbFilePath is null or blank
        /// or
        /// collectionName is null or blank
        /// </exception>
        public SQLiteStorage(string dbFilePath, string collectionName)
        {
            if (String.IsNullOrWhiteSpace(dbFilePath))
                throw new ArgumentException("dbFilePath is null or blank");
            if (String.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("collectionName is null or blank");
            if (collectionName.Any(c => c == '[' || c == ']'))
                throw new ArgumentException("collectionName cannot contain '[' or ']'");

            _dbFilePath = dbFilePath;
            _collectionName = collectionName;
            _connectionString = String.Format(CONN_STRING_TEMPLATE, dbFilePath);

            _createTableSql = String.Format("CREATE TABLE IF NOT EXISTS [{0}] (id TEXT PRIMARY KEY, json TEXT)", _collectionName);
            _insertSql = String.Format("INSERT INTO [{0}] (id, json) VALUES (@id, @json)", _collectionName);
            _selectOneSql = String.Format("SELECT json FROM [{0}] WHERE id = @id", _collectionName);
            _selectManySql = String.Format("SELECT json FROM [{0}] WHERE id IN @ids", _collectionName);

            EnsureDatabaseExists();
            EnsureCollectionTableExists();
        }

        /// <summary>
        /// Ensures the database exists.
        /// </summary>
        private void EnsureDatabaseExists()
        {
            if (!File.Exists(_dbFilePath))
                SQLiteConnection.CreateFile(_dbFilePath);            
        }

        /// <summary>
        /// Ensures the collection table exists.
        /// </summary>
        private void EnsureCollectionTableExists()
        {            
            using (var conn = GetConnection())
            {
                conn.Execute(_createTableSql);
            }
        }

        /// <summary>
        /// Gets a SQLite connection from the pool.
        /// </summary>
        /// <returns></returns>
        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }

        /// <summary>
        /// Inserts a new content into the storage.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>The unique identifier for the inserted content</returns>
        public async Task<Guid> InsertAsync(ExpandoObject content)
        {            
            using (var conn = GetConnection())
            {
                var guid = Guid.NewGuid();                

                dynamic dynamicContent = content;
                dynamicContent._id = guid;
                dynamicContent._createdTimeStamp = DateTime.Now;                                
                
                var json = JSON.SerializeDynamic(content, Options.ISO8601);
                await conn.ExecuteAsync(_insertSql, new { id = guid.ToString(), json = json });     

                return guid;               
            }
        }

        /// <summary>
        /// Gets the content identified by the given GUID.
        /// </summary>
        /// <param name="guid">The unique identifier for the content.</param>
        /// <returns></returns>
        public async Task<ExpandoObject> GetAsync(Guid guid)
        {            
            using (var conn = GetConnection())
            {                
                var result = await conn.QueryAsync<string>(_selectOneSql, new { id = guid.ToString()});
                
                var json = result.FirstOrDefault();
                if (String.IsNullOrWhiteSpace(json))    
                    return null;                
                
                var dictionary = JSON.Deserialize<Dictionary<string, object>>(json, Options.ISO8601);
                return dictionary.ToExpando();
            }
        }

        /// <summary>
        /// Gets the contents identified by the given list of GUIDs.
        /// </summary>
        /// <param name="guids">A list of GUIDs identifying the contents to be retrieved.</param>
        /// <returns></returns>
        public async Task<IEnumerable<ExpandoObject>> GetAsync(IList<Guid> guids)
        {            
            using (var conn = GetConnection())
            {
                var result = await conn.QueryAsync<string>(_selectManySql, new { ids = guids.Select(g => g.ToString())});
                return result.ToExpandoList();
            }
        }

        /// <summary>
        /// Updates the asynchronous.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public Task<int> UpdateAsync(ExpandoObject content)
        {
            throw new NotImplementedException();
        }

        public Task<int> DeleteAsync(Guid guid)
        {
            throw new NotImplementedException();
        }
    }
}


