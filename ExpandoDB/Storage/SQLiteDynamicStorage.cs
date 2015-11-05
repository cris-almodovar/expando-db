using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// 
    /// </summary>
    public class SQLiteDynamicStorage : IDynamicStorage
    {
        private const string CONN_STRING_TEMPLATE = "Data Source={0}; Version=3; Pooling=true; Max Pool Size=100; DateTimeKind=UTC; Enlist=N; Compress=True";
        private readonly string _dbFilePath;
        private readonly string _connectionString;
        private readonly string _collectionName;
        private readonly string _createTableSql;
        private readonly string _insertSql;
        private readonly string _selectOneSql;
        private readonly string _selectManySql;
        private readonly string _selectCountSql;
        private readonly string _updateSql;
        private readonly string _deleteOneSql;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteDynamicStorage"/> class.
        /// </summary>
        /// <param name="dbFilePath">The database file path.</param>
        /// <param name="collectionName">Name of the collection.</param>        
        public SQLiteDynamicStorage(string dbFilePath, string collectionName)
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
            _selectCountSql = String.Format("SELECT COUNT(*) FROM [{0}] WHERE id = @id", _collectionName);
            _updateSql = String.Format("UPDATE [{0}] SET json = @json WHERE id = @id", _collectionName);
            _deleteOneSql = String.Format("DELETE FROM [{0}] WHERE id = @id", _collectionName);

            EnsureDatabaseExists();
            EnsureCollectionTableExists();

            NetJSON.NetJSON.DateFormat = NetJSON.NetJSONDateFormat.ISO;            
            NetJSON.NetJSON.UseEnumString = true;
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
        /// Gets a SQLite connection from the connection pool.
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
            if (content == null)
                throw new ArgumentNullException("content");
                       
            using (var conn = GetConnection())
            {
                var guid = Guid.NewGuid();                

                var dictionary = (IDictionary<string,object>)content;
                dictionary["_id"] = guid;
                dictionary["_createdTimestamp"] = DateTime.UtcNow;
                dictionary.ConvertDatesToUtc();                                               
                
                var json = NetJSON.NetJSON.Serialize(content);
                await conn.ExecuteAsync(_insertSql, new { id = guid.ToString(), json });     

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
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");
                    
            using (var conn = GetConnection())
            {                
                var result = await conn.QueryAsync<string>(_selectOneSql, new { id = guid.ToString()});
                
                var json = result.FirstOrDefault();
                if (String.IsNullOrWhiteSpace(json))    
                    return null;
                
                return json.ToExpando();
            }
        }

        /// <summary>
        /// Gets the contents identified by the given list of GUIDs.
        /// </summary>
        /// <param name="guids">A list of GUIDs identifying the contents to be retrieved.</param>
        /// <returns></returns>
        public async Task<IEnumerable<ExpandoObject>> GetAsync(IList<Guid> guids)
        {
            if (guids == null)
                throw new ArgumentNullException("guids");   
              
            using (var conn = GetConnection())
            {
                var result = await conn.QueryAsync<string>(_selectManySql, new { ids = guids.Select(g => g.ToString())});
                return result.ToExpandoList();
            }
        }

        /// <summary>
        /// Updates the dynamic content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>        /
        public async Task<int> UpdateAsync(ExpandoObject content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var dictionary = content as IDictionary<string, object>;
            if (dictionary == null)
                throw new Exception("The content cannot be converted to a Dictionary");

            var guid = dictionary.ContainsKey("_id") ? (Guid)dictionary["_id"] : Guid.Empty;
            if (guid == Guid.Empty)
                throw new Exception("The content does not have an _id field"); 

            using (var conn = GetConnection())
            {
                var count = await conn.ExecuteScalarAsync<int>(_selectCountSql, new { id = guid.ToString() });
                if (count == 0)
                    return 0;

                dictionary["_modifiedTimestamp"] = DateTime.UtcNow;
                dictionary.ConvertDatesToUtc();
                var json = NetJSON.NetJSON.Serialize(content);

                count = await conn.ExecuteAsync(_updateSql, new { id = guid.ToString(), json });
                return count;
            }
        }

        /// <summary>
        /// Deletes the dynamic content.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        /// <returns></returns>
        public async Task<int> DeleteAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");

            using (var conn = GetConnection())
            {
                var count = await conn.ExecuteAsync(_deleteOneSql, new { id = guid.ToString() });
                return count;
            }

        }
    }
}


