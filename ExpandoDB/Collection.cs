using Common.Logging;
using ExpandoDB.Search;
using ExpandoDB.Storage;
using System;
using System.Collections.Generic;
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
        /// Prevents a default instance of the <see cref="Collection"/> class from being created.
        /// </summary>
        private Collection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Collection" /> class based on a Schema.
        /// </summary>
        /// <param name="schema">The Schema.</param>
        /// <param name="database">The database.</param>
        internal Collection(Schema schema, Database database)
            : this(schema.Name, database, schema)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Collection" /> class.
        /// </summary>
        /// <param name="name">The name of the Document Collection.</param>
        /// <param name="database">The database.</param>
        /// <param name="schema">The schema.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        internal Collection(string name, Database database, Schema schema = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"{nameof(name)} cannot be null or blank");
            if (database == null)
                throw new ArgumentNullException(nameof(database));

            Name = name;
            Schema = schema ?? Schema.CreateDefault(name);

            _documentStorage = database.DocumentStorage;            

            var indexPath = Path.Combine(database.IndexBasePath, name);
            _luceneIndex = new LuceneIndex(indexPath, Schema);            
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
                document._id = SequentialGuid.NewGuid();          

            _luceneIndex.Insert(document);       
            await _documentStorage.InsertAsync(Name, document).ConfigureAwait(false);

            return document._id.Value;
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
            
            searchResult.Facets = luceneResult.Facets;

            // NOTE: At this point the Items collection only contains the compressed binary form of the Document objects.
            // The Items collection will be deserialized to Document objects only when enumerated.

            if (criteria.IncludeHighlight == true)                            
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
        /// Gets all Documents from the collection.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException"></exception>
        public async Task<IEnumerable<Document>> GetAllAsync()
        {
            var allDocuments = await _documentStorage.GetAllAsync(Name);
            return allDocuments;            
        }

        /// <summary>
        /// Counts the Documents that match the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns></returns>
        public int Count(SearchCriteria criteria = null)
        {
            EnsureCollectionIsNotDropped();

            if (String.IsNullOrWhiteSpace(criteria?.Query))
                return _luceneIndex.GetDocumentCount();
            
            criteria.TopN = 0;  // We're not interested in the docs, just the total hits.
            criteria.TopNFacets = 0;

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
        internal async Task<bool> DropAsync()
        {
            EnsureCollectionIsNotDropped();

            IsDropped = true;

            await _documentStorage.DropAsync(Name).ConfigureAwait(false);
            await _luceneIndex.DropAsync().ConfigureAwait(false);
            
            return IsDropped;
        }

        /// <summary>
        /// Truncates this Document Collection.
        /// </summary>
        /// <returns></returns>        
        public async Task<bool> TruncateAsync()
        {
            EnsureCollectionIsNotDropped();            

            await _documentStorage.TruncateAsync(Name).ConfigureAwait(false);
            _luceneIndex.Truncate();

            return true;
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

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
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
        /// Deallocates resources used by the <see cref="Collection"/>.
        /// </summary>
        public void Dispose()
        {            
            Dispose(true);         
        }
        #endregion
    }
}
