using System.Collections.Generic;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Encapsulates the search parameters expected by ExpandoDB.
    /// </summary>
    public class SearchCriteria
    {
        /// <summary>
        /// The defaul value for the TopN property.
        /// </summary>
        internal const int DEFAULT_TOP_N = 1000;

        /// <summary>
        /// The defaul value for the TopNFacets property.
        /// </summary>
        internal const int DEFAULT_TOP_N_FACETS = 0;

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
        /// Gets or sets a comma separated list of Document field to be used for sorting, e.g. "Author:asc,Category:desc". 
        /// By default, the sort order is ascending; to sort in descending order, append a :desc suffix to the field name.
        /// </summary>
        /// <value>
        /// A comma separated list of fields
        /// </value>
        public string SortByFields { get; set; }

        /// <summary>
        /// Gets or sets the number of items to return; default is 100.
        /// </summary>
        /// <value>
        /// The number of items to return.
        /// </value>
        public int? TopN { get; set; }

        /// <summary>
        /// Gets or sets the number of items returned per page of the resultset; default is 10.
        /// </summary>
        /// <value>
        /// The number of items per page.
        /// </value>
        public int? ItemsPerPage { get; set; }

        /// <summary>
        /// Gets or sets the page to return, from a multi-page resultset; default is 1.
        /// </summary>
        /// <value>
        /// The page number to return.
        /// </value>
        public int? PageNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include a highlight field in each document; default is false.
        /// </summary>
        /// <value>
        /// A value indicating whether to include a highlight field in each document.
        /// </value>
        public bool? IncludeHighlight { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of Facets to be included in the resultset; optional.
        /// </summary>
        /// <value>
        /// A comma separated list of Facets
        /// </value>
        public string FacetsToReturn { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of Facet values (e.g. Author:Crispin) that will be used to filter 
        /// the resultset.
        /// </summary>
        /// <value>
        /// A comma separated list of Facet values
        /// </value>
        public string FacetFilters { get; set; }

        /// <summary>
        /// Gets or sets the number of Facet values to return; default is 0.
        /// </summary>
        /// <value>
        /// The number of Facet values to return.
        /// </value>
        public int? TopNFacets { get; set; }
    }
}
