﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace ExpandoDB.Storage
{
    internal class SQLiteSchemaStorage : ISchemaStorage
    {
        public const string SCHEMA_TABLE_NAME = "_schema";
        private const string CONN_STRING_TEMPLATE = "Data Source={0}; Version=3; Pooling=true; Max Pool Size=100; DateTimeKind=UTC; Enlist=N; Compress=True";
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

            _connectionString = String.Format(CONN_STRING_TEMPLATE, dbFilePath);
            _createTableSql = String.Format("CREATE TABLE IF NOT EXISTS [{0}] (name TEXT PRIMARY KEY, json TEXT)", SCHEMA_TABLE_NAME);
            _insertOneSql = String.Format("INSERT INTO [{0}] (name, json) VALUES (@name, @json)", SCHEMA_TABLE_NAME);
            _selectAllSql = String.Format("SELECT name,json FROM [{0}]", SCHEMA_TABLE_NAME);           
            _updateOneSql = String.Format("UPDATE [{0}] SET json = @json WHERE name = @name", SCHEMA_TABLE_NAME);
            _deleteOneSql = String.Format("DELETE FROM [{0}] WHERE name = @name", SCHEMA_TABLE_NAME);

            EnsureDatabaseExists();
            EnsureSchemaTableExists();

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
                var result = await conn.QueryAsync(_selectAllSql);
                var resultLookup = result.ToDictionary(row => row.name as string);

                var collectionSchemas = new List<ContentCollectionSchema>();
                foreach (var schemaName in resultLookup.Keys)
                {
                    var json = resultLookup[schemaName].json as string;
                    var schema = NetJSON.NetJSON.Deserialize<ContentCollectionSchema>(json);
                    collectionSchemas.Add(schema);
                }

                return collectionSchemas;
            }
        }       

        public async Task<string> InsertAsync(ContentCollectionSchema collectionSchema)
        {
            if (collectionSchema == null)
                throw new ArgumentNullException("collectionSchema");

            using (var conn = GetConnection())
            {
                var name = collectionSchema.Name;
                var json = NetJSON.NetJSON.Serialize(collectionSchema);
                await conn.ExecuteAsync(_insertOneSql, new { name, json });

                return name;
            }            
        }

        public async Task<int> UpdateAsync(ContentCollectionSchema collectionSchema)
        {
            if (collectionSchema == null)
                throw new ArgumentNullException("collectionSchema");

            using (var conn = GetConnection())
            {
                var name = collectionSchema.Name;
                var json = NetJSON.NetJSON.Serialize(collectionSchema);
                var count = await conn.ExecuteAsync(_updateOneSql, new { name, json });

                return count;
            }
        }

        public async Task<int> DeleteAsync(string schemaName)
        {
            if (String.IsNullOrWhiteSpace(schemaName))
                throw new ArgumentException("schemaName cannot be null or blank");

            using (var conn = GetConnection())
            {               
                var count = await conn.ExecuteAsync(_deleteOneSql, new { name = schemaName });
                return count;
            }
        }
    }
}