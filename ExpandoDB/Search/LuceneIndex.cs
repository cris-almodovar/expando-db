using FlexLucene.Analysis;
using FlexLucene.Index;
using FlexLucene.Queryparser.Classic;
using FlexLucene.Search;
using FlexLucene.Store;
using java.nio.file;
using System;
using System.Collections.Generic;
using LuceneFieldType = FlexLucene.Document.FieldType;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents the Lucene full-text index for a Collection of dynamic Contents objects
    /// </summary>
    public class LuceneIndex : IDisposable
    {
        public const string FULL_TEXT_FIELD = "_full_text";
        private const int DEFAULT_SEARCH_LIMIT = 1000;
        private readonly Directory _indexDirectory;
        private readonly Analyzer _compositeAnalyzer;
        private readonly IndexWriter _writer;
        private readonly QueryParser _queryParser;        
        private readonly SearcherManager _searcherManager;        
        private readonly System.Timers.Timer _refreshTimer;
        private Func<SearchSchema> _getSearchSchema;
        public static readonly LuceneFieldType TEXT_FIELD_TYPE;
        public static readonly LuceneFieldType ID_FIELD_TYPE;
        public static readonly LuceneFieldType STRING_FIELD_TYPE;
        public static readonly LuceneFieldType NUMERIC_FIELD_TYPE;
        public static readonly LuceneFieldType DATE_FIELD_TYPE;

        /// <summary>
        /// Initializes the <see cref="LuceneIndex"/> class.
        /// </summary>
        static LuceneIndex()
        {
            TEXT_FIELD_TYPE = new LuceneFieldType();
            TEXT_FIELD_TYPE.SetValues(true, true, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS, false, false, DocValuesType.NONE);

            ID_FIELD_TYPE = new LuceneFieldType();
            ID_FIELD_TYPE.SetValues(false, true, IndexOptions.DOCS, true, false, DocValuesType.SORTED);

            STRING_FIELD_TYPE = new LuceneFieldType();
            STRING_FIELD_TYPE.SetValues(false, true, IndexOptions.DOCS, false, false, DocValuesType.SORTED);

            NUMERIC_FIELD_TYPE = new LuceneFieldType();
            NUMERIC_FIELD_TYPE.SetValues(false, true, IndexOptions.DOCS, false, false, DocValuesType.SORTED);

            DATE_FIELD_TYPE = new LuceneFieldType();
            DATE_FIELD_TYPE.SetValues(false, true, IndexOptions.DOCS, false, false, DocValuesType.SORTED);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneIndex"/> class.
        /// </summary>
        /// <param name="indexPath">The path to the directory that will contain the index files.</param>
        /// <param name="getSearchSchema">Returns the IndexShema for the full-text index.</param>        
        public LuceneIndex(string indexPath, Func<SearchSchema> getSearchSchema)
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

            _getSearchSchema = null;
        }
    }
}
