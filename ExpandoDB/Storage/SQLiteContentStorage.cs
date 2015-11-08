using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// Implements the persistence of dynamic Content objects, using SQLite as the storage engine.
    /// </summary>
    public class SQLiteContentStorage : IContentStorage
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
        private readonly string _deleteManySql;        

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteContentStorage"/> class.
        /// </summary>
        /// <param name="dbFilePath">The database file path.</param>
        /// <param name="collectionName">Name of the collection.</param>        
        public SQLiteContentStorage(string dbFilePath, string collectionName)
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
            _deleteManySql = String.Format("DELETE FROM [{0}] WHERE id IN @ids", _collectionName);            

            EnsureDatabaseExists();
            EnsureCollectionTableExists();

            NetJSON.NetJSON.DateFormat = NetJSON.NetJSONDateFormat.ISO;
            NetJSON.NetJSON.UseEnumString = false;
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
        public async Task<Guid> InsertAsync(Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");
                       
            using (var conn = GetConnection())
            {
                var guid = Guid.NewGuid();
                content._id = guid;
                content._createdTimestamp = DateTime.UtcNow;
                content.ConvertDatesToUtc();

                var id = content._id.ToString();
                var json = content.ToJson();                
                await conn.ExecuteAsync(_insertSql, new { id, json });

                return guid;               
            }
        }

        /// <summary>
        /// Gets the content identified by the given GUID.
        /// </summary>
        /// <param name="guid">The unique identifier for the content.</param>
        /// <returns></returns>
        public async Task<Content> GetAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");
                    
            using (var conn = GetConnection())
            {
                var id = guid.ToString();
                var result = await conn.QueryAsync<string>(_selectOneSql, new { id });
                
                var json = result.FirstOrDefault();
                if (String.IsNullOrWhiteSpace(json))    
                    return null;
                
                return json.ToContent();
            }
        }

        /// <summary>
        /// Gets the Contents identified by the given list of GUIDs.
        /// </summary>
        /// <param name="guids">A list of GUIDs identifying the Contents to be retrieved.</param>
        /// <returns></returns>
        public async Task<IEnumerable<Content>> GetAsync(IList<Guid> guids)
        {
            if (guids == null)
                throw new ArgumentNullException("guids");   
              
            using (var conn = GetConnection())
            {
                var ids = guids.Select(g => g.ToString());
                var result = await conn.QueryAsync<string>(_selectManySql, new { ids });
                return result.ToEnumerableContents();
            }
        }

        /// <summary>
        /// Updates the dynamic content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>        /
        public async Task<int> UpdateAsync(Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");            
            
            if (content._id == null || content._id == Guid.Empty)
                throw new Exception("The content does not have an _id field"); 

            using (var conn = GetConnection())
            {
                var id = content._id.ToString();
                var count = await conn.ExecuteScalarAsync<int>(_selectCountSql, new { id });
                if (count == 0)
                    return 0;

                content._modifiedTimestamp = DateTime.UtcNow;
                content.ConvertDatesToUtc();
                
                var json = content.ToJson();                
                count = await conn.ExecuteAsync(_updateSql, new { id, json });                
                return count;
            }
        }

        /// <summary>
        /// Deletes the Content identified by the GUID.
        /// </summary>
        /// <param name="guid">The GUID of the Content to be deleted.</param>
        /// <returns></returns>
        public async Task<int> DeleteAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");

            using (var conn = GetConnection())
            {
                var id = guid.ToString(); 
                var count = await conn.ExecuteAsync(_deleteOneSql, new { id });
                return count;
            }
        }

        /// <summary>
        /// Deletes the Contents identified by the given list of GUIDs.
        /// </summary>
        /// <param name="guids">The GUIDs of the Contents to be deleted.</param>
        /// <returns></returns>        
        public async Task<int> DeleteAsync(IList<Guid> guids)
        {
            if (guids == null)
                throw new ArgumentNullException("guids");

            using (var conn = GetConnection())
            {
                var ids = guids.Select(g => g.ToString());
                var count = await conn.ExecuteAsync(_deleteManySql, new { ids });
                return count;
            }
        }

        /// <summary>
        /// Checks whether a Content with the given GUID exists.
        /// </summary>
        /// <param name="guid">The GUID of the Content.</param>
        /// <returns></returns>        
        public async Task<bool> ExistsAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");

            using (var conn = GetConnection())
            {
                var id = guid.ToString();
                var count = await conn.ExecuteScalarAsync<int>(_selectCountSql, new { id });
                return count > 0;
            }
        }
    }
}


