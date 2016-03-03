namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Representst the JSON data returned by the DELETE /db/{collection} API.
    /// </summary>
    /// <seealso cref="ExpandoDB.Rest.DTO.ResponseDto" />
    public class DropCollectionResponseDto : ResponseDto
    {
        public bool isDropped { get; set; }
    }
}
