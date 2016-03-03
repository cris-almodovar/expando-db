using Common.Logging;
using FlexLucene.Analysis;
using FlexLucene.Analysis.Standard;
using FlexLucene.Document;
using FlexLucene.Index;
using FlexLucene.Search;
using FlexLucene.Search.Vectorhighlight;
using FlexLucene.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Annotates a sequence of <see cref="Content"/> objects by adding a <b>_highlight</b> field;
    /// the <b>_highlight</b> field will contain the best matching text fragment from the <see cref="Content"/> 
    /// object's full-text field.
    /// </summary>
    public static class LuceneHighlighter
    {
        public const string HIGHLIGHT_FIELD_NAME = "_highlight";
        private static readonly ILog _log = LogManager.GetLogger(typeof(LuceneHighlighter).Name);

        /// <summary>
        /// Annotates the given sequence of <see cref="Content"/> objects by adding a <b>_highlight</b> field;
        /// the <b>_highlight</b> field will contain the best matching text fragment from the <see cref="Content"/> 
        /// object's full-text field.
        /// </summary>
        /// <param name="hits">The sequence of <see cref="Content"/> objects.</param>
        /// <param name="criteria">The search criteria that produced the hits.</param>
        /// <returns>
        /// The original sequence of Content objects, with a <b>_highlight</b> field added to each Content.
        /// </returns>
        public static IEnumerable<Content> GenerateHighlights(this IEnumerable<Content> hits, SearchCriteria criteria)
        {
            if (hits == null)
                throw new ArgumentNullException(nameof(hits));
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));
            if (String.IsNullOrWhiteSpace(criteria.Query))
                throw new ArgumentException("SearchCriteria.Query cannot be empty");

            var contents = hits.ToList();
            try
            {
                var indexDirectory = new RAMDirectory();
                var analyzer = new FullTextAnalyzer();
                var config = new IndexWriterConfig(analyzer);
                var writer = new IndexWriter(indexDirectory, config);

                BuidIndex(contents, writer);
                GenerateHighlights(contents, writer, criteria);

                writer.DeleteAll();
                writer.Commit();
                writer.Close();
                indexDirectory.Close();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            return contents;
        }

        private static void GenerateHighlights(IList<Content> contents, IndexWriter writer, SearchCriteria criteria)
        {
            var contentHightlightMap = contents.ToDictionary(c => c._id.ToString());

            var reader = DirectoryReader.Open(writer, true);
            var indexSchema = CreateIndexSchema();
            var queryParser = new LuceneQueryParser(LuceneFieldExtensions.FULL_TEXT_FIELD_NAME, writer.GetAnalyzer(), indexSchema);
            queryParser.SetMultiTermRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);

            var query = queryParser.Parse(criteria.Query)
                                   .Rewrite(reader);            

            var highlighter = CreateHighlighter();            
            var fieldQuery = highlighter.GetFieldQuery(query);

            var searcher = new IndexSearcher(reader);
            var topFieldDocs = searcher.Search(query, contents.Count, Sort.RELEVANCE);
            var scoreDocs = topFieldDocs.ScoreDocs;

            foreach (var sd in scoreDocs)
            {
                var bestFragment = highlighter.GetBestFragment(fieldQuery, reader, sd.Doc, LuceneFieldExtensions.FULL_TEXT_FIELD_NAME, 256);
                var document = searcher.Doc(sd.Doc);
                var docId = document.Get(Content.ID_FIELD_NAME);

                if (contentHightlightMap.ContainsKey(docId))
                {
                    var dictionary = contentHightlightMap[docId].AsDictionary();
                    dictionary[HIGHLIGHT_FIELD_NAME] = bestFragment;
                }
            }            
        }

        private static IndexSchema CreateIndexSchema()
        {
            var indexSchema = new IndexSchema();
            foreach (var indexedField in new[] { new IndexedField { Name = Content.ID_FIELD_NAME, DataType = FieldDataType.String },
                                                  new IndexedField { Name = LuceneFieldExtensions.FULL_TEXT_FIELD_NAME, DataType = FieldDataType.String }
                                                })
            {
                indexSchema.Fields.TryAdd(indexedField.Name, indexedField);
            }

            return indexSchema;
        }

        private static void BuidIndex(IEnumerable<Content> hits, IndexWriter writer)
        {
            foreach (var content in hits)
            {
                var doc = new Document();

                var idField = new StringField(Content.ID_FIELD_NAME, content._id.ToString(), FieldStore.YES);
                doc.Add(idField);

                var fullTextFieldType = new FieldType(TextField.TYPE_STORED);
                fullTextFieldType.SetStoreTermVectors(true);
                fullTextFieldType.SetStoreTermVectorOffsets(true);
                fullTextFieldType.SetStoreTermVectorPositions(true);
                var fullTextField = new Field(LuceneFieldExtensions.FULL_TEXT_FIELD_NAME, content.ToLuceneFullTextString(), fullTextFieldType);
                doc.Add(fullTextField);

                writer.AddDocument(doc);
            }
            writer.Commit();
        }

        private static FastVectorHighlighter CreateHighlighter()
        {
            var fragListBuilder = new SimpleFragListBuilder();
            var fragmentBuilder = new ScoreOrderFragmentsBuilder(BaseFragmentsBuilder.COLORED_PRE_TAGS, BaseFragmentsBuilder.COLORED_POST_TAGS);
            return new FastVectorHighlighter(true, true, fragListBuilder, fragmentBuilder);
        }        
    }
}
