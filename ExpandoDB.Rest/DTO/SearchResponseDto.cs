using System.Collections.Generic;
using System.Dynamic;

namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the JSON data returned by the GET /db/{collection} API.
    /// </summary>
    /// <seealso cref="ExpandoDB.Rest.DTO.ResponseDto" />
    public class SearchResponseDto : ResponseDto
    {           
        public string select { get; set; }
        public int topN { get; set; }
        public string from { get; set; }
        public string where { get; set; }
        public string orderBy { get; set; }        
        public int itemCount { get; set; }
        public int totalHits { get; set; }
        public int pageCount { get; set; }
        public int pageNumber { get; set; }
        public int itemsPerPage { get; set; }
        public bool highlight { get; set; }
        public List<ExpandoObject> items { get; set; }        
    }
}
