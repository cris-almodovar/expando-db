using ExpandoDB.Search;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{
    public class SearchResponseDto : IResponseDto
    {
        public DateTime timestamp { get; set; }
        public string elapsed { get; set; }        
        public string select { get; set; }
        public string from { get; set; }
        public string where { get; set; }
        public string orderBy { get; set; }
        public int topN { get; set; }
        public int itemCount { get; set; }
        public int totalHits { get; set; }
        public int pageCount { get; set; }
        public int pageNumber { get; set; }
        public int itemsPerPage { get; set; }
        public List<ExpandoObject> items { get; set; }        
    }
}
