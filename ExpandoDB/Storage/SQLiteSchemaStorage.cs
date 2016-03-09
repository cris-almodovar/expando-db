using Dapper;
using ExpandoDB.Serialization;
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
    /// A Schema Storage engine that persists Content objects to a SQLite database.
    /// </summary>
    /// <seealso cref="ExpandoDB.Storage.ISchemaStorage" />
    internal class SQLiteSchemaStorage : ISchemaStorage
    {
        public const string SCHEMA_TABLE_NAME = "_schema";
        private const string CONN_STRING_TEMPLATE = "Data Source={0}; Version=3; Pooling=True; Max Pool Size={1}; DateTimeKind=UTC; Enlist=N; Compress=True; Synchronous={2}; Page Size=1024; Cache Size={3}; Jounal Mode=WAL;";
        private readonly string _dbFilePath;
        private readonly string _connectionString;
        private readonly string _createTableSql;
        private readonly string _insertOneSql;
        private readonly string _selectAllSql;
        private readonly string _updateOneSql;
        private readonly string _deleteOneSql;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteSchemaStorage"/> class.
        /// </summary>
        /// <param name="dbFilePath">The database file path.</param>        
        public SQLiteSchemaStorage(string dbFilePath)
        {
            if (String.IsNullOrWhiteSpace(dbFilePath))
                throw new ArgumentException("dbFilePath is null or blank");

            _dbFilePath = dbFilePath;

            var maxPoolSize = ConfigurationManager.AppSettings["SQLiteMaxPoolSize"] ?? "100";
            var synchronous = ConfigurationManager.AppSettings["SQLitePragmaSynchronous"] ?? "Off";
            var cacheSize = ConfigurationManager.AppSettings["SQLitePragmaCacheSize"] ?? "10000";
            _connectionString = String.Format(CONN_STRING_TEMPLATE, dbFilePath, maxPoolSize, synchronous, cacheSize);

            _createTableSql = $"CREATE TABLE IF NOT EXISTS [{SCHEMA_TABLE_NAME}] (name TEXT PRIMARY KEY, json TEXT)";
            _insertOneSql = $"INSERT INTO [{SCHEMA_TABLE_NAME}] (name, json) VALUES (@name, @json)";
            _selectAllSql = $"SELECT name,json FROM [{SCHEMA_TABLE_NAME}]";           
            _updateOneSql = $"UPDATE [{SCHEMA_TABLE_NAME}] SET json = @json WHERE name = @name";
            _deleteOneSql = $"DELETE FROM [{SCHEMA_TABLE_NAME}] WHERE name = @name";

            EnsureDatabaseExists();
            EnsureSchemaTableExists();                 
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
        /// Ensures the schema table exists.
        /// </summary>
        private void EnsureSchemaTableExists()
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

        public async Task<IList<ContentCollectionSchema>> GetAllAsync()
        {
            using (var conn = GetConnection())
            {
                var result = await conn.QueryAsync(_selectAllSql).ConfigureAwait(false);
                var resultLookup = result.ToDictionary(row => row.name as string);

                var collectionSchemas = new List<ContentCollectionSchema>();
                foreach (var schemaName in resultLookup.Keys)
                {
                    var json = resultLookup[schemaName].json as string;
                    var schema = DynamicSerializer.Deserialize<ContentCollectionSchema>(json);
                    collectionSchemas.Add(schema);
                }

                return collectionSchemas;
            }
        }       

        public async Task<string> InsertAsync(ContentCollectionSchema collectionSchema)
        {
            if (collectionSchema == null)
                throw new ArgumentNullException(nameof(collectionSchema));

            using (var conn = GetConnection())
            {
                var name = collectionSchema.Name;
                var json = DynamicSerializer.Serialize(collectionSchema);
                await conn.ExecuteAsync(_insertOneSql, new { name, json }).ConfigureAwait(false);

                return name;
            }            
        }

        public async Task<int> UpdateAsync(ContentCollectionSchema collectionSchema)
        {
            if (collectionSchema == null)
                throw new ArgumentNullException(nameof(collectionSchema));

            using (var conn = GetConnection())
            {
                var name = collectionSchema.Name;
                var json = DynamicSerializer.Serialize(collectionSchema);
                var count = await conn.ExecuteAsync(_updateOneSql, new { name, json }).ConfigureAwait(false);

                return count;
            }
        }

        public async Task<int> DeleteAsync(string schemaName)
        {
            if (String.IsNullOrWhiteSpace(schemaName))
                throw new ArgumentException("schemaName cannot be null or blank");

            using (var conn = GetConnection())
            {               
                var count = await conn.ExecuteAsync(_deleteOneSql, new { name = schemaName }).ConfigureAwait(false);
                return count;
            }
        }
    }
}
