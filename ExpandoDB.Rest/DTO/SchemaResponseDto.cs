namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the JSON data returned by the GET /db/{collection}/schema API.
    /// </summary>
    /// <seealso cref="ExpandoDB.Rest.DTO.ResponseDto" />
    public class SchemaResponseDto : ResponseDto
    {          
        public ContentCollectionSchema schema { get; set; }
    }
}
