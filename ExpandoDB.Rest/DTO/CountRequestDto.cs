namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the REST request accepted by the GET /db/{collection}/count API
    /// </summary>
    public class CountRequestDto
    {
        /// <summary>
        /// Gets or sets the where clause, which is simply a Lucene query expression.
        /// </summary>
        /// <value>
        /// The where clause
        /// </value>
        public string where { get; set; }
    }
}
