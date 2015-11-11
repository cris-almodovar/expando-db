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
        private readonly IContentStorage _storage;
        private readonly LuceneIndex _luceneIndex;
        private readonly ContentSchema _schema;
        private readonly string _name;
        /// <summary>
        /// Gets the name of the ContentCollection.
        /// </summary>
        /// <value>
        /// The name of the ContentCollection
        /// </value>
        public string Name { get { return _name; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentCollection"/> class.
        /// </summary>
        /// <param name="name">The name of the ContentCollection.</param>
        /// <param name="dbFilePath">The database file path.</param>
        /// <param name="indexPath">The index path.</param>
        public ContentCollection(string name, string dbFilePath, string indexPath)
        {            
            _name = name;
            _dbFilePath = dbFilePath;
            _indexPath = indexPath;
            if (!Directory.Exists(_indexPath))
                Directory.CreateDirectory(_indexPath);

            _storage = new SQLiteContentStorage(_dbFilePath, _name);
            _schema = ContentSchema.CreateDefault();
            _luceneIndex = new LuceneIndex(_indexPath, _schema);
        }

        /// <summary>
        /// Inserts the specified Content into the ContentCollection
        /// </summary>
        /// <param name="content">The Content object to insert</param>
        /// <returns></returns>
        public async Task<Guid> Insert(Content content)
        {
            var guid = await _storage.InsertAsync(content);
            _luceneIndex.Insert(content);

            return guid;
        }

        /// <summary>
        /// Searches the ContentCollection for Contents that match the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns></returns>
        public async Task<SearchResult<Content>> Search(SearchCriteria criteria)
        {              
            var luceneResult = _luceneIndex.Search(criteria);
            var searchResult = new SearchResult<Content>(criteria, luceneResult);            

            if (searchResult.HitCount > 0)            
                searchResult.Items = await _storage.GetAsync(luceneResult.Items.ToList());            

            return searchResult; 
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _luceneIndex.Dispose();
        }
    }
}
