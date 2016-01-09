using FlexLucene.Document;
using FlexLucene.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using LuceneDocument = FlexLucene.Document.Document;

namespace ExpandoDB.Search
{
    public static class SearchExtensions
    {
        public static LuceneDocument ToLuceneDocument(this Content content, IndexSchema indexSchema = null)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (indexSchema == null)
                indexSchema = IndexSchema.CreateDefault();

            var dictionary = content.AsDictionary();
            if (!dictionary.ContainsKey(Content.ID_FIELD_NAME))
                throw new InvalidOperationException("Cannot index a Content that does not have an _id.");

            var luceneDocument = new LuceneDocument();
            foreach (var fieldName in dictionary.Keys)
            {               
                IndexedField indexedField = null;
                if (!indexSchema.Fields.TryGetValue(fieldName, out indexedField))                
                {
                    indexedField = new IndexedField { 
                        Name = fieldName                        
                    };
                    indexSchema.Fields.TryAdd(fieldName, indexedField);
                }

                var fieldValue = dictionary[fieldName];                
                var luceneFields = fieldValue.ToLuceneFields(indexedField);
                foreach (var luceneField in luceneFields)
                    luceneDocument.Add(luceneField);
            }

            // The full-text field is always generated and added to the lucene document,
            // even though it is not part of the index schema exposed to the user.
            var fullText = content.ToLuceneFullTextString();
             luceneDocument.Add(new TextField(LuceneField.FULL_TEXT_FIELD_NAME, fullText, FieldStore.NO));            

            return luceneDocument;
        }

        /// <summary>
        /// Validates the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        public static void Validate(this SearchCriteria criteria)
        {
            if (criteria.TopN <= 0)
                throw new ArgumentException("topN cannot be <= zero");
            if (criteria.ItemsPerPage <= 0)
                throw new ArgumentException("itemsPerPage cannot be <= zero");
            if (criteria.PageNumber <= 0)
                throw new ArgumentException("pageNumber cannot be <= zero");
        }

        /// <summary>
        /// Populates the SearchResult object with data from the specified TopFieldDocs object.
        /// </summary>
        /// <param name="result">The SearchResult to be populated.</param>
        /// <param name="topFieldDocs">The TopFieldDocs object returned by Lucene.</param>
        /// <param name="getDoc">A lambda that returns the Lucene document given the doc id.</param>
        public static void PopulateWith(this SearchResult<Guid> result, TopFieldDocs topFieldDocs, Func<int, LuceneDocument> getDoc)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (topFieldDocs == null)
                throw new ArgumentNullException(nameof(topFieldDocs));
            if (getDoc == null)
                throw new ArgumentNullException(nameof(getDoc));

            result.ItemCount = topFieldDocs.ScoreDocs.Length;
            result.TotalHits = topFieldDocs.TotalHits;

            if (result.ItemCount > 0)
            {
                var itemsToSkip = (result.PageNumber - 1) * result.ItemsPerPage;
                var itemsToTake = result.ItemsPerPage;

                var scoreDocs = topFieldDocs.ScoreDocs
                                            .Skip(itemsToSkip)
                                            .Take(itemsToTake)
                                            .ToList();

                var contentIds = new List<Guid>();
                for (var i = 0; i < scoreDocs.Count; i++)
                {
                    var sd = scoreDocs[i];
                    var doc = getDoc(sd.Doc);
                    if (doc == null)
                        continue;

                    var idField = doc.GetField(LuceneField.ID_FIELD_NAME);
                    var idValue = idField.StringValue();

                    contentIds.Add(Guid.Parse(idValue));
                }

                result.Items = contentIds;
                result.PageCount = ComputePageCount(result.ItemCount, result.ItemsPerPage);
            }
        }

        private static int ComputePageCount(int hitCount, int itemsPerPage)
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

    }
}
