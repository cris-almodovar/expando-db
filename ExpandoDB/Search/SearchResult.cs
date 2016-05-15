using System;
using System.Collections.Generic;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Encapsulates information about an ExpandoDB serch query and its result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    public struct SearchResult<TResult>
    {           
        public string Query { get; set; }
        public string SortByField { get; set; }
        public int TopN { get; set; }
        public int ItemCount { get; set; }
        public int TotalHits { get; set; }        
        public int PageCount { get; set; }
        public int PageNumber { get; set; }
        public int ItemsPerPage { get; set; }
        public bool IncludeHighlight { get; set; }
        public IEnumerable<TResult> Items { get; set; }       

        
        public SearchResult(SearchCriteria criteria, int itemCount = 0, int totalHits = 0, int pageCount = 0)
        {          
            Query = criteria.Query;
            SortByField = criteria.SortByField;
            TopN = criteria.TopN;
            ItemsPerPage = criteria.ItemsPerPage;
            IncludeHighlight = criteria.IncludeHighlight;
            PageNumber = criteria.PageNumber;

            ItemCount = itemCount;
            TotalHits = totalHits;
            PageCount = pageCount;            

            Items = new List<TResult>();
        }

        
    }
}
