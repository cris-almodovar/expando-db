using System;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents an error in the Lucene IndexSchema
    /// </summary>
    [Serializable]
    public class IndexSchemaException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexSchemaException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IndexSchemaException(string message) : base (message)
        {
        }
    }
}
