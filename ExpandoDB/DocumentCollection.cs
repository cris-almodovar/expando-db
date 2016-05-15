using Common.Logging;
using ExpandoDB.Search;
using ExpandoDB.Storage;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ExpandoDB
{
    /// <summary>
    /// Represents a collection of <see cref="Document"/> objects.
    /// </summary>
    /// <remarks>
    /// This class is analogous to an MongoDB collection.
    /// </remarks>
    public class DocumentCollection : IDisposable
    {
        private readonly LightningStorageEngine _storageEngine;
        private readonly string _indexPath;
        private readonly IDocumentStorage _documentStorage;
        private readonly ISchemaStorage _schemaStorage;
        private readonly LuceneIndex _luceneIndex;
        private readonly IndexSchema _indexSchema;
        private readonly string _name;
        private readonly Timer _schemaPersistenceTimer;
        private readonly double _schemaPersistenceIntervalSeconds;        
        private readonly ILog _log = LogManager.GetLogger(typeof(DocumentCollection).Name);

        /// <summary>
        /// Gets the IndexSchema associated with the DocumentCollection.
        /// </summary>
        /// <value>
        /// The IndexSchema object.
        /// </value>
        public IndexSchema IndexSchema { get { return _indexSchema; } }

        /// <summary>
        /// Gets the name of the DocumentCollection.
        /// </summary>
        /// <value>
        /// The name of the DocumentCollection
        /// </value>
        public string Name { get { return _name; } }

        /// <summary>
        /// Gets a value indicating whether this DocumentCollection has already been dropped.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has already been dropped; otherwise, <c>false</c>.
        /// </value>
        public bool IsDropped { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentCollection" /> class based on a DocumentCollectionSchema.
        /// </summary>
        /// <param name="schema">The DocumentCollectionSchema.</param>
        /// <param name="storageEngine">The storage engine.</param>
        public DocumentCollection(DocumentCollectionSchema schema, LightningStorageEngine storageEngine)
            : this(schema.Name, storageEngine, schema.ToIndexSchema())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentCollection" /> class.
        /// </summary>
        /// <param name="name">The name of the DocumentCollection.</param>
        /// <param name="storageEngine">The storage engine.</param>
        /// <param name="indexSchema">The index schema.</param>        
        public DocumentCollection(string name, LightningStorageEngine storageEngine, IndexSchema indexSchema = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"{nameof(name)} cannot be null or blank");
            if (storageEngine == null)
                throw new ArgumentNullException(nameof(storageEngine));

            _name = name;
            _storageEngine = storageEngine;

            _indexPath = Path.Combine(_storageEngine.DataPath, Database.INDEX_DIRECTORY_NAME, name);
            if (!Directory.Exists(_indexPath))
                Directory.CreateDirectory(_indexPath);

            _documentStorage = new LightningDocumentStorage(_name, _storageEngine);
            _schemaStorage = new LightningSchemaStorage(_storageEngine);

            _indexSchema = indexSchema ?? IndexSchema.CreateDefault(name);
            _luceneIndex = new LuceneIndex(_indexPath, _indexSchema);

            _schemaPersistenceIntervalSeconds = Double.Parse(ConfigurationManager.AppSettings["SchemaPersistenceIntervalSeconds"] ?? "1");
            _schemaPersistenceTimer = new Timer( _ => Task.Run(async() => await PersistSchema().ConfigureAwait(false)), null, TimeSpan.FromSeconds(_schemaPersistenceIntervalSeconds), TimeSpan.FromSeconds(_schemaPersistenceIntervalSeconds));
        }

        private async Task PersistSchema()
        {
            if (IsDropped)
                return;            
            try
            {
                var liveSchema = _indexSchema.ToDocumentCollectionSchema();
                var savedSchema = await _schemaStorage.GetAsync(_name).ConfigureAwait(false);

                if (savedSchema == null)
                    await _schemaStorage.InsertAsync(liveSchema).ConfigureAwait(false);
                else
                    if (liveSchema != savedSchema)
                        await _schemaStorage.UpdateAsync(liveSchema).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }            
        }

        /// <summary>
        /// Inserts the specified Document into the DocumentCollection
        /// </summary>
        /// <param name="document">The Document object to insert</param>
        /// <returns></returns>
        public async Task<Guid> InsertAsync(Document document)
        {
            EnsureCollectionIsNotDropped();

            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (document._id.HasValue)
            {
                var exists = await _documentStorage.ExistsAsync(document._id.Value).ConfigureAwait(false);
                if (exists)
                    throw new InvalidOperationException("There is an existing Document with the same _id");
            }

            if (document._id == null || document._id.Value == Guid.Empty)
                document._id = Guid.NewGuid();            

            _luceneIndex.Insert(document);
            var guid = await _documentStorage.InsertAsync(document).ConfigureAwait(false);             

            return guid;
        }

        /// <summary>
        /// Searches the DocumentCollection for Document objects that match the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task<SearchResult<Document>> SearchAsync(SearchCriteria criteria)
        {
            EnsureCollectionIsNotDropped();            

            var luceneResult = _luceneIndex.Search(criteria);
            var searchResult = new SearchResult<Document>(criteria, luceneResult.ItemCount, luceneResult.TotalHits, luceneResult.PageCount);

            if (searchResult.ItemCount > 0)
                searchResult.Items = await _documentStorage.GetAsync(luceneResult.Items.ToList()).ConfigureAwait(false); 

            // NOTE: At this point the Items collection only contains the compressed binary form of the Document objects.
            // The Items collection will be deserialized to Document objects only when enumerated.

            if (criteria.IncludeHighlight)                            
                searchResult.Items = searchResult.Items.GenerateHighlights(criteria);            

            return searchResult;
        }

        /// <summary>
        /// Gets the Document identified by the specified guid.
        /// </summary>
        /// <param name="guid">The Document's unique identifier.</param>
        /// <returns></returns>        
        /// <remarks>
        /// This method bypasses the Lucene index, retrieving the Document 
        /// directly from storage.
        /// </remarks>
        public async Task<Document> GetAsync(Guid guid)
        {
            EnsureCollectionIsNotDropped();

            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");

            var document = await _documentStorage.GetAsync(guid).ConfigureAwait(false); 
            return document;
        }

        /// <summary>
        /// Counts the Documents that match the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns></returns>
        public int Count(SearchCriteria criteria)
        {
            EnsureCollectionIsNotDropped();           
            
            criteria.TopN = 1;  // We're not interested in the docs, just the total hits.
            var luceneResult = _luceneIndex.Search(criteria);
            return luceneResult.TotalHits;
        }

        /// <summary>
        /// Updates the Document object.
        /// </summary>
        /// <param name="document">The Document object to be updated.</param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(Document document)
        {
            EnsureCollectionIsNotDropped();

            if (document == null)
                throw new ArgumentNullException(nameof(document));

            _luceneIndex.Update(document);
            var affected = await _documentStorage.UpdateAsync(document).ConfigureAwait(false); 

            return affected;
        }


        /// <summary>
        /// Deletes the Document object identified by the specified guid.
        /// </summary>
        /// <param name="guid">The GUID of the Document object to be deleted.</param>
        /// <returns></returns>
        public async Task<int> DeleteAsync(Guid guid)
        {
            EnsureCollectionIsNotDropped();

            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");

            _luceneIndex.Delete(guid);
            var affected = await _documentStorage.DeleteAsync(guid).ConfigureAwait(false);             

            return affected;
        }

        /// <summary>
        /// Drops this DocumentCollection.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Dropping a DocumentCollection means dropping the underlying storage table Lucene index.</remarks>
        public async Task<bool> DropAsync()
        {
            EnsureCollectionIsNotDropped();

            IsDropped = true;

            await _documentStorage.DropAsync().ConfigureAwait(false);

            _schemaPersistenceTimer.Dispose();
            await _schemaStorage.DeleteAsync(_name).ConfigureAwait(false);

            _luceneIndex.Dispose();
            
            var tryCount = 0;
            while (tryCount < 3)
            {
                tryCount += 1;
                // Wait half a second before deleting the Lucene index
                await Task.Delay(TimeSpan.FromSeconds(0.5)).ConfigureAwait(false);
                if (!Directory.Exists(_indexPath))
                    break;

                Directory.Delete(_indexPath, true);                
            }

            if (Directory.Exists(_indexPath))
                throw new Exception($"Unable to delete Lucene index directory: {_indexPath}");
            
            return IsDropped;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            EnsureCollectionIsNotDropped();
            PersistSchema().Wait();

            _schemaPersistenceTimer.Dispose();
            _luceneIndex.Dispose();            
        }

        /// <summary>
        /// Raises an exception if the DocumentCollection has already been dropped.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">This DocumentCollection has already been dropped.</exception>
        private void EnsureCollectionIsNotDropped()
        {
            if (IsDropped)
                throw new InvalidOperationException("This DocumentCollection has already been dropped.");
        }

        internal DocumentCollectionSchema GetSchema()
        {
            var schema = new DocumentCollectionSchema(Name);
            foreach (var fieldName in IndexSchema.Fields.Keys)
            {
                var field = IndexSchema.Fields[fieldName];
                if (field == null)
                    continue;

                var fieldCopy = new DocumentCollectionSchemaField
                {
                    Name = field.Name,
                    DataType = field.DataType,
                    ArrayElementDataType = field.ArrayElementDataType,
                    ObjectSchema = field.ObjectSchema.ToDocumentCollectionSchema()
                };

                schema.Fields.Add(fieldCopy);
            }

            schema.Fields = schema.Fields.OrderBy(f => f.Name).ToList();

            return schema;
        }
    }
}
