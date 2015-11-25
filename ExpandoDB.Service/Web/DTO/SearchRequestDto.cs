using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest.DTO.DTO
{
    public class SearchRequestDto
    {
        public string select { get; set; }
        public string where { get; set; }
        public string sortBy { get; set; }
        public int topN { get; set; }
        public int itemsPerPage { get; set; }
        public int pageNumber { get; set; }
    }
}
