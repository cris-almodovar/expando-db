using ExpandoDB.Search;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Server.Web.DTO
{
    public class SearchResponseDto : IResponseDto
    {
        public string Elapsed { get; set; }
        public string Select { get; set; }
        public string FromCollection { get; set; }
        public string Where { get; set; }
        public string SortBy { get; set; }
        public int TopN { get; set; }
        public int ItemCount { get; set; }
        public int TotalHits { get; set; }
        public int PageCount { get; set; }
        public int PageNumber { get; set; }
        public int ItemsPerPage { get; set; }
        public List<ExpandoObject> Contents { get; set; }        
    }
}
