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
        private readonly string _contentCollectionName;
        private readonly string _createTableSql;
        private readonly string _insertSql;
        private readonly string _selectOneSql;
        private readonly string _selectManySql;
        private readonly string _selectCountSql;
        private readonly string _updateSql;
        private readonly string _deleteOneSql;
        private readonly string _deleteManySql;
        private readonly string _dropTableSql;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteContentStorage"/> class.
        /// </summary>
        /// <param name="dbFilePath">The database file path.</param>
        /// <param name="contentCollectionName">The name of the Content collection that will be stored in this instance.
        /// This corresponds to the name of the SQLite table that will hold the collection data.
        /// </param>        
        public SQLiteContentStorage(string dbFilePath, string contentCollectionName)
        {
            if (String.IsNullOrWhiteSpace(dbFilePath))
                throw new ArgumentException("dbFilePath is null or blank");
            if (String.IsNullOrWhiteSpace(contentCollectionName))
                throw new ArgumentException("collectionName is null or blank");
            if (contentCollectionName.Any(c => c == '[' || c == ']'))
                throw new ArgumentException("collectionName cannot contain '[' or ']'");

            _dbFilePath = dbFilePath;
            _contentCollectionName = contentCollectionName;
            _connectionString = String.Format(CONN_STRING_TEMPLATE, dbFilePath);

            _createTableSql = String.Format("CREATE TABLE IF NOT EXISTS [{0}] (id TEXT PRIMARY KEY, json TEXT)", _contentCollectionName);
            _insertSql = String.Format("INSERT INTO [{0}] (id, json) VALUES (@id, @json)", _contentCollectionName);
            _selectOneSql = String.Format("SELECT json FROM [{0}] WHERE id = @id", _contentCollectionName);
            _selectManySql = String.Format("SELECT id, json FROM [{0}] WHERE id IN @ids", _contentCollectionName);
            _selectCountSql = String.Format("SELECT COUNT(*) FROM [{0}] WHERE id = @id", _contentCollectionName);
            _updateSql = String.Format("UPDATE [{0}] SET json = @json WHERE id = @id", _contentCollectionName);
            _deleteOneSql = String.Format("DELETE FROM [{0}] WHERE id = @id", _contentCollectionName);
            _deleteManySql = String.Format("DELETE FROM [{0}] WHERE id IN @ids", _contentCollectionName);
            _dropTableSql = String.Format("DROP TABLE IF EXISTS [{0}]", _contentCollectionName);

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
        /// Inserts a Content object into the storage.
        /// </summary>
        /// <param name="content">The Content object to insert.</param>
        /// <returns>The GUID of the inserted Content</returns>
        /// <remarks>
        /// If the Content object does not have an id, it will be auto-generated.</remarks>
        public async Task<Guid> InsertAsync(Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");
                       
            using (var conn = GetConnection())
            {  
                if (!content._id.HasValue)
                    content._id = Guid.NewGuid();

                content._createdTimestamp = content._modifiedTimestamp = DateTime.UtcNow;
                content.ConvertDatesToUtc();

                var id = content._id.ToString();
                var json = content.ToJson();                
                await conn.ExecuteAsync(_insertSql, new { id, json });

                return content._id.Value;               
            }
        }

        /// <summary>
        /// Gets the Content identified by the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID for the content.</param>
        /// <returns>The Content object that corresponds to the specified GUID,
        /// or null if there is no Content object with the specified GUID.</returns>
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
        /// Gets the Contents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="guids">A list of GUIDs identifying the Contents to be retrieved.</param>
        /// <returns></returns>
        public async Task<IEnumerable<Content>> GetAsync(IList<Guid> guids)
        {
            if (guids == null)
                throw new ArgumentNullException("guids");

            using (var conn = GetConnection())
            {   
                var ids = guids.Select(g => g.ToString()).ToList();
                var result = await conn.QueryAsync(_selectManySql, new { ids });
                
                // The result will not be in the same order as the input guids.
                // So we need re-sort the result to be in the same order as the input guids

                var resultLookup = result.ToDictionary(row => row.id as string);
                var orderedResult = new string[guids.Count];

                for (var i = 0; i < guids.Count; i++)
                {
                    var id = ids[i];
                    var json = resultLookup[id].json as string;

                    orderedResult[i] = json;
                }

                return orderedResult.ToEnumerableContents();
            }
        }

        /// <summary>
        /// Updates the Content object.
        /// </summary>
        /// <param name="content">The Content object to update.</param>
        /// <returns></returns>        
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
        /// Deletes the Content object identified by the specified GUID.
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
        /// Deletes multiple Contents identified by the specified list of GUIDs.
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
        /// Checks whether a Content with the specified GUID exists.
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

        /// <summary>
        /// Drops the underlying SQLite table that stores the data for this Storage.
        /// </summary>
        /// <returns></returns>
        public async Task DropAsync()
        {
            using (var conn = GetConnection())
            {                
                await conn.ExecuteAsync(_dropTableSql);                
            }
        }
    }
}


