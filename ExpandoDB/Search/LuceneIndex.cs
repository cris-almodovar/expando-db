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
using FlexLucene.Codecs.Lucene60;
using FlexLucene.Document;
using FlexLucene.Util;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents the Lucene full-text index for a Collection of Documents objects
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class LuceneIndex : IDisposable
    {       
        private readonly Directory _indexDirectory;
        private readonly Directory _taxonomyDirectory;
        private readonly Analyzer _compositeAnalyzer;
        private readonly IndexWriter _indexWriter;
        private readonly DirectoryTaxonomyWriter _taxonomyWriter;        
        private readonly LuceneFacetBuilder _facetBuilder;
        private readonly SearcherTaxonomyManager _searcherTaxonomyManager;        
        private readonly Timer _refreshTimer;
        private readonly Timer _commitTimer;        
        private readonly ManualResetEventSlim _writeAllowedFlag;
        private readonly ILog _log = LogManager.GetLogger(typeof(LuceneIndex).Name);    
        private readonly double _refreshIntervalSeconds;
        private readonly double _commitIntervalSeconds;        
        private readonly double _ramBufferSizeMB;
        private const string TAXONOMY_INDEX_NAME = "facets";

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
        /// Gets the analyzer.
        /// </summary>
        /// <value>
        /// The analyzer.
        /// </value>
        public Analyzer Analyzer { get { return _compositeAnalyzer; } }

        /// <summary>
        /// Gets the searcher taxonomy manager.
        /// </summary>
        /// <value>
        /// The searcher taxonomy manager.
        /// </value>
        internal SearcherTaxonomyManager SearcherTaxonomyManager {  get { return _searcherTaxonomyManager;  } }


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

            var taxonomyIndexPath = System.IO.Path.Combine(IndexPath, TAXONOMY_INDEX_NAME);
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
           
            _writeAllowedFlag = new ManualResetEventSlim(true);

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
                try
                {
                    // Don't allow index writes while we are committing 
                    // to the main index and taxonomy index.
                    _writeAllowedFlag.Reset();                    

                    _taxonomyWriter.Commit();
                    _indexWriter.Commit();
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
                finally
                {             
                    _writeAllowedFlag.Set();
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
                document._id = DocumentId.NewGuid();

            var luceneDocument = document.ToLuceneDocument(Schema, _facetBuilder);

            _writeAllowedFlag.Wait();
            _indexWriter.AddDocument(luceneDocument);
        }

        /// <summary>
        /// Deletes the document identified by the guid.
        /// </summary>
        /// <param name="guid">The unique identifier.</param>
        public void Delete(Guid guid)
        {
            if (guid == Guid.Empty)
                throw new ArgumentException(nameof(guid) + " cannot be empty");

            var idTerm = new Term(Schema.MetadataField.ID, guid.ToString().ToLower());
            
            _writeAllowedFlag.Wait();
            _indexWriter.DeleteDocuments(idTerm);            
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
            var idTerm = new Term(Schema.MetadataField.ID, id);

            _writeAllowedFlag.Wait();
            _indexWriter.UpdateDocument(idTerm, luceneDocument);            
        }

        /// <summary>
        /// Searches the Lucene index for Documents that match the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">criteria</exception>
        /// <exception cref="ArgumentNullException"></exception>
        public SearchResult<Guid> Search(SearchCriteria criteria)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            criteria.Validate(); 
            var result = new SearchResult<Guid>(criteria);            

            var instance = _searcherTaxonomyManager.Acquire() as SearcherTaxonomyManagerSearcherAndTaxonomy;
            if (instance != null)
            {
                var searcher = instance.Searcher;
                var taxonomyReader = instance.TaxonomyReader;

                try
                {                    
                    var topN = criteria.TopN ?? SearchCriteria.DEFAULT_TOP_N;
                    if (topN == 0)
                        topN = 1;

                    var itemsPerPage = criteria.ItemsPerPage ?? SearchCriteria.DEFAULT_ITEMS_PER_PAGE;
                    var pageNumber = criteria.PageNumber ?? 1;
                    var topNFacets = criteria.TopNFacets ?? SearchCriteria.DEFAULT_TOP_N_FACETS;                    

                    var queryParser = new LuceneQueryParser(Schema.MetadataField.FULL_TEXT, _compositeAnalyzer, Schema);
                    var queryString = String.IsNullOrWhiteSpace(criteria.Query) ? LuceneQueryParser.ALL_DOCS_QUERY : criteria.Query;
                    var query = queryParser.Parse(queryString);

                    var sort = queryParser.GetSortCriteria(criteria.SortByFields, Schema);
                    var facetFilters = criteria.FacetFilters.ToLuceneFacetFields(Schema);
                    var topDocs = (TopDocs)null;
                    var facets = (IEnumerable<FacetValue>)null; 

                    if (facetFilters.Count() == 0)
                    {
                        // We are NOT going to do a drill-down on specific Facets.
                        // Instead we will take the top N Facet values from all Facets.

                        var facetsCollector = new FacetsCollector();

                        // Get the matching Documents
                        topDocs = FacetsCollector.Search(searcher, query, topN, sort, facetsCollector);

                        if (topNFacets > 0)
                        {
                            // Get the Facet counts from the matching Documents
                            var facetCounts = new FastTaxonomyFacetCounts(taxonomyReader, _facetBuilder.FacetsConfig, facetsCollector);
                            facets = facetCounts.ToFacetValues(topNFacets);
                        }
                    }
                    else
                    {
                        // Perform a drill-down / drill-sideways query
                        var drillDownQuery = new DrillDownQuery(_facetBuilder.FacetsConfig, query);
                        foreach (var facetField in facetFilters)
                            drillDownQuery.Add(facetField.Dim, facetField.Path);                        

                        var drillSideways = new DrillSideways(searcher, _facetBuilder.FacetsConfig, taxonomyReader);
                        var drillSidewaysResult = drillSideways.Search(drillDownQuery, null, null, topN, sort, false, false);

                        // Get the matching documents
                        topDocs = drillSidewaysResult.Hits;

                        if (topNFacets > 0)
                        {
                            // Get the Facet counts from the matching Documents
                            facets = drillSidewaysResult.Facets.ToFacetValues(topNFacets, facetFilters);
                        }
                    }

                    if (criteria.TopN == 0)
                        topDocs.ScoreDocs = new ScoreDoc[0];                    

                    // TODO: Don't pass TopDocs; pass an IEnumerable<Guid>
                    result.PopulateWith(topDocs, facets, id => searcher.Doc(id));                    
                }
                finally
                {
                    _searcherTaxonomyManager.Release(instance); 
                    searcher = null;
                    taxonomyReader = null;
                }
            }

            return result;
        }

        /// <summary>
        /// Opens the cursor.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        /// <param name="autoOpen">if set to <c>true</c>, the cursor is automatically opened.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">criteria</exception>
        internal DocValuesCursor CreateCursor(CursorSearchCriteria criteria, bool autoOpen = false)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            var docIdCursor = new DocValuesCursor(criteria, this, autoOpen);
            return docIdCursor;
        }

        

        /// <summary>
        /// Gets the current total number of documents in the index.
        /// </summary>
        /// <returns></returns>
        public int GetDocumentCount()
        {
            return _indexWriter.NumDocs();                            
        }

        /// <summary>
        /// Deletes all documents from the Lucene index.
        /// </summary>
        /// <returns></returns>
        public void Truncate()
        {
            if (!IsDisposed)
                _indexWriter.DeleteAll();
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
                    _taxonomyDirectory.Close();

                    _indexWriter.Close();                    
                    _indexDirectory.Close();

                    _writeAllowedFlag.Dispose();
                }               

                IsDisposed = true;
            }
        }


        /// <summary>
        /// Closes the <see cref="LuceneIndex"/> and deallocates resources.
        /// </summary>
        public void Dispose()
        {           
            Dispose(true);            
        }
        #endregion
    }
}
