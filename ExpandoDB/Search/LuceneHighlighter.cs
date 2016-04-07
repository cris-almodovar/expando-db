﻿using Common.Logging;
using FlexLucene.Analysis;
using FlexLucene.Analysis.Standard;
using FlexLucene.Document;
using FlexLucene.Index;
using FlexLucene.Queryparser.Classic;
using FlexLucene.Search;
using FlexLucene.Search.Vectorhighlight;
using FlexLucene.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using java.util;

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
        private static readonly FieldType ExtendedTextFieldType;

        static LuceneHighlighter()
        {
            ExtendedTextFieldType = new FieldType(TextField.TYPE_STORED);
            ExtendedTextFieldType.SetStoreTermVectors(true);
            ExtendedTextFieldType.SetStoreTermVectorOffsets(true);
            ExtendedTextFieldType.SetStoreTermVectorPositions(true);
            ExtendedTextFieldType.Freeze();
        }

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
            var queryParser = new HighlighterQueryParser(writer.GetAnalyzer());
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
                var bestFragment = highlighter.GetBestFragment(fieldQuery, reader, sd.Doc, LuceneExtensions.FULL_TEXT_FIELD_NAME, 150);
                var document = searcher.Doc(sd.Doc);
                var docId = document.Get(Content.ID_FIELD_NAME);

                if (contentHightlightMap.ContainsKey(docId))
                {
                    var dictionary = contentHightlightMap[docId].AsDictionary();
                    dictionary[HIGHLIGHT_FIELD_NAME] = bestFragment;
                }
            }            
        }        

        private static void BuidIndex(IEnumerable<Content> hits, IndexWriter writer)
        {
            foreach (var content in hits)
            {
                var doc = new Document();

                var idField = new StringField(Content.ID_FIELD_NAME, content._id.ToString(), FieldStore.YES);
                doc.Add(idField);

                var fullTextField = new Field(LuceneExtensions.FULL_TEXT_FIELD_NAME, content.ToLuceneFullTextString(), ExtendedTextFieldType);
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

        class HighlighterQueryParser : QueryParser
        {
            public HighlighterQueryParser(Analyzer analyzer)
                : base(LuceneExtensions.FULL_TEXT_FIELD_NAME, analyzer)
            {
            }

            protected override Query GetFieldQuery(string fieldName, string queryText, bool quoted)
            {
                if (fieldName != LuceneExtensions.FULL_TEXT_FIELD_NAME)
                    return null;

                return base.GetFieldQuery(fieldName, queryText, quoted);
            }

            protected override Query GetFieldQuery(string fieldName, string queryText, int slop)
            {
                if (fieldName != LuceneExtensions.FULL_TEXT_FIELD_NAME)
                    return null;

                return base.GetFieldQuery(fieldName, queryText, slop);
            }

            protected override Query GetRangeQuery(string fieldName, string part1, string part2, bool startInclusive, bool endInclusive)
            {
                if (fieldName != LuceneExtensions.FULL_TEXT_FIELD_NAME)
                    return null;

                return base.GetRangeQuery(fieldName, part1, part2, startInclusive, endInclusive);
            }

            protected override Query GetFuzzyQuery(string fieldName, string termString, float minSimilarity)
            {
                if (fieldName != LuceneExtensions.FULL_TEXT_FIELD_NAME)
                    return null;

                return base.GetFuzzyQuery(fieldName, termString, minSimilarity);
            }

            protected override Query GetPrefixQuery(string fieldName, string termString)
            {
                if (fieldName != LuceneExtensions.FULL_TEXT_FIELD_NAME)
                    return null;

                return base.GetPrefixQuery(fieldName, termString);
            }

            protected override Query GetRegexpQuery(string fieldName, string termString)
            {
                if (fieldName != LuceneExtensions.FULL_TEXT_FIELD_NAME)
                    return null;

                return base.GetRegexpQuery(fieldName, termString);
            }

            protected override Query GetWildcardQuery(string fieldName, string termString)
            {
                if (fieldName != LuceneExtensions.FULL_TEXT_FIELD_NAME)
                    return null;

                return base.GetWildcardQuery(fieldName, termString);
            }

            protected override Query GetBooleanQuery(List list)
            {
                if (list.size() == 1)                
                {
                    var clause = list.get(0) as BooleanClause;
                    if (clause != null)
                        return clause.GetQuery(); 
                }
                return base.GetBooleanQuery(list);
            }

            protected override Query GetBooleanQuery(List list, bool disableCoord)
            {
                if (list.size() == 1)
                {
                    var clause = list.get(0) as BooleanClause;
                    if (clause != null)
                        return clause.GetQuery();
                }
                return base.GetBooleanQuery(list, disableCoord);
            }

        }
    }

    
}