using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO
{
    public class SearchRequestDto
    {        
        public string select { get; set; }
        public int topN { get; set; }
        public string where { get; set; }
        public string orderBy { get; set; }        
        public int itemsPerPage { get; set; }
        public int pageNumber { get; set; }
    }
}
