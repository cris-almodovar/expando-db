using ExpandoDB.Search;
using ExpandoDB.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ExpandoDB
{
    /// <summary>
    /// Represents a collection of Content objects.
    /// </summary>
    /// <remarks>
    /// This class is analogous to an RDBMS table.
    /// </remarks>
    public class ContentCollection : IDisposable
    {
        private readonly string _dbFilePath;
        private readonly string _indexPath;
        private readonly IContentStorage _contentStorage;
        private readonly LuceneIndex _luceneIndex;
        private readonly IndexSchema _indexSchema;        
        private readonly string _name;

        /// <summary>
        /// Gets the IndexSchema associated with the ContentCollection.
        /// </summary>
        /// <value>
        /// The IndexSchema object.
        /// </value>
        public IndexSchema IndexSchema { get { return _indexSchema; } }
        
        /// <summary>
        /// Gets the name of the ContentCollection.
        /// </summary>
        /// <value>
        /// The name of the ContentCollection
        /// </value>
        public string Name { get { return _name; } }

        /// <summary>
        /// Gets a value indicating whether this ContentCollection has been already dropped.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has already been dropped; otherwise, <c>false</c>.
        /// </value>
        public bool IsDropped { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentCollection" /> class based on a ContentCollectionSchema.
        /// </summary>
        /// <param name="schema">The ContentCollectionSchema.</param>
        /// <param name="dbPath">The path to the db folder.</param>
        public ContentCollection(ContentCollectionSchema schema, string dbPath)
            : this ( schema.Name, dbPath, schema.GetIndexSchema() )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentCollection" /> class.
        /// </summary>
        /// <param name="name">The name of the ContentCollection.</param>
        /// <param name="dbPath">The path to the db folder.</param>
        /// <param name="indexSchema">The index schema.</param>
        public ContentCollection(string name, string dbPath, IndexSchema indexSchema = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("name cannot be null or blank");
            if (String.IsNullOrWhiteSpace(dbPath))
                throw new ArgumentException("dbPath cannot be null or blank");

            _name = name;

            if (!Directory.Exists(dbPath))
                Directory.CreateDirectory(dbPath);

            _dbFilePath = Path.Combine(dbPath, Database.DB_FILENAME);

            _indexPath = Path.Combine(dbPath, Database.INDEX_DIR_NAME, name);
            if (!Directory.Exists(_indexPath))
                Directory.CreateDirectory(_indexPath);

            _contentStorage = new SQLiteContentStorage(_dbFilePath, _name);

            _indexSchema = indexSchema ?? IndexSchema.CreateDefault(name);            
            _luceneIndex = new LuceneIndex(_indexPath, _indexSchema);
        }

        /// <summary>
        /// Inserts the specified Content into the ContentCollection
        /// </summary>
        /// <param name="content">The Content object to insert</param>
        /// <returns></returns>
        public async Task<Guid> InsertAsync(Content content)
        {
            EnsureCollectionIsNotDropped();

            if (content == null)
                throw new ArgumentNullException("content");

            if (content._id.HasValue)
            {
                var exists = await _contentStorage.ExistsAsync(content._id.Value);
                if (exists)
                    throw new InvalidOperationException("There is an existing Content with the same _id");
            }
            
            var guid = await _contentStorage.InsertAsync(content);
            _luceneIndex.Insert(content);

            return guid;
        }

        /// <summary>
        /// Searches the ContentCollection for Contents that match the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns></returns>
        public async Task<SearchResult<Content>> SearchAsync(SearchCriteria criteria)
        {
            EnsureCollectionIsNotDropped();

            if (criteria == null)
                throw new ArgumentNullException("criteria");

            var luceneResult = _luceneIndex.Search(criteria);
            var searchResult = new SearchResult<Content>(criteria, luceneResult.HitCount, luceneResult.TotalHitCount, luceneResult.PageCount);            

            if (searchResult.HitCount > 0)            
                searchResult.Items = await _contentStorage.GetAsync(luceneResult.Items.ToList());            

            return searchResult; 
        }

        /// <summary>
        /// Gets the Content identified by the specified guid.
        /// </summary>
        /// <param name="guid">The Content's unique identifier.</param>
        /// <returns></returns>        
        /// <remarks>
        /// This method bypasses the Lucene index and
        /// tries to retrieve the Content from storage.
        /// </remarks>
        public async Task<Content> GetAsync(Guid guid)
        {
            EnsureCollectionIsNotDropped();

            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");
            
            var content = await _contentStorage.GetAsync(guid);
            return content;
        }

        /// <summary>
        /// Counts the Contents that match the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns></returns>
        public int Count(SearchCriteria criteria)
        {
            EnsureCollectionIsNotDropped();

            if (criteria == null)
                throw new ArgumentNullException("criteria");

            var luceneResult = _luceneIndex.Search(criteria);
            return luceneResult.TotalHitCount ?? 0;
        }

        /// <summary>
        /// Updates the Content object.
        /// </summary>
        /// <param name="content">The Content object to be updated.</param>
        /// <returns></returns>
        public async Task<int> UpdateAsync(Content content)
        {
            EnsureCollectionIsNotDropped();

            if (content == null)
                throw new ArgumentNullException("content");

            var affected = await _contentStorage.UpdateAsync(content);
            if (affected > 0)
                _luceneIndex.Update(content);

            return affected;
        }


        /// <summary>
        /// Deletes the Content object identified by the specified guid.
        /// </summary>
        /// <param name="guid">The GUID of the Content object to be deleted.</param>
        /// <returns></returns>
        public async Task<int> DeleteAsync(Guid guid)
        {
            EnsureCollectionIsNotDropped();

            if (guid == Guid.Empty)
                throw new ArgumentException("guid cannot be empty");

            var affected = await _contentStorage.DeleteAsync(guid);            
            _luceneIndex.Delete(guid);

            return affected;
        }

        /// <summary>
        /// Drops this ContentCollection.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Dropping a ContentCollection means dropping the underlying DB table Lucene index.</remarks>
        public async Task<bool> DropAsync()
        {
            EnsureCollectionIsNotDropped();

            await _contentStorage.DropAsync();            
            
            _luceneIndex.Dispose();            
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            Directory.Delete(_indexPath, true);
            
            IsDropped = true;
            return IsDropped;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            EnsureCollectionIsNotDropped();

            _luceneIndex.Dispose();
        }

        /// <summary>
        /// Raises an exception if the ContentCollection has already been dropped.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">This ContentCollection has already been dropped.</exception>
        private void EnsureCollectionIsNotDropped()
        {
            if (IsDropped)
                throw new InvalidOperationException("This ContentCollection has already been dropped.");
        }

        internal ContentCollectionSchema GetSchema()
        {
            var schema = new ContentCollectionSchema(Name);
            foreach (var fieldName in IndexSchema.Fields.Keys)
            {
                var field = IndexSchema.Fields[fieldName];
                if (field == null)
                    continue;

                var fieldCopy = new IndexedField 
                { 
                    Name = field.Name, 
                    DataType = field.DataType, 
                    ArrayElementDataType = 
                    field.ArrayElementDataType
                };

                schema.IndexedFields.Add(fieldCopy);
            }

            schema.IndexedFields = schema.IndexedFields.OrderBy(f => f.Name).ToList();

            return schema;
        }
    }
}
