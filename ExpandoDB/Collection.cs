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
    public class Collection : IDisposable
    {        
        private readonly string _indexPath;
        private readonly IDocumentStorage _documentStorage;        
        private readonly LuceneIndex _luceneIndex;
        private readonly ILog _log = LogManager.GetLogger(typeof(Collection).Name);

        /// <summary>
        /// Gets the name of the Document Collection.
        /// </summary>
        /// <value>
        /// The name of the Document Collection
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the Schema associated with the Document Collection.
        /// </summary>
        /// <value>
        /// The Schema object.
        /// </value>
        public Schema Schema { get; private set; }        

        /// <summary>
        /// Gets a value indicating whether this Document Collection has already been dropped.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has already been dropped; otherwise, <c>false</c>.
        /// </value>
        public bool IsDropped { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Collection" /> class based on a Schema.
        /// </summary>
        /// <param name="schema">The Schema.</param>
        /// <param name="documentStorage">The document storage.</param>
        public Collection(Schema schema, IDocumentStorage documentStorage)
            : this(schema.Name, documentStorage, schema)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Collection" /> class.
        /// </summary>
        /// <param name="name">The name of the Document Collection.</param>
        /// <param name="documentStorage">The Document storage.</param>
        /// <param name="schema">The schema.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        public Collection(string name, IDocumentStorage documentStorage, Schema schema = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"{nameof(name)} cannot be null or blank");
            if (documentStorage == null)
                throw new ArgumentNullException(nameof(documentStorage));

            Name = name;
            _documentStorage = documentStorage;            

            _indexPath = Path.Combine(_documentStorage.DataPath, Database.INDEX_DIRECTORY_NAME, name);
            if (!Directory.Exists(_indexPath))
                Directory.CreateDirectory(_indexPath);            

            Schema = schema ?? Schema.CreateDefault(name);
            _luceneIndex = new LuceneIndex(_indexPath, Schema);            
        }        

        /// <summary>
        /// Inserts the specified Document into the Document Collection
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
                var exists = await _documentStorage.ExistsAsync(Name, document._id.Value).ConfigureAwait(false);
                if (exists)
                    throw new InvalidOperationException("There is an existing Document with the same _id");
            }

            if (document._id == null || document._id.Value == Guid.Empty)
                document._id = Guid.NewGuid();            

            _luceneIndex.Insert(document);
            var guid = await _documentStorage.InsertAsync(Name, document).ConfigureAwait(false);             

            return guid;
        }

        /// <summary>
        /// Searches the Document Collection for Document objects that match the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task<SearchResult<Document>> SearchAsync(SearchCriteria criteria)
        {
            EnsureCollectionIsNotDropped();

            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            var luceneResult = _luceneIndex.Search(criteria);
            var searchResult = new SearchResult<Document>(criteria, luceneResult.ItemCount, luceneResult.TotalHits, luceneResult.PageCount);

            if (searchResult.ItemCount > 0)
                searchResult.Items = await _documentStorage.GetAsync(Name, luceneResult.Items.ToList()).ConfigureAwait(false); 

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

            var document = await _documentStorage.GetAsync(Name, guid).ConfigureAwait(false); 
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

            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));
            
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
            var affected = await _documentStorage.UpdateAsync(Name, document).ConfigureAwait(false); 

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
            var affected = await _documentStorage.DeleteAsync(Name, guid).ConfigureAwait(false);             

            return affected;
        }

        /// <summary>
        /// Drops this Document Collection.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Dropping a Document Collection means dropping the underlying storage table Lucene index.</remarks>
        public async Task<bool> DropAsync()
        {
            EnsureCollectionIsNotDropped();

            IsDropped = true;

            await _documentStorage.DropAsync(Name).ConfigureAwait(false);            

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
        /// Raises an exception if the Document Collection has already been dropped.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">This Document Collection has already been dropped.</exception>
        private void EnsureCollectionIsNotDropped()
        {
            if (IsDropped)
                throw new InvalidOperationException("This Document Collection has already been dropped.");
        }

        #region IDisposable Support

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            EnsureCollectionIsNotDropped();

            if (!IsDisposed)
            {
                if (disposing)
                {                    
                    _luceneIndex.Dispose();
                }               

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {            
            Dispose(true);         
        }
        #endregion
    }
}
