namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the REST request accepted by the GET /db/{collection} API.
    /// </summary>
    public class SearchRequestDto
    {
        /// <summary>
        /// Gets or sets a comma separated list of fields to return.
        /// </summary>
        /// <value>
        /// The select list.
        /// </value>
        public string select { get; set; }

        /// <summary>
        /// Gets or sets the number of documents to return; default is 100.
        /// </summary>
        /// <value>
        /// The number of documents to return.
        /// </value>
        public int? topN { get; set; }

        /// <summary>
        /// Gets or sets a Lucene query expression that will be used to search
        /// for matching documents.
        /// </summary>
        /// <value>
        /// A Lucene query expression.
        /// </value>
        public string where { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of fields that will be used to sort
        /// the matching documents.
        /// </summary>
        /// <value>
        /// The order by list.
        /// </value>
        public string orderBy { get; set; }

        /// <summary>
        /// Gets or sets the number of items (documents or groups) returned per page of the resultset; default is 10.
        /// </summary>
        /// <value>
        /// The number of items (documents or groups) per page.
        /// </value>
        public int? itemsPerPage { get; set; }

        /// <summary>
        /// Gets or sets the page to return, from a multi-page resultset; default is 1.
        /// </summary>
        /// <value>
        /// The page number to return.
        /// </value>
        public int? pageNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include a highlight field in each matching document.
        /// </summary>
        /// <value>
        /// A value indicating whether to include a highlight field in each matching document.
        /// </value>
        public bool? highlight { get; set; }


        /// <summary>
        /// Gets or sets a comma separated list of Facets to be returned in the resultset.
        /// </summary>
        /// <value>
        /// A comma separated list of Facets
        /// </value>
        public string selectFacets { get; set; }

        /// <summary>
        /// Gets or sets a comma separated list of Facet values (e.g. Author:Crispin) that will be used to filter the resultset.
        /// </summary>
        /// <value>
        /// A comma separated list of Facet values
        /// </value>
        public string whereFacets { get; set; }

        /// <summary>
        /// Gets or sets the number of Facet values to return; default is 0.
        /// </summary>
        /// <value>
        /// The number of Facet values to return.
        /// </value>
        public int? topNFacets { get; set; }        
    }
}
