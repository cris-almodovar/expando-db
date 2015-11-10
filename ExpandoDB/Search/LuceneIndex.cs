using FlexLucene.Analysis;
using FlexLucene.Index;
using FlexLucene.Queryparser.Classic;
using FlexLucene.Search;
using FlexLucene.Store;
using java.nio.file;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LuceneFieldType = FlexLucene.Document.FieldType;
using System.Threading;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents the Lucene full-text index for a Collection of dynamic Contents objects
    /// </summary>
    public class LuceneIndex : IDisposable
    {
        private const int DEFAULT_SEARCH_TOP_N = 100000;
        private const int DEFAULT_SEARCH_ITEMS_PER_PAGE = 10;
        private readonly Directory _indexDirectory;
        private readonly Analyzer _compositeAnalyzer;
        private readonly IndexWriter _writer;
        private readonly QueryParser _queryParser;
        private readonly IndexSchema _indexSchema;
        private SearcherManager _searcherManager;
        private object _searcherManagerLock = new object();
        private readonly System.Timers.Timer _refreshTimer;            

        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneIndex"/> class.
        /// </summary>
        /// <param name="indexPath">The path to the directory that will contain the index files.</param>                
        public LuceneIndex(string indexPath, IndexSchema indexSchema = null)
        {
            if (String.IsNullOrWhiteSpace(indexPath))
                throw new ArgumentNullException("indexPath");
            if (indexSchema == null)
                indexSchema = IndexSchema.CreateDefault();

            if (!System.IO.Directory.Exists(indexPath))
                System.IO.Directory.CreateDirectory(indexPath);

            var path = Paths.get(indexPath);
            _indexDirectory = new MMapDirectory(path);

            _indexSchema = indexSchema;
            _compositeAnalyzer = new CompositeAnalyzer(_indexSchema);

            var config = new IndexWriterConfig(_compositeAnalyzer);
            _writer = new IndexWriter(_indexDirectory, config);
            
            InitializeSearcherManager();            

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

        public IndexSchema IndexSchema { get { return _indexSchema; } }
        
        /// <summary>
        /// Refreshes the Lucene index so that Search() reflects the latest insertions and deletions.
        /// </summary>
        /// <remarks>
        /// The index refreshes itself automatically every second.
        /// </remarks>
        public void Refresh()
        {
            var searcherManager = GetSearcherManager();
            searcherManager.MaybeRefresh();
        }

        private void OnRefreshTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Refresh();
            }
            catch { }
        }

        /// <summary>
        /// Inserts a dynamic content into the index.
        /// </summary>
        /// <param name="content">The dynamic content.</param>        
        public void Insert(Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");
            
            var document = content.ToLuceneDocument(_indexSchema);
            
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

            if (content._id == null || content._id == Guid.Empty)
                throw new InvalidOperationException("Cannot update Content that does not have an _id");
            
            var document = content.ToLuceneDocument(_indexSchema);

            var id = content._id.ToString();
            var idTerm = new Term("_id", id);
            
            _writer.UpdateDocument(idTerm, document);
            _writer.Commit();
        }

        public SearchResult<Guid> Search(SearchCriteria criteria)
        {
            criteria.TopN = criteria.TopN ?? DEFAULT_SEARCH_TOP_N;
            criteria.ItemsPerPage = criteria.ItemsPerPage ?? DEFAULT_SEARCH_ITEMS_PER_PAGE;
            criteria.PageNumber = criteria.PageNumber ?? 1;

            if (criteria.TopN <= 0)
                throw new ArgumentException("topN cannot be <= zero");
            if (criteria.ItemsPerPage <= 0)
                throw new ArgumentException("itemsPerPage cannot be <= zero");
            if (criteria.PageNumber <= 0)
                throw new ArgumentException("pageNumber cannot be <= zero");

            var result = new SearchResult<Guid>(criteria)
            {
                HitCount = 0,
                TotalHitCount = 0,
                PageCount = 0,
                Items = new List<Guid>()
            };   

            var query = _queryParser.Parse(criteria.Query);

            var searcherManager = GetSearcherManager();
            if (searcherManager == null)
            {
                Thread.SpinWait(10); // Avoid a context switch - we know the searcherManager will be available soon!
                searcherManager = GetSearcherManager();
            }

            var searcher = searcherManager.Acquire() as IndexSearcher;
            if (searcher != null)
            {
                try
                {
                    var sort = GetSortCriteria(criteria.SortByField);
                    
                    var topFieldDocs = searcher.Search(query, criteria.TopN.Value, sort); 

                    // Check if Search() returned more than topN matching items;                    
                    result.HitCount = topFieldDocs.ScoreDocs.Length;
                    result.TotalHitCount = topFieldDocs.TotalHits;                 

                    if (result.HitCount > 0)
                    {
                        var itemsToSkip = (criteria.PageNumber.Value - 1) * criteria.ItemsPerPage.Value;
                        var itemsToTake = criteria.ItemsPerPage.Value;

                        var scoreDocsForCurrentPage = 
                                topFieldDocs.ScoreDocs
                                            .Skip(itemsToSkip)
                                            .Take(itemsToTake)
                                            .ToList();

                        var contentIds = new List<Guid>();
                        for (var i = 0; i < scoreDocsForCurrentPage.Count; i++)
                        {
                            var sd = scoreDocsForCurrentPage[i];
                            var doc = searcher.Doc(sd.Doc);
                            if (doc == null)
                                continue;

                            var idField = doc.GetField(LuceneField.ID_FIELD_NAME);
                            var idValue = idField.stringValue();

                            contentIds.Add(Guid.Parse(idValue));
                        }

                        result.Items = contentIds;
                        result.PageCount = ComputePageCount(result.HitCount.Value, criteria.ItemsPerPage.Value);                        
                    }                    
                                        
                }
                catch (AlreadyClosedException)  
                {
                    InitializeSearcherManager();                    
                }                
                finally
                {
                    searcherManager.Release(searcher);
                }
            }

            return result;
        }

        private int ComputePageCount(int hitCount, int itemsPerPage)
        {
            var pageCount = 0;
            if (hitCount > 0 && itemsPerPage > 0)
            {
                pageCount = hitCount / itemsPerPage;
                var remainder = hitCount % itemsPerPage;
                if (remainder > 0)
                    pageCount += 1;
            }

            return pageCount;
        }

        private SearcherManager GetSearcherManager()
        {
            if (_searcherManager == null)
                InitializeSearcherManager();

            return _searcherManager;
        }

        private void InitializeSearcherManager()
        {
            lock (_searcherManagerLock)
            {
                _searcherManager = new SearcherManager(_writer, true, null);
            }
        }

        private Sort GetSortCriteria(string sortByField = null)
        {
            if (String.IsNullOrWhiteSpace(sortByField))
                return Sort.RELEVANCE;

            var sortFields = new List<SortField>();            
            var sortFieldName = sortByField.Trim();
            var reverse = sortFieldName.StartsWith("-", StringComparison.InvariantCulture);
            sortFields.Add(new SortField(sortFieldName, SortField.Type.STRING, reverse));            

            return new Sort(sortFields.ToArray());
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
        }
    }
}
