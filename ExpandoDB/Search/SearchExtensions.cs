using FlexLucene.Document;
using FlexLucene.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using LuceneDocument = FlexLucene.Document.Document;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Implements utility methods for Lucene search.
    /// </summary>
    public static class SearchExtensions
    {
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

                var documentIds = new List<Guid>();
                for (var i = 0; i < scoreDocs.Count; i++)
                {
                    var sd = scoreDocs[i];
                    var doc = getDoc(sd.Doc);
                    if (doc == null)
                        continue;

                    var idField = doc.GetField(Schema.StandardField.ID);
                    var idValue = idField.StringValue();

                    documentIds.Add(Guid.Parse(idValue));
                }

                result.Items = documentIds;
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
