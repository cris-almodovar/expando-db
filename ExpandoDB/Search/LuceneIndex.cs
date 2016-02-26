using Common.Logging;
using FlexLucene.Analysis;
using FlexLucene.Index;
using FlexLucene.Search;
using FlexLucene.Store;
using java.nio.file;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents the Lucene full-text index for a Collection of dynamic Contents objects
    /// </summary>
    public class LuceneIndex : IDisposable
    {
        public const int DEFAULT_SEARCH_TOP_N = 100000;
        public const int DEFAULT_SEARCH_ITEMS_PER_PAGE = 10;
        private const string ALL_DOCS_QUERY = "*:*";
        private readonly Directory _indexDirectory;
        private readonly Analyzer _compositeAnalyzer;
        private readonly IndexWriter _writer;                
        private readonly SearcherManager _searcherManager;        
        private readonly Timer _refreshTimer;
        private readonly Timer _commitTimer;
        private readonly IndexSchema _indexSchema;
        private readonly ILog _log = LogManager.GetLogger(typeof(LuceneIndex).Name);    
        private readonly double _refreshIntervalSeconds;
        private readonly double _commitIntervalSeconds;
        private readonly long _writerLockTimeoutMilliseconds;
        private readonly double _ramBufferSizeMB;

        public IndexSchema Schema { get { return _indexSchema; } }       


        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneIndex"/> class.
        /// </summary>
        /// <param name="indexPath">The path to the directory that will contain the Lucene index files.</param>
        public LuceneIndex(string indexPath, IndexSchema indexSchema = null)
        {
            if (String.IsNullOrWhiteSpace(indexPath))
                throw new ArgumentNullException(nameof(indexPath)); 
            if (indexSchema == null)
                indexSchema = IndexSchema.CreateDefault();

            if (!System.IO.Directory.Exists(indexPath))
                System.IO.Directory.CreateDirectory(indexPath);

            var path = Paths.get(indexPath);
            _indexDirectory = new MMapDirectory(path);

            _indexSchema = indexSchema ?? IndexSchema.CreateDefault();
            _compositeAnalyzer = new CompositeAnalyzer(_indexSchema);

            _ramBufferSizeMB = Double.Parse(ConfigurationManager.AppSettings["LuceneRAMBufferSizeMB"] ?? "64");
            _writerLockTimeoutMilliseconds = Convert.ToInt64(Double.Parse(ConfigurationManager.AppSettings["LuceneWriterLockTimeoutSeconds"] ?? "1") * 1000);            

            var config = new IndexWriterConfig(_compositeAnalyzer)
                            .SetRAMBufferSizeMB(_ramBufferSizeMB)
                            .SetWriteLockTimeout(_writerLockTimeoutMilliseconds)
                            .SetCommitOnClose(true);
            
            _writer = new IndexWriter(_indexDirectory, config);            

            _searcherManager = new SearcherManager(_writer, true, null);            

            _refreshIntervalSeconds = Double.Parse(ConfigurationManager.AppSettings["LuceneRefreshIntervalSeconds"] ?? "0.5");    
            _commitIntervalSeconds = Double.Parse(ConfigurationManager.AppSettings["LuceneCommitIntervalSeconds"] ?? "60");

            _refreshTimer = new Timer(o => Refresh(), null, TimeSpan.FromSeconds(_refreshIntervalSeconds), TimeSpan.FromSeconds(_refreshIntervalSeconds));
            _commitTimer = new Timer(o => Commit(), null, TimeSpan.FromSeconds(_commitIntervalSeconds), TimeSpan.FromSeconds(_commitIntervalSeconds));
        }

        /// <summary>
        /// Commits the latest insertions and deletions to the on-disk Lucene index.
        /// </summary>  
        /// <remarks>
        /// For performance, the <see cref="LuceneIndex"/> object indexes data using an in-memory buffer instead of writing directly to disk.
        /// <para>
        /// The <see cref="LuceneIndex"/> object auto-invokes this method every N seconds, where N is the value of the <b>LuceneCommitIntervalSeconds</b> config item.
        /// </para>
        /// </remarks>      
        public void Commit()
        {
            try
            {
                if (_writer.HasUncommittedChanges())
                    _writer.Commit();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

        }

        /// <summary>
        /// Refreshes the Lucene index so that the Search operation reflects the latest insertions and deletions.
        /// </summary>
        /// <remarks>
        /// The <see cref="LuceneIndex"/> object auto-invokes this method every N seconds, where N is the value of the <b>LuceneRefreshIntervalSeconds</b> config item.
        /// </remarks>
        public void Refresh()
        {
            try
            {  
                _searcherManager.MaybeRefresh();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        /// <summary>
        /// Inserts a dynamic content into the index.
        /// </summary>
        /// <param name="content">The dynamic content.</param>        
        public void Insert(Content content)
        {            
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (content._id == null || content._id.Value == Guid.Empty)
                content._id = Guid.NewGuid();

            var document = content.ToLuceneDocument(_indexSchema);
            _writer.AddDocument(document);
        }

        /// <summary>
        /// Deletes the content identified by the guid.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        public void Delete(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException(nameof(guid) + " cannot be empty");

            var idTerm = new Term(Content.ID_FIELD_NAME, guid.ToString());
            _writer.DeleteDocuments(idTerm);
        }

        /// <summary>
        /// Updates the Content identified by its _id field.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <exception cref="ArgumentNullException">content</exception>
        public void Update(Content content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (content._id == null || content._id == Guid.Empty)
                throw new InvalidOperationException("Cannot update Content that does not have an _id");

            var document = content.ToLuceneDocument(_indexSchema);
            var id = content._id.ToString();
            var idTerm = new Term(Content.ID_FIELD_NAME, id);

            _writer.UpdateDocument(idTerm, document);
        }        

        /// <summary>
        /// Searches the Lucene index for Contents that match the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns></returns>
        public SearchResult<Guid> Search(SearchCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            criteria.Query = String.IsNullOrWhiteSpace(criteria.Query) ? ALL_DOCS_QUERY : criteria.Query;
            criteria.TopN = criteria.TopN > 0 ? criteria.TopN : DEFAULT_SEARCH_TOP_N;
            criteria.ItemsPerPage = criteria.ItemsPerPage > 0 ? criteria.ItemsPerPage : DEFAULT_SEARCH_ITEMS_PER_PAGE;
            criteria.PageNumber = criteria.PageNumber > 0 ? criteria.PageNumber : 1;
            criteria.Validate();

            var result = new SearchResult<Guid>(criteria);
            var queryParser = new LuceneQueryParser(LuceneFieldExtensions.FULL_TEXT_FIELD_NAME, _compositeAnalyzer, _indexSchema);
            var query = queryParser.Parse(criteria.Query);

            var searcher = _searcherManager.Acquire() as IndexSearcher;
            if (searcher != null)
            {
                try
                {
                    var sort = GetSortCriteria(criteria.SortByField);
                    var topFieldDocs = searcher.Search(query, criteria.TopN, sort);
                    result.PopulateWith(topFieldDocs, id => searcher.Doc(id));
                }
                finally
                {
                    _searcherManager.Release(searcher); 
                    searcher = null;
                }
            }

            return result;
        }

        private Sort GetSortCriteria(string sortByField = null)
        {
            if (String.IsNullOrWhiteSpace(sortByField))
                return Sort.RELEVANCE;

            var sortFields = new List<SortField>();            
            var sortFieldName = sortByField.Trim().TrimStart('+');
            var reverse = sortFieldName.StartsWith("-", StringComparison.InvariantCulture);
            if (reverse)
                sortFieldName = sortFieldName.TrimStart('-');

            sortFields.Add(new SortField(sortFieldName, SortFieldType.STRING, reverse));            

            return new Sort(sortFields.ToArray());
        }       

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _refreshTimer.Dispose();
            _commitTimer.Dispose();

            _searcherManager.Close();

            if (_writer.HasUncommittedChanges())
                _writer.Commit();

            _writer.Close();
                  
        }
    }
}
