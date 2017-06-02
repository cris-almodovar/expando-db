namespace ExpandoDB.Search
{
    /// <summary>
    /// 
    /// </summary>
    public class CursorSearchCriteria
    {
        /// <summary>
        /// Gets or sets the Lucene query expression
        /// </summary>
        /// <value>
        /// The Lucene query expression
        /// </value>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of Document field to be used for sorting, e.g. "Author:asc,Category:desc". 
        /// By default, the sort order is ascending; to sort in descending order, append a :desc suffix to the field name.
        /// </summary>
        /// <value>
        /// A comma separated list of fields
        /// </value>
        public string SortByFields { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of Document fields to be returned in the cursor
        /// </summary>
        /// <value>
        /// A comma separated list of fields
        /// </value>
        public string SelectFields { get; set; }

        /// <summary>
        /// Gets or sets the number of items to return; default is 1000.
        /// </summary>
        /// <value>
        /// The number of items to return.
        /// </value>
        public int? TopN { get; set; }
    }
}