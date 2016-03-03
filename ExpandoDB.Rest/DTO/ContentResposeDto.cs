using System.Dynamic;

namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the JSON data returned by the GET /db/{collection}/{id} API.
    /// </summary>
    /// <seealso cref="ExpandoDB.Rest.DTO.ResponseDto" />
    public class ContentResposeDto : ResponseDto
    {             
        public string from { get; set; }
        public ExpandoObject content { get; set; }
    }
}
