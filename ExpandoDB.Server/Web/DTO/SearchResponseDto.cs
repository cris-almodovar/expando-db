using ExpandoDB.Search;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Server.Web.DTO
{
    public class SearchResponseDto 
    {
        public string Elapsed { get; set; }
        public string Select { get; set; }
        public string FromCollection { get; set; }
        public string Where { get; set; }
        public string SortBy { get; set; }
        public int? TopN { get; set; }
        public int? HitCount { get; set; }
        public int? TotalHitCount { get; set; }
        public int? PageCount { get; set; }
        public int? PageNumber { get; set; }
        public int? ItemsPerPage { get; set; }
        public IEnumerable<ExpandoObject> Contents { get; set; }

        public SearchResponseDto(string collectionName, SearchRequestDto searchRequestDto, SearchResult<Content> searchResult)
        {
            if (searchRequestDto == null)
                throw new ArgumentNullException("searchRequestDto");
            if (searchResult == null)
                throw new ArgumentNullException("searchResult");
            
            Select = searchRequestDto.Select;
            FromCollection = collectionName;
            Where = searchRequestDto.Where;
            SortBy = searchRequestDto.SortBy;
            TopN = searchResult.TopN;
            HitCount = searchResult.HitCount;
            TotalHitCount = searchResult.TotalHitCount;
            PageCount = searchResult.PageCount;
            PageNumber = searchResult.PageNumber;
            ItemsPerPage = searchResult.ItemsPerPage;
            Contents = searchResult.Items.Select(c => c.Project(Select.ToList()).AsExpando());
        }     
    }
}
