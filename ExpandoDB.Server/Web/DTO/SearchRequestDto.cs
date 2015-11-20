using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Server.Web.DTO
{
    public class SearchRequestDto
    {
        public string Select { get; set; }
        public string Where { get; set; }
        public string SortBy { get; set; }
        public int TopN { get; set; }
        public int ItemsPerPage { get; set; }
        public int PageNumber { get; set; }
    }
}
