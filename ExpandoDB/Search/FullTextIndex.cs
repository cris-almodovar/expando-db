using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlexLucene.Analysis;
using FlexLucene.Index;
using FlexLucene.Queryparser.Classic;
using FlexLucene.Search;
using FlexLucene.Store;
using java.nio.file;
using FlexLucene.Analysis.Miscellaneous;
using FlexLucene.Analysis.Core;
using System.Timers;
using System.Dynamic;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents the Lucene full-text index for a collection of contents.
    /// </summary>
    public class FullTextIndex : IDisposable
    {
        private const string FULL_TEXT_FIELD = "_full_text";
        private const int DEFAULT_SEARCH_LIMIT = 1000;
        private readonly Directory _indexDirectory;
        private readonly Analyzer _compositeAnalyzer;
        private readonly IndexWriter _writer;
        private readonly QueryParser _queryParser;        
        private readonly SearcherManager _searcherManager;        
        private readonly System.Timers.Timer _refreshTimer;
        private Func<IndexSchema> _getIndexSchema;

        /// <summary>
        /// Initializes a new instance of the <see cref="FullTextIndex"/> class.
        /// </summary>
        /// <param name="indexPath">The path to the directory that will contain the index files.</param>
        /// <param name="getIndexSchema">Returns the IndexShema for the full-text index.</param>        
        public FullTextIndex(string indexPath, Func<IndexSchema> getIndexSchema)
        {
            if (String.IsNullOrWhiteSpace(indexPath))
                throw new ArgumentNullException("indexPath");    
            if (getIndexSchema == null)
                throw new ArgumentNullException("getIndexSchema");

            if (!System.IO.Directory.Exists(indexPath))
                System.IO.Directory.CreateDirectory(indexPath);

            var path = Paths.get(indexPath);
            _indexDirectory = new MMapDirectory(path);

            _getIndexSchema = getIndexSchema;           
            _compositeAnalyzer = new CompositeAnalyzer(_getIndexSchema);

            var config = new IndexWriterConfig(_compositeAnalyzer);
            _writer = new IndexWriter(_indexDirectory, config);
            _searcherManager = new SearcherManager(_writer, true, null);

            _queryParser = new QueryParser(FULL_TEXT_FIELD, _compositeAnalyzer);            

            _refreshTimer = new System.Timers.Timer
            {
                Interval = 1000,  
                AutoReset = true,
                Enabled = true
            };

            _refreshTimer.Elapsed += RefreshIndexReader;
            _refreshTimer.Start();

        }

        private void RefreshIndexReader(object sender, System.Timers.ElapsedEventArgs e)
        {
            _searcherManager.MaybeRefresh();
        }

        /// <summary>
        /// Inserts a dynamic content into the index.
        /// </summary>
        /// <param name="content">The dynamic content.</param>        
        public void Insert(ExpandoObject content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var indexSchema = _getIndexSchema();
            var document = content.ToLuceneDocument(indexSchema);
            
            _writer.AddDocument(document);
            _writer.Commit();
        }

        /// <summary>
        /// Deletes the content identified by the guid.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        public void Delete(Guid guid)
        {
            var idTerm = new Term("_id", guid.ToString());
            _writer.DeleteDocuments(idTerm);
            _writer.Commit();
        }

        /// <summary>
        /// Updates the content identified by its _id field.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <exception cref="ArgumentNullException">content</exception>
        public void Update(ExpandoObject content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var indexSchema = _getIndexSchema();
            var document = content.ToLuceneDocument(indexSchema);

            var id = (content as IDictionary<string, object>)["_id"].ToString();
            var idTerm = new Term("_id", id);
            
            _writer.UpdateDocument(idTerm, document);
            _writer.Commit();
        }

        public IEnumerable<Guid> Search(string query, string sortByField = null)
        {
            return null;
        }

        public IEnumerable<Guid> Search(SearchContext context)
        {
            return null;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _searcherManager.Close();
            _writer.Close();

            _refreshTimer.Stop();
            _refreshTimer.Dispose();

            _getIndexSchema = null;
        }
    }
}
