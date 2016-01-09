using System;
using System.Collections.Generic;

namespace ExpandoDB.Search
{
    public class SearchResult<TResult>
    {           
        public string Query { get; set; }
        public string SortByField { get; set; }
        public int TopN { get; set; }
        public int ItemCount { get; set; }
        public int TotalHits { get; set; }        
        public int PageCount { get; set; }
        public int PageNumber { get; set; }
        public int ItemsPerPage { get; set; }
        public IEnumerable<TResult> Items { get; set; }       

        
        public SearchResult(SearchCriteria criteria, int itemCount = 0, int totalHits = 0, int pageCount = 0)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));
                   
            Query = criteria.Query;
            SortByField = criteria.SortByField;
            TopN = criteria.TopN;
            ItemsPerPage = criteria.ItemsPerPage;
            PageNumber = criteria.PageNumber;

            ItemCount = itemCount;
            TotalHits = totalHits;
            PageCount = pageCount;            

            Items = new List<TResult>();
        }

        
    }
}
