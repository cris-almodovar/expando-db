namespace ExpandoDB.Search
{
    public class SearchCriteria
    {               
        public string Query { get; set; }        
        public string SortByField { get; set; }
        public int TopN { get; set; }
        public int ItemsPerPage { get; set; }
        public int PageNumber { get; set; }
        public bool IncludeHighlight { get; set; }
    }
}
