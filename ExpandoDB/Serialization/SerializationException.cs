using System;

namespace ExpandoDB.Serialization
{
    [Serializable]
    public class SerializationException : Exception
    {
        public SerializationException(string message) : base(message)
        {
        }
    }
}
