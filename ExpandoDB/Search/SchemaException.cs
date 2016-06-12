using System;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents an error in the Schema
    /// </summary>
    [Serializable]
    public class SchemaException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SchemaException(string message) : base (message)
        {
        }
    }
}
