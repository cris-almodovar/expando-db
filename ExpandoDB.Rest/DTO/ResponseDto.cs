using System;

namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Defines the fields common to all JSON data returned by ExpandoDB REST endpoints.
    /// </summary>
    public abstract class ResponseDto
    {        
        public DateTime timestamp { get; set; }
        public string elapsed { get; set; }
    }
}
