namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the JSON data returned by the [PUT|PATCH|DELETE] /db/{collection}/{id} APIs.
    /// </summary>
    /// <seealso cref="ExpandoDB.Rest.DTO.ResponseDto" />
    public class UpdateResponseDto : ResponseDto
    {        
        public string from { get; set; }
        public int affectedCount { get; set; }
    }
}
