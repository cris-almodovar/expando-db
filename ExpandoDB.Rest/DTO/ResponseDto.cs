using System;

namespace ExpandoDB.Rest.DTO
{
    /// <summary>
    /// Defines the fields common to all JSON response returned by ExpandoDB REST endpoints.
    /// </summary>
    public abstract class ResponseDto
    {
        /// <summary>
        /// Gets or sets the DateTime (UTC) when this response DTO was generated on the server.
        /// </summary>
        /// <value>
        /// The timestamp.
        /// </value>
        public DateTime timestamp { get; set; }
        /// <summary>
        /// Gets or sets the time it took to process the request and generate this response DTO on the server.
        /// </summary>
        /// <value>
        /// The elapsed time.
        /// </value>
        public string elapsed { get; set; }
    }
}
