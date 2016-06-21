using System;

namespace ExpandoDB.Serialization
{
    /// <summary>
    /// Represents errors that occur as a result of serialization/deserialization.
    /// </summary>
    /// <seealso cref="System.Exception" />
    [Serializable]
    public class SerializationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SerializationException(string message) : base(message)
        {
        }
    }
}
