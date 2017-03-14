using System;
using System.Collections.Generic;
using System.Text;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Encapsulates information about an ExpandoDB search query and its result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public class SearchResult<TResult>
    {
        /// <summary>
        /// Gets or sets the Lucene query expression that was used to search
        /// for matching documents.
        /// </summary>
        /// <value>
        /// A Lucene query expression.
        /// </value>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets the requested comma separated list of fields to be used to sort
        /// the matching documents.
        /// </summary>
        /// <value>
        /// The order by list.
        /// </value>
        public string SortByFields { get; set; }

        /// <summary>
        /// Gets or sets the requested number of documents to return; default is 100.
        /// </summary>
        /// <value>
        /// The number of documents to return.
        /// </value>
        public int? TopN { get; set; }

        /// <summary>
        /// Gets or sets the number of items returned by the search operation. This is normally
        /// equal to the TotalHits; but if TotalHits > TopN, this value will be equal to TopN.
        /// </summary>
        /// <value>
        /// The number of items returned by the search operation
        /// </value>
        public int ItemCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of items that matched the query expression; this value
        /// can be equal to or greater than ItemCount.
        /// </summary>
        /// <value>
        /// The total number of items that matched the query expression.
        /// </value>
        public int TotalHits { get; set; }

        /// <summary>
        /// Gets or sets the number of pages in the resultset.
        /// </summary>
        /// <value>
        /// The number of pages in the resultset.
        /// </value>
        public int PageCount { get; set; }

        /// <summary>
        /// Gets or sets the requested page to return, in the multi-page resultset; default is 1.
        /// </summary>
        /// <value>
        /// The page number to return.
        /// </value>
        public int? PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the requested number of items to return per page of the resultset; default is 10.
        /// </summary>
        /// <value>
        /// The number of documents per page.
        /// </value>
        public int? ItemsPerPage { get; set; }

        /// <summary>
        /// Gets or sets the requested value indicating whether to include a highlight field in each matching document.
        /// </summary>
        /// <value>
        /// A value indicating whether to include a highlight field in each matching document.
        /// </value>
        public bool? IncludeHighlight { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of Facet values (e.g. Author:Crispin) that will be used to drill-sideways thru the Document Facets.
        /// </summary>
        /// <value>
        /// A comma separated list of Facet values
        /// </value>
        public string SelectFacets { get; set; }

        /// <summary>
        /// Gets or sets the requested number of Facet values to return; default is 10.
        /// </summary>
        /// <value>
        /// The number of Facet values to return.
        /// </value>
        public int? TopNFacets { get; set; }
        
        /// <summary>
        /// Gets or sets the matching items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        public IEnumerable<TResult> Items { get; set; }

        /// <summary>
        /// Gets or sets the Facets of the items in the search results.
        /// </summary>
        /// <value>
        /// The categories.
        /// </value>
        public IEnumerable<FacetValue> Facets { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchResult{TResult}"/> class.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="itemCount">The item count.</param>
        /// <param name="totalHits">The total hits.</param>
        /// <param name="pageCount">The page count.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public SearchResult(SearchCriteria criteria, int itemCount = 0, int totalHits = 0, int pageCount = 0)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));

            Query = criteria.Query;
            SortByFields = criteria.SortByFields;
            TopN = criteria.TopN ?? SearchCriteria.DEFAULT_TOP_N;
            ItemsPerPage = criteria.ItemsPerPage ?? SearchCriteria.DEFAULT_ITEMS_PER_PAGE;
            IncludeHighlight = criteria.IncludeHighlight;
            PageNumber = criteria.PageNumber ?? 1;

            SelectFacets = criteria.SelectFacets;
            TopNFacets = criteria.TopNFacets ?? SearchCriteria.DEFAULT_TOP_N_FACETS;            

            ItemCount = itemCount;
            TotalHits = totalHits;
            PageCount = pageCount;     

            Items = new List<TResult>();
            Facets = new List<FacetValue>();
        }


    }
    
}
