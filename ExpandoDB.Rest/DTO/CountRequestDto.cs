namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the request data accepted by the GET /db/{collection}/count API
    /// </summary>
    public class CountRequestDto
    {        
        public string where { get; set; }
    }
}
