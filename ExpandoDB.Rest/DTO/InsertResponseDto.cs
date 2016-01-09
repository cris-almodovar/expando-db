using System;

namespace ExpandoDB.Rest.DTO
{
    public class InsertResponseDto : ResponseDto
    {           
        public string from { get; set; }
        public Guid _id { get; set; }        
    }
}
