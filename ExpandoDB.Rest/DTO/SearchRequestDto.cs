namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the REST request accepted by the GET /db/{collection} API.
    /// </summary>
    public class SearchRequestDto
    {        
        public string select { get; set; }
        public int? topN { get; set; }
        public string where { get; set; }
        public string orderBy { get; set; }        
        public int? documentsPerPage { get; set; }
        public int? pageNumber { get; set; }
        public bool? highlight { get; set; }
        public string selectCategories { get; set; }
        public int? topNCategories { get; set; }
    }
}
