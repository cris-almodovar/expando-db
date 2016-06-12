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
        public SerializationException(string message) : base(message)
        {
        }
    }
}
