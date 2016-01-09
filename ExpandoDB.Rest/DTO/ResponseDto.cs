using System;

namespace ExpandoDB.Rest.DTO
{
    public abstract class ResponseDto
    {        
        public DateTime timestamp { get; set; }
        public string elapsed { get; set; }
    }
}
