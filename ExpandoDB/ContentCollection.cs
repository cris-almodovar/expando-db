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
        private readonly IndexSchema _indexSchema;
        private readonly string _name;
        /// <summary>
        /// Gets the name of the ContentCollection.
        /// </summary>
        /// <value>
        /// The name of the ContentCollection
        /// </value>
        public string Name { get { return _name; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentCollection" /> class.
        /// </summary>
        /// <param name="name">The name of the ContentCollection.</param>
        /// <param name="dbPath">The path to the db folder.</param>
        /// <param name="indexSchema">The index schema.</param>
        public ContentCollection(string name, string dbPath, IndexSchema indexSchema = null)
        {            
            _name = name;

            if (!Directory.Exists(dbPath))
                Directory.CreateDirectory(dbPath);

            _dbFilePath = Path.Combine(dbPath, Database.DB_FILENAME);

            _indexPath = Path.Combine(dbPath, Database.INDEX_DIR_NAME, name);
            if (!Directory.Exists(_indexPath))
                Directory.CreateDirectory(_indexPath);

            _storage = new SQLiteContentStorage(_dbFilePath, _name);

            _indexSchema = indexSchema ?? IndexSchema.CreateDefault();
            _indexSchema.Name = name;
            _luceneIndex = new LuceneIndex(_indexPath, _indexSchema);
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
        public async Task<SearchResult<Content>> SearchAsync(SearchCriteria criteria)
        {              
            var luceneResult = _luceneIndex.Search(criteria);
            var searchResult = new SearchResult<Content>(criteria, luceneResult);            

            if (searchResult.HitCount > 0)            
                searchResult.Items = await _storage.GetAsync(luceneResult.Items.ToList());            

            return searchResult; 
        }

        /// <summary>
        /// Counts the Contents that match the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns></returns>
        public int Count(SearchCriteria criteria)
        {
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
            var affected = await _storage.UpdateAsync(content);
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
            var affected = await _storage.DeleteAsync(guid);
            if (affected > 0)
                _luceneIndex.Delete(guid);

            return affected;
        }

        /// <summary>
        /// Drops this Collection.
        /// </summary>
        /// <returns></returns>
        public async Task DropAsync()
        {
            await _storage.DropAsync();            
            _luceneIndex.Dispose();
            await Task.Delay(500);
            Directory.Delete(_indexPath, true);
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
