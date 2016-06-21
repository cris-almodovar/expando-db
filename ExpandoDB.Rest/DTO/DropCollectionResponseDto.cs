namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Representst the JSON response returned by the DELETE /db/{collection} API.
    /// </summary>
    /// <seealso cref="ExpandoDB.Rest.DTO.ResponseDto" />
    public class DropCollectionResponseDto : ResponseDto
    {
        public bool isDropped { get; set; }
    }
}
