using System;

namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Represents the JSON data returned by the POST /db/{collection} API.
    /// </summary>
    /// <seealso cref="ExpandoDB.Rest.DTO.ResponseDto" />
    public class InsertResponseDto : ResponseDto
    {           
        public string from { get; set; }
        public Guid _id { get; set; }        
    }
}
