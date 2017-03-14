using FlexLucene.Document;
using FlexLucene.Facet;
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
    public static class SearchUtils
    {
        /// <summary>
        /// Converts the given comma-separated string to an IList
        /// </summary>
        /// <param name="csvString">The CSV string.</param>
        /// <returns></returns>
        public static IList<string> ToList(this string csvString)
        {
            var list = new List<string>();
            if (!String.IsNullOrWhiteSpace(csvString))
            {
                var fields = csvString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select(fieldName => fieldName.Trim());

                list.AddRange(fields);
            }
            return list;
        }

        /// <summary>
        /// Validates the specified search criteria.
        /// </summary>
        /// <param name="criteria">The search criteria.</param>
        public static void Validate(this SearchCriteria criteria)
        {
            if (criteria.TopN < 0)
                throw new ArgumentException("topN cannot be < zero");
            if (criteria.ItemsPerPage <= 0)
                throw new ArgumentException("itemsPerPage cannot be <= zero");
            if (criteria.PageNumber <= 0)
                throw new ArgumentException("pageNumber cannot be <= zero");
        }

        /// <summary>
        /// Populates the SearchResult object with data from the specified TopFieldDocs object.
        /// </summary>
        /// <param name="result">The SearchResult to be populated.</param>
        /// <param name="topDocs">The TopDocs object returned by Lucene.</param>
        /// <param name="facets">The categories.</param>
        /// <param name="getDoc">A lambda that returns the Lucene document given the doc id.</param>
        /// <exception cref="ArgumentNullException">
        /// </exception>
        public static void PopulateWith(this SearchResult<Guid> result, TopDocs topDocs, IEnumerable<FacetValue> facets, Func<int, LuceneDocument> getDoc)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));
            if (topDocs == null)
                throw new ArgumentNullException(nameof(topDocs));
            if (getDoc == null)
                throw new ArgumentNullException(nameof(getDoc));

            result.ItemCount = topDocs.ScoreDocs.Length;
            result.TotalHits = topDocs.TotalHits;

            var itemsPerPage = result.ItemsPerPage ?? SearchCriteria.DEFAULT_ITEMS_PER_PAGE;
            var pageNumber = result.PageNumber ?? 1;
            var topNFacetValues = result.TopNFacets ?? SearchCriteria.DEFAULT_TOP_N_FACETS;
            var documentIds = new List<Guid>();

            if (result.ItemCount > 0)
            {
                var itemsToSkip = (pageNumber - 1) * itemsPerPage;
                var itemsToTake = itemsPerPage;

                var scoreDocs = topDocs.ScoreDocs
                                            .Skip(itemsToSkip)
                                            .Take(itemsToTake)
                                            .ToList();
                                
                for (var i = 0; i < scoreDocs.Count; i++)
                {
                    var sd = scoreDocs[i];
                    var doc = getDoc(sd.Doc);
                    if (doc == null)
                        continue;

                    var idField = doc.GetField(Schema.MetadataField.ID);
                    var idValue = idField.StringValue();

                    documentIds.Add(Guid.Parse(idValue));
                }                
            }

            result.Items = documentIds;
            result.Facets = facets ?? Enumerable.Empty<FacetValue>();
            result.PageCount = ComputePageCount(result.ItemCount, itemsPerPage);
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
