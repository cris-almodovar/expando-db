using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Search
{
    public class SearchCriteria
    {               
        public string Query { get; set; }        
        public string SortByField { get; set; }
        public int TopN { get; set; }
        public int ItemsPerPage { get; set; }
        public int PageNumber { get; set; }
    }
}
