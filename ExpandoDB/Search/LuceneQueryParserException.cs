﻿using System;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Represents an error that occurred during the parsing of a Lucene query
    /// </summary>
    [Serializable]
    public class LuceneQueryParserException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneQueryParserException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public LuceneQueryParserException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneQueryParserException"/> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public LuceneQueryParserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
