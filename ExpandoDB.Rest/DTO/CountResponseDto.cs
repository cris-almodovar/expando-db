namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the JSON response returned by the GET /db/{collection}/count API.
    /// </summary>
    /// <seealso cref="ExpandoDB.Rest.DTO.ResponseDto" />
    public class CountResponseDto : ResponseDto
    {          
        public string from { get; set; }
        public string where { get; set; }
        public int count { get; set; }        
    }
}
