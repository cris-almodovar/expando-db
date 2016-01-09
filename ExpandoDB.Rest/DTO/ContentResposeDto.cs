using System.Dynamic;

namespace ExpandoDB.Rest.DTO
{
    public class ContentResposeDto : ResponseDto
    {             
        public string from { get; set; }
        public ExpandoObject content { get; set; }
    }
}
