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
        /// Gets or sets the Lucene query expression
        /// </summary>
        /// <value>
        /// The Lucene query expression
        /// </value>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets the Document field to be used for sorting. By default, the sort order is ascending; 
        /// to sort in descending order, prefix the field with '-' (minus sign).
        /// </summary>
        /// <value>
        /// The field to be used for sorting
        /// </value>
        public string SortByField { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items to be returned by the search query.
        /// </summary>
        /// <value>
        /// The maximum number of items to be returned by the search query.
        /// </value>
        public int TopN { get; set; }

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
        /// Gets or sets the number of pages of the search result.
        /// </summary>
        /// <value>
        /// The number of pages of the search result.
        /// </value>
        public int PageCount { get; set; }

        /// <summary>
        /// Gets or sets the current page number within the result set.
        /// </summary>
        /// <value>
        /// The current page number.
        /// </value>
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the number of items in one page of the result set.
        /// </summary>
        /// <value>
        /// The number of items in one page of the result set.
        /// </value>
        public int ItemsPerPage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether text matches are highlighted.
        /// </summary>
        /// <value>
        ///   <c>true</c> if text matches are highlighted; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeHighlight { get; set; }    

        /// <summary>
        /// Gets or sets a comma-separated list of categories that the user has selected;
        /// these categories will be used to drill-sideways to.
        /// </summary>
        /// <value>
        /// The categories.
        /// </value>
        public string SelectCategories { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of categories to be returned by the search query.
        /// </summary>
        /// <value>
        /// The maximum number of categories to be returned by the search query.
        /// </value>
        public int TopNCategories { get; set; }
        
        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>
        /// The items.
        /// </value>
        public IEnumerable<TResult> Items { get; set; }

        /// <summary>
        /// Gets or sets the categories of the items in the search results.
        /// </summary>
        /// <value>
        /// The categories.
        /// </value>
        public IEnumerable<FacetValue> Categories { get; set; }

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
            SortByField = criteria.SortByField;
            TopN = criteria.TopN;
            ItemsPerPage = criteria.ItemsPerPage;
            IncludeHighlight = criteria.IncludeHighlight;
            PageNumber = criteria.PageNumber;

            SelectCategories = criteria.SelectCategories;
            TopNCategories = criteria.TopNCategories;            

            ItemCount = itemCount;
            TotalHits = totalHits;
            PageCount = pageCount;     

            Items = new List<TResult>();
            Categories = new List<FacetValue>();
        }


    }
    
}
