using ExpandoDB.Search;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Server.Web
{
    public class SearchResponseDto : SearchResult<ExpandoObject>
    {
        public string Elapsed { get; set; }

        public SearchResponseDto(SearchCriteria criteria, int? hitCount = null, int? totalHitCount = null, int? pageCount = null)
            : base (criteria, hitCount, totalHitCount, pageCount)
        {
        }
    }
}
