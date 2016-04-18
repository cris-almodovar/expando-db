using System.Dynamic;

namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the JSON data returned by the GET /db/{collection}/{id} API.
    /// </summary>
    /// <seealso cref="ExpandoDB.Rest.DTO.ResponseDto" />
    public class DocumentResposeDto : ResponseDto
    {             
        public string from { get; set; }
        public ExpandoObject document { get; set; }
    }
}
