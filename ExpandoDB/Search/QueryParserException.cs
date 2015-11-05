using System;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents an error that occurred during the parsing of a Lucene query
    /// </summary>
    [Serializable]
    public class QueryParserException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParserException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public QueryParserException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParserException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public QueryParserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
