using System;
using System.Collections;
using System.Collections.Generic;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// Represents an enumerable list of Document objects, created from a list of JSON strings.
    /// </summary>
    public class EnumerableDocuments : IEnumerable<Document>
    {        
        private readonly IEnumerable<LightningKeyValuePair> _keyValuePairs;        

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableDocuments"/> class.
        /// </summary>
        /// <param name="keyValuePairs">The list of key-value pairs</param>
        public EnumerableDocuments(IEnumerable<LightningKeyValuePair> keyValuePairs)
        {
            if (keyValuePairs == null)
                throw new ArgumentNullException(nameof(keyValuePairs));

            _keyValuePairs = keyValuePairs;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Document> GetEnumerator()
        {
            foreach (var kv in _keyValuePairs)
                yield return kv.ToDocument();            
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
