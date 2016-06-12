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
using JavaDouble = java.lang.Double;
using JavaLong = java.lang.Long;
using JavaInteger = java.lang.Integer;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents the Lucene full-text index for a Collection of Documents objects
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
        private readonly Schema _schema;
        private readonly ILog _log = LogManager.GetLogger(typeof(LuceneIndex).Name);    
        private readonly double _refreshIntervalSeconds;
        private readonly double _commitIntervalSeconds;        
        private readonly double _ramBufferSizeMB;

        public Schema Schema { get { return _schema; } }       


        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneIndex"/> class.
        /// </summary>
        /// <param name="indexPath">The path to the directory that will contain the Lucene index files.</param>
        public LuceneIndex(string indexPath, Schema schema = null)
        {
            if (String.IsNullOrWhiteSpace(indexPath))
                throw new ArgumentNullException(nameof(indexPath)); 
            if (schema == null)
                schema = Schema.CreateDefault();

            if (!System.IO.Directory.Exists(indexPath))
                System.IO.Directory.CreateDirectory(indexPath);

            var path = Paths.get(indexPath);
            _indexDirectory = new MMapDirectory(path);            

            _schema = schema ?? Schema.CreateDefault();
            _compositeAnalyzer = new CompositeAnalyzer(_schema);            

            _ramBufferSizeMB = Double.Parse(ConfigurationManager.AppSettings["IndexWriter.RAMBufferSizeMB"] ?? "128");            

            var config = new IndexWriterConfig(_compositeAnalyzer)
                            .SetOpenMode(IndexWriterConfigOpenMode.CREATE_OR_APPEND)
                            .SetRAMBufferSizeMB(_ramBufferSizeMB)                            
                            .SetCommitOnClose(true);
            
            _writer = new IndexWriter(_indexDirectory, config);            

            _searcherManager = new SearcherManager(_writer, true, false, null);         

            _refreshIntervalSeconds = Double.Parse(ConfigurationManager.AppSettings["IndexSearcher.RefreshIntervalSeconds"] ?? "0.5");    
            _commitIntervalSeconds = Double.Parse(ConfigurationManager.AppSettings["IndexWriter.CommitIntervalSeconds"] ?? "60");

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
        /// Inserts a dynamic document into the index.
        /// </summary>
        /// <param name="document">The dynamic document.</param>        
        public void Insert(Document document)
        {            
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (document._id == null || document._id.Value == Guid.Empty)
                document._id = Guid.NewGuid();

            var luceneDocument = document.ToLuceneDocument(_schema);            
            _writer.AddDocument(luceneDocument);            
        }

        /// <summary>
        /// Deletes the document identified by the guid.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        public void Delete(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException(nameof(guid) + " cannot be empty");

            var idTerm = new Term(Schema.StandardField.ID, guid.ToString().ToLower());
            _writer.DeleteDocuments(idTerm);
        }

        /// <summary>
        /// Updates the Document identified by its _id field.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <exception cref="ArgumentNullException">document</exception>
        public void Update(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            if (document._id == null || document._id == Guid.Empty)
                throw new InvalidOperationException("Cannot update Document that does not have an _id");

            var luceneDocument = document.ToLuceneDocument(_schema);
            var id = document._id.ToString().ToLower();
            var idTerm = new Term(Schema.StandardField.ID, id);

            _writer.UpdateDocument(idTerm, luceneDocument);
        }        

        /// <summary>
        /// Searches the Lucene index for Documents that match the specified search criteria.
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
            var queryParser = new LuceneQueryParser(Schema.StandardField.FULL_TEXT, _compositeAnalyzer, _schema);
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
                      
            var fieldName = sortByField.Trim().TrimStart('+');
            var isDescending = fieldName.StartsWith("-", StringComparison.InvariantCulture);
            if (isDescending)
                fieldName = fieldName.TrimStart('-');

            var sortBySchemaField = _schema.FindField(fieldName, false);
            if (sortBySchemaField == null)
                throw new LuceneQueryParserException($"Invalid sortBy field: '{fieldName}'.");

            // Notes: 
            // 1. The actual sort fieldname is different, e.g. 'fieldName' ==> '__fieldName_sort__'
            // 2. If a document does not have a value for the sort field, a default 'missing value' is assigned
            //    so that the document always appears last in the resultset.

            var sortFieldName = fieldName.ToSortFieldName();
            SortField sortField = null;

            switch (sortBySchemaField.DataType)
            {
                case Schema.DataType.Number:
                    sortField = new SortField(sortFieldName, SortFieldType.DOUBLE, isDescending);
                    sortField.SetMissingValue(isDescending ? LuceneExtensions.DOUBLE_MIN_VALUE : LuceneExtensions.DOUBLE_MAX_VALUE);
                    break;

                case Schema.DataType.DateTime:
                case Schema.DataType.Boolean:
                    sortField = new SortField(sortFieldName, SortFieldType.LONG, isDescending);
                    sortField.SetMissingValue(isDescending ? LuceneExtensions.LONG_MIN_VALUE : LuceneExtensions.LONG_MAX_VALUE);
                    break;

                case Schema.DataType.Text:
                case Schema.DataType.Guid:
                    sortField = new SortField(sortFieldName, SortFieldType.STRING, isDescending);
                    sortField.SetMissingValue(isDescending ?  SortField.STRING_FIRST : SortField.STRING_LAST );
                    break;

                default:
                    throw new LuceneQueryParserException($"Invalid sortBy field: '{fieldName}'. Only Number, DateTime, Boolean, Text, and GUID fields can be used for sorting.");
            }

            if (sortField == null)
                return Sort.RELEVANCE;
            else   
                return new Sort(new[] { sortField });
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
