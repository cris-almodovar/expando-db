namespace ExpandoDB.Rest.DTO
{
    public class CountRequestDto
    {
        /// <summary>
        /// Gets or sets the where clause for the ExpandoDB count query
        /// </summary>
        /// <value>
        /// A Lucene query expression
        /// </value>
        public string where { get; set; }
    }
}
