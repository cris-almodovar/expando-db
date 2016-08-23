using Common.Logging;
using FlexLucene.Analysis;
using FlexLucene.Facet;
using FlexLucene.Facet.Taxonomy;
using FlexLucene.Facet.Taxonomy.Directory;
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
using System.Linq;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents the Lucene full-text index for a Collection of Documents objects
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class LuceneIndex : IDisposable
    {        
        private const string ALL_DOCS_QUERY = "*:*";        
        private readonly Directory _indexDirectory;
        private readonly Directory _taxonomyDirectory;
        private readonly Analyzer _compositeAnalyzer;
        private readonly IndexWriter _indexWriter;
        private readonly DirectoryTaxonomyWriter _taxonomyWriter;        
        private readonly LuceneFacetBuilder _facetBuilder;
        private readonly SearcherTaxonomyManager _searcherTaxonomyManager;        
        private readonly Timer _refreshTimer;
        private readonly Timer _commitTimer;
        private readonly ReaderWriterLockSlim _indexWriterLock;       
        private readonly ILog _log = LogManager.GetLogger(typeof(LuceneIndex).Name);    
        private readonly double _refreshIntervalSeconds;
        private readonly double _commitIntervalSeconds;        
        private readonly double _ramBufferSizeMB;

        /// <summary>
        /// Gets the schema of the Documents in the Lucene index.
        /// </summary>
        /// <value>
        /// The schema.
        /// </value>
        public Schema Schema { get; private set; }

        /// <summary>
        /// Gets the Lucene index path.
        /// </summary>
        /// <value>
        /// The index path.
        /// </value>
        public string IndexPath { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneIndex" /> class.
        /// </summary>
        /// <param name="indexPath">The path to the directory that will contain the Lucene index files.</param>
        /// <param name="schema">The schema.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public LuceneIndex(string indexPath, Schema schema)
        {
            if (String.IsNullOrWhiteSpace(indexPath))
                throw new ArgumentNullException(nameof(indexPath)); 
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            IndexPath = indexPath;
            Schema = schema;

            if (System.IO.Directory.Exists(IndexPath))
            {
                if (Schema.IsDefault())
                    throw new InvalidOperationException($"There is an existing index on '{IndexPath}'.");
            }                
            else
            {
                System.IO.Directory.CreateDirectory(IndexPath);
            }                        

            _indexDirectory = new MMapDirectory(Paths.get(IndexPath));

            var taxonomyIndexPath = System.IO.Path.Combine(IndexPath, "taxonomy");
            if (!System.IO.Directory.Exists(taxonomyIndexPath))            
                System.IO.Directory.CreateDirectory(taxonomyIndexPath);

            _taxonomyDirectory = new MMapDirectory(Paths.get(taxonomyIndexPath));         
                           
            _compositeAnalyzer = new CompositeAnalyzer(Schema);            

            _ramBufferSizeMB = Double.Parse(ConfigurationManager.AppSettings["IndexWriter.RAMBufferSizeMB"] ?? "128");            

            var config = new IndexWriterConfig(_compositeAnalyzer)
                            .SetOpenMode(IndexWriterConfigOpenMode.CREATE_OR_APPEND)
                            .SetRAMBufferSizeMB(_ramBufferSizeMB)                            
                            .SetCommitOnClose(true);
            
            _indexWriter = new IndexWriter(_indexDirectory, config);
            _taxonomyWriter = new DirectoryTaxonomyWriter(_taxonomyDirectory, IndexWriterConfigOpenMode.CREATE_OR_APPEND);

            _searcherTaxonomyManager = new SearcherTaxonomyManager(_indexWriter, true, null, _taxonomyWriter);            
            _facetBuilder = new LuceneFacetBuilder(_taxonomyWriter);                        

            _refreshIntervalSeconds = Double.Parse(ConfigurationManager.AppSettings["IndexSearcher.RefreshIntervalSeconds"] ?? "0.5");    
            _commitIntervalSeconds = Double.Parse(ConfigurationManager.AppSettings["IndexWriter.CommitIntervalSeconds"] ?? "60");

            _indexWriterLock = new ReaderWriterLockSlim();
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
            if (_indexWriter.HasUncommittedChanges())
            {
                if (!_indexWriterLock.TryEnterWriteLock(100))
                    return;

                try
                {                    
                    _taxonomyWriter.Commit();
                    _indexWriter.Commit();
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
                finally
                {
                    _indexWriterLock.ExitWriteLock();
                }
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
                _searcherTaxonomyManager.MaybeRefresh();                    
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

            var luceneDocument = document.ToLuceneDocument(Schema, _facetBuilder);

            _indexWriterLock.EnterWriteLock();
            try
            {                
                _indexWriter.AddDocument(luceneDocument);
            }
            finally
            {
                _indexWriterLock.ExitWriteLock();
            }   
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

            _indexWriterLock.EnterWriteLock();
            try
            {
                _indexWriter.DeleteDocuments(idTerm);
            }
            finally
            {
                _indexWriterLock.ExitWriteLock();
            }
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

            var luceneDocument = document.ToLuceneDocument(Schema, _facetBuilder);
            var id = document._id.ToString().ToLower();
            var idTerm = new Term(Schema.StandardField.ID, id);

            _indexWriterLock.EnterWriteLock();
            try
            {
                _indexWriter.UpdateDocument(idTerm, luceneDocument);
            }
            finally
            {
                _indexWriterLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Searches the Lucene index for Documents that match the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentNullException"></exception>
        public SearchResult<Guid> Search(SearchCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            criteria.Query = String.IsNullOrWhiteSpace(criteria.Query) ? ALL_DOCS_QUERY : criteria.Query;
            criteria.TopN = criteria.TopN > 0 ? criteria.TopN : SearchCriteria.DEFAULT_TOP_N;
            criteria.ItemsPerPage = criteria.ItemsPerPage > 0 ? criteria.ItemsPerPage : SearchCriteria.DEFAULT_ITEMS_PER_PAGE;
            criteria.PageNumber = criteria.PageNumber > 0 ? criteria.PageNumber : 1;
            criteria.Validate();

            var result = new SearchResult<Guid>(criteria);
            var queryParser = new LuceneQueryParser(Schema.StandardField.FULL_TEXT, _compositeAnalyzer, Schema);
            var query = queryParser.Parse(criteria.Query);

            var instance = _searcherTaxonomyManager.Acquire() as SearcherTaxonomyManagerSearcherAndTaxonomy;
            if (instance != null)
            {
                var searcher = instance.Searcher;
                var taxonomyReader = instance.TaxonomyReader;

                try
                {
                    var sort = GetSortCriteria(criteria.SortByField);
                    var selectedFacets = criteria.SelectCategories.ToFacetFields();
                    var topDocs = (TopDocs)null;                                        
                    var categories = Enumerable.Empty<Category>();

                    if (selectedFacets.Count() == 0)
                    {
                        // We are not going to do a drill-down on specific facets.
                        // Instead we will just take the top N facets from the matching Documents.
                        var facetsCollector = new FacetsCollector();

                        // Get the matching Documents
                        topDocs = FacetsCollector.Search(searcher, query, criteria.TopN, sort, facetsCollector);

                        // Get the Facet counts from the matching Documents
                        var facetCounts = new FastTaxonomyFacetCounts(taxonomyReader, _facetBuilder.FacetsConfig, facetsCollector);                        
                        categories = facetCounts.GetCategories(criteria.TopNCategories);
                    }
                    else
                    {
                        // Perform a drill-sideways query
                        var drillDownQuery = new DrillDownQuery(_facetBuilder.FacetsConfig, query);
                        foreach (var facetField in selectedFacets)
                            drillDownQuery.Add(facetField.Dim, facetField.Path);                        

                        var drillSideways = new DrillSideways(searcher, _facetBuilder.FacetsConfig, taxonomyReader);
                        var drillSidewaysResult = drillSideways.Search(drillDownQuery, null, null, criteria.TopN, sort, false, false);

                        // Get the matching documents
                        topDocs = drillSidewaysResult.Hits;                        

                        // Get the Facet counts from the matching Documents
                        categories = drillSidewaysResult.Facets.GetCategories(criteria.TopNCategories, selectedFacets);
                    }

                    // TODO: Don't pass TopDocs; pass an IEnumerable<Guid> and IEnumerable<Category>
                    result.PopulateWith(topDocs, id => searcher.Doc(id));
                    result.Categories = categories;
                }
                finally
                {
                    _searcherTaxonomyManager.Release(instance); 
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

            var sortBySchemaField = Schema.FindField(fieldName, false);
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
                    sortField.SetMissingValue(isDescending ? LuceneUtils.DOUBLE_MIN_VALUE : LuceneUtils.DOUBLE_MAX_VALUE);
                    break;

                case Schema.DataType.DateTime:
                case Schema.DataType.Boolean:
                    sortField = new SortField(sortFieldName, SortFieldType.LONG, isDescending);
                    sortField.SetMissingValue(isDescending ? LuceneUtils.LONG_MIN_VALUE : LuceneUtils.LONG_MAX_VALUE);
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
        /// Drops this Lucene index
        /// </summary>
        internal async Task DropAsync()
        {
            Dispose();

            var tryCount = 0;
            while (tryCount < 3)
            {
                tryCount += 1;

                // Wait half a second before deleting the Lucene index
                await Task.Delay(500).ConfigureAwait(false);
                if (!System.IO.Directory.Exists(IndexPath))
                    break;

                System.IO.Directory.Delete(IndexPath, true);
            }

            if (System.IO.Directory.Exists(IndexPath))
                throw new Exception($"Unable to delete Lucene index directory: {IndexPath}");

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
            if (!IsDisposed)
            {
                if (disposing)
                {
                    _refreshTimer.Dispose();
                    _commitTimer.Dispose();                                      

                    if (_indexWriter.HasUncommittedChanges())
                    {
                        _taxonomyWriter.Commit();
                        _indexWriter.Commit();
                    }

                    _searcherTaxonomyManager.Close();

                    _taxonomyWriter.Close();
                    _indexWriter.Close();
                    _indexWriterLock.Dispose();             
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
