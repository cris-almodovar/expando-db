namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents the search parameters accepted by ExpandoDB.
    /// </summary>
    public class SearchCriteria
    {
        /// <summary>
        /// The defaul value for the TopN property.
        /// </summary>
        internal const int DEFAULT_TOP_N = 100;

        /// <summary>
        /// The defaul valeu of the ItemsPerPage property.
        /// </summary>
        internal const int DEFAULT_ITEMS_PER_PAGE = 10;        

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
        public int TopN { get; set; } = DEFAULT_TOP_N;

        /// <summary>
        /// Gets or sets the number of items in one page of the result set.
        /// </summary>
        /// <value>
        /// The number of items in one page of the result set.
        /// </value>
        public int ItemsPerPage { get; set; } = DEFAULT_ITEMS_PER_PAGE;

        /// <summary>
        /// Gets or sets the current page number within the result set.
        /// </summary>
        /// <value>
        /// The current page number.
        /// </value>
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether highlights will be included in the documents.
        /// </summary>
        /// <value>
        ///   <c>true</c> if highlights will be included; otherwise, <c>false</c>.
        /// </value>
        public bool IncludeHighlight { get; set; }
    }
}
