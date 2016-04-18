using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// A Document Storage engine that persists Document objects to a SQLite database.
    /// </summary>
    public class SQLiteDocumentStorage : IDocumentStorage
    {
        private const string CONN_STRING_TEMPLATE = "Data Source={0}; Version=3; Pooling=True; Max Pool Size={1}; DateTimeKind=UTC; Enlist=N; Compress=True; Synchronous={2}; Page Size=1024; Cache Size={3}; Jounal Mode=WAL;";
        private const int SQLITE_MAX_VARIABLE_NUMBER = 999;
        private readonly string _dbFilePath;
        private readonly string _connectionString;
        private readonly string _documentCollectionName;
        private readonly string _createTableSql;
        private readonly string _insertOneSql;
        private readonly string _selectOneSql;
        private readonly string _selectManySql;
        private readonly string _selectCountSql;
        private readonly string _updateOneSql;
        private readonly string _deleteOneSql;
        private readonly string _deleteManySql;
        private readonly string _dropTableSql;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteDocumentStorage"/> class.
        /// </summary>
        /// <param name="dbFilePath">The database file path.</param>
        /// <param name="documentCollectionName">The name of the Document collection that will be stored in this instance.
        /// This corresponds to the name of the SQLite table that will hold the collection data.
        /// </param>        
        public SQLiteDocumentStorage(string dbFilePath, string documentCollectionName)
        {
            if (String.IsNullOrWhiteSpace(dbFilePath))
                throw new ArgumentException("dbFilePath is null or blank");
            if (String.IsNullOrWhiteSpace(documentCollectionName))
                throw new ArgumentException("collectionName is null or blank");
            if (documentCollectionName.Any(c => c == '[' || c == ']'))
                throw new ArgumentException("collectionName cannot contain '[' or ']'");
            if (documentCollectionName == SQLiteSchemaStorage.SCHEMA_TABLE_NAME)
                throw new ArgumentException($"collectionName cannot be '{SQLiteSchemaStorage.SCHEMA_TABLE_NAME}'; this is a reserved name.");

            _dbFilePath = dbFilePath;
            _documentCollectionName = documentCollectionName;

            var maxPoolSize = ConfigurationManager.AppSettings["SQLiteMaxPoolSize"] ?? "100";
            var synchronous = ConfigurationManager.AppSettings["SQLitePragmaSynchronous"] ?? "Off";
            var cacheSize = ConfigurationManager.AppSettings["SQLitePragmaCacheSize"] ?? "10000";
            _connectionString = String.Format(CONN_STRING_TEMPLATE, dbFilePath, maxPoolSize, synchronous, cacheSize);

            _createTableSql = $"CREATE TABLE IF NOT EXISTS [{_documentCollectionName}] (id TEXT PRIMARY KEY, json TEXT)";
            _insertOneSql = $"INSERT INTO [{_documentCollectionName}] (id, json) VALUES (@id, @json)";
            _selectOneSql = $"SELECT id, json FROM [{_documentCollectionName}] WHERE id = @id";
            _selectManySql = $"SELECT id, json FROM [{_documentCollectionName}] WHERE id IN @ids";
            _selectCountSql = $"SELECT COUNT(*) FROM [{_documentCollectionName}] WHERE id = @id";
            _updateOneSql = $"UPDATE [{_documentCollectionName}] SET json = @json WHERE id = @id";
            _deleteOneSql = $"DELETE FROM [{_documentCollectionName}] WHERE id = @id";
            _deleteManySql = $"DELETE FROM [{_documentCollectionName}] WHERE id IN @ids";
            _dropTableSql = $"DROP TABLE IF EXISTS [{_documentCollectionName}]";

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
        /// Gets a SQLite connection from the connection pool.
        /// </summary>
        /// <returns></returns>
        private SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }

        /// <summary>
        /// Inserts a Document object into the storage.
        /// </summary>
        /// <param name="document">The Document object to insert.</param>
        /// <returns>The GUID of the inserted Document</returns>
        /// <remarks>
        /// If the Document object does not have an id, it will be auto-generated.</remarks>
        public async Task<Guid> InsertAsync(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
                       
            using (var conn = GetConnection())
            {  
                if (document._id == null || document._id.Value == Guid.Empty)
                    document._id = Guid.NewGuid();

                document._createdTimestamp = document._modifiedTimestamp = DateTime.UtcNow;
                document.ConvertDatesToUtc();

                var id = document._id.ToString();
                var json = document.ToJson();                
                await conn.ExecuteAsync(_insertOneSql, new { id, json }).ConfigureAwait(false);

                return document._id.Value;               
            }
        }

        /// <summary>
        /// Gets the Document identified by the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID for the document.</param>
        /// <returns>The Document object that corresponds to the specified GUID,
        /// or null if there is no Document object with the specified GUID.</returns>
        public async Task<Document> GetAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");
                    
            using (var conn = GetConnection())
            {
                var id = guid.ToString();
                var result = await conn.QueryAsync<StorageRow>(_selectOneSql, new { id }).ConfigureAwait(false);
                
                var row = result.FirstOrDefault();
                if (row == null)    
                    return null;
                
                return row.ToDocument();
            }
        }

        /// <summary>
        /// Gets the Documents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="guids">A list of GUIDs identifying the Documents to be retrieved.</param>
        /// <returns>A list of Document objects, in the same sequence as the input list of GUIDs.</returns>
        public async Task<IEnumerable<Document>> GetAsync(IList<Guid> guids)
        {
            if (guids == null)
                throw new ArgumentNullException(nameof(guids));

            using (var conn = GetConnection())
            {   
                var idList = guids.Select(g => g.ToString()).ToList();                
                var result = new List<StorageRow>();

                // SQLite can only handle SQLITE_MAX_VARIABLE_NUMBER number of variables per SQL statement
                // so we need to break up the idList into batches of SQLITE_MAX_VARIABLE_NUMBER ids each.
                var itemsPerBatch = SQLITE_MAX_VARIABLE_NUMBER;
                var batchCount = ComputeBatchCount(idList.Count, itemsPerBatch);

                for (var batchNumber = 0; batchNumber < batchCount; batchNumber++)
                {
                    var subList = idList.Skip(batchNumber * itemsPerBatch).Take(itemsPerBatch).ToList();
                    var subResult = await conn.QueryAsync<StorageRow>(_selectManySql, new { ids = subList }).ConfigureAwait(false);

                    // The result will *NOT* be in the same order as the input guids;
                    // we need re-sort the result to be in the same order as the input guids

                    var subResultLookup = subResult.ToDictionary(row => row.id as string);
                    for (var i = 0; i < subList.Count; i++)
                    {
                        var id = subList[i];

                        if (subResultLookup.ContainsKey(id))
                            result.Add(subResultLookup[id]);
                    }
                }

                return result.ToEnumerableDocuments();
            }
        }

        /// <summary>
        /// Updates the Document object.
        /// </summary>
        /// <param name="document">The Document object to update.</param>
        /// <returns></returns>        
        public async Task<int> UpdateAsync(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));            
            
            if (document._id == null || document._id == Guid.Empty)
                throw new Exception("The document does not have an _id field"); 

            using (var conn = GetConnection())
            {
                var id = document._id.ToString();
                var count = await conn.ExecuteScalarAsync<int>(_selectCountSql, new { id }).ConfigureAwait(false);
                if (count == 0)
                    return 0;                

                var result = await conn.QueryAsync<StorageRow>(_selectOneSql, new { id }).ConfigureAwait(false);
                var row = result.FirstOrDefault();
                if (row == null)
                    return 0;

                // Make sure the _createdTimestamp is not overwritten
                // Copy the value from the existing Document.
                var existingDocument = row.ToDocument();
                document._createdTimestamp = existingDocument._createdTimestamp;  

                // Always set the _modifiedTimestamp to the current UTC date/time.
                document._modifiedTimestamp = DateTime.UtcNow;

                // Make sure all date/times are in ISO UTC format.
                document.ConvertDatesToUtc();
                
                var json = document.ToJson();                
                count = await conn.ExecuteAsync(_updateOneSql, new { id, json }).ConfigureAwait(false);                
                return count;
            }
        }

        /// <summary>
        /// Deletes the Document object identified by the specified GUID.
        /// </summary>
        /// <param name="guid">The GUID of the Document to be deleted.</param>
        /// <returns></returns>
        public async Task<int> DeleteAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");

            using (var conn = GetConnection())
            {
                var id = guid.ToString(); 
                var count = await conn.ExecuteAsync(_deleteOneSql, new { id }).ConfigureAwait(false);
                return count;
            }
        }

        /// <summary>
        /// Deletes multiple Documents identified by the specified list of GUIDs.
        /// </summary>
        /// <param name="guids">The GUIDs of the Documents to be deleted.</param>
        /// <returns></returns>        
        public async Task<int> DeleteAsync(IList<Guid> guids)
        {
            if (guids == null)
                throw new ArgumentNullException(nameof(guids));

            using (var conn = GetConnection())
            {
                // TODO: delete in batches of SQLITE_MAX_VARIABLE_NUMBER each.
                var ids = guids.Select(g => g.ToString());
                var count = await conn.ExecuteAsync(_deleteManySql, new { ids }).ConfigureAwait(false);
                return count;
            }
        }

        /// <summary>
        /// Checks whether a Document with the specified GUID exists.
        /// </summary>
        /// <param name="guid">The GUID of the Document.</param>
        /// <returns></returns>        
        public async Task<bool> ExistsAsync(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");

            using (var conn = GetConnection())
            {
                var id = guid.ToString();
                var count = await conn.ExecuteScalarAsync<int>(_selectCountSql, new { id }).ConfigureAwait(false);
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
                await conn.ExecuteAsync(_dropTableSql).ConfigureAwait(false);                
            }            
        }

        private static int ComputeBatchCount(int totalCount, int itemsPerBatch)
        {
            var batchCount = 0;
            if (totalCount > 0 && itemsPerBatch > 0)
            {
                batchCount = totalCount / itemsPerBatch;
                var remainder = totalCount % itemsPerBatch;
                if (remainder > 0)
                    batchCount += 1;
            }

            return batchCount;
        }
    }
}


