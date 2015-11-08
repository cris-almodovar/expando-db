using FlexLucene.Analysis;
using FlexLucene.Index;
using FlexLucene.Queryparser.Classic;
using FlexLucene.Search;
using FlexLucene.Store;
using java.nio.file;
using System;
using System.Collections.Generic;
using System.Text;
using LuceneFieldType = FlexLucene.Document.FieldType;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents the Lucene full-text index for a Collection of dynamic Contents objects
    /// </summary>
    public class LuceneIndex : IDisposable
    {
        private const int DEFAULT_SEARCH_LIMIT = 1000;
        private readonly Directory _indexDirectory;
        private readonly Analyzer _compositeAnalyzer;
        private readonly IndexWriter _writer;
        private readonly QueryParser _queryParser;        
        private readonly SearcherManager _searcherManager;        
        private readonly System.Timers.Timer _refreshTimer;
        private Func<IndexSchema> _getSearchSchema;     

        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneIndex"/> class.
        /// </summary>
        /// <param name="indexPath">The path to the directory that will contain the index files.</param>
        /// <param name="getSearchSchema">Returns the IndexShema for the full-text index.</param>        
        public LuceneIndex(string indexPath, Func<IndexSchema> getSearchSchema)
        {
            if (String.IsNullOrWhiteSpace(indexPath))
                throw new ArgumentNullException("indexPath");    
            if (getSearchSchema == null)
                throw new ArgumentNullException("getIndexSchema");

            if (!System.IO.Directory.Exists(indexPath))
                System.IO.Directory.CreateDirectory(indexPath);

            var path = Paths.get(indexPath);
            _indexDirectory = new MMapDirectory(path);

            _getSearchSchema = getSearchSchema;           
            _compositeAnalyzer = new CompositeAnalyzer(_getSearchSchema);

            var config = new IndexWriterConfig(_compositeAnalyzer);
            _writer = new IndexWriter(_indexDirectory, config);
            _searcherManager = new SearcherManager(_writer, true, null);

            _queryParser = new QueryParser(LuceneField.FULL_TEXT_FIELD_NAME, _compositeAnalyzer);            

            _refreshTimer = new System.Timers.Timer
            {
                Interval = 1000,  
                AutoReset = true,
                Enabled = true
            };

            _refreshTimer.Elapsed += OnRefreshTimerElapsed;
            _refreshTimer.Start();

        }

        /// <summary>
        /// Refreshes the Lucene index so that Search() reflects the latest insertions and deletions.
        /// </summary>
        /// <remarks>
        /// The index refreshes itself automatically every second.
        /// </remarks>
        public void Refresh()
        {
            _searcherManager.MaybeRefresh();
        }

        private void OnRefreshTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Refresh();
        }

        /// <summary>
        /// Inserts a dynamic content into the index.
        /// </summary>
        /// <param name="content">The dynamic content.</param>        
        public void Insert(Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var searchSchema = _getSearchSchema();
            var document = content.ToLuceneDocument(searchSchema);
            
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
        /// Updates the Content identified by its _id field.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <exception cref="ArgumentNullException">content</exception>
        public void Update(Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var searchSchema = _getSearchSchema();
            var document = content.ToLuceneDocument(searchSchema);

            var id = content._id.ToString();
            var idTerm = new Term("_id", id);
            
            _writer.UpdateDocument(idTerm, document);
            _writer.Commit();
        }

        public IEnumerable<Guid> Search(string queryString, string sortByField = null)
        {
            var query = _queryParser.Parse(queryString);            

            var searcher = _searcherManager.Acquire() as IndexSearcher;
            if (searcher != null)
            {
                try
                {
                    var sort = GetSortCriteria(sortByField);
                    var topDocs = searcher.Search(query, 1000000, sort);
                    var hitCount = topDocs.TotalHits; // > context.MaxItems.Value ? context.MaxItems.Value : topDocs.TotalHits;

                    if (topDocs.TotalHits > 0)
                    {
                        foreach (var sd in topDocs.ScoreDocs)
                        {
                            var doc = searcher.Doc(sd.Doc);
                            if (doc != null)
                            {
                                var idField = doc.GetField(LuceneField.ID_FIELD_NAME);                                
                                var bytes = idField.binaryValue().Bytes;
                                var guid = Guid.Parse(Encoding.UTF8.GetString(bytes));                                
                                yield return guid;
                            }
                        }
                    }
                }                
                finally
                {
                    _searcherManager.Release(searcher);
                }
            }                     
        }

        private Sort GetSortCriteria(string sortByField = null)
        {
            if (String.IsNullOrWhiteSpace(sortByField))
                return Sort.RELEVANCE;

            var descending = sortByField.TrimStart().StartsWith("-", StringComparison.InvariantCulture);
            var sortField = new SortField(sortByField, SortField.Type.STRING, descending);
            return new Sort(sortField);
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

            _refreshTimer.Elapsed -= OnRefreshTimerElapsed;
            _refreshTimer.Stop();
            _refreshTimer.Dispose();

            _getSearchSchema = null;
        }
    }
}
