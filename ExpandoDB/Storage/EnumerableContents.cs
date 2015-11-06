using System;
using System.Collections;
using System.Collections.Generic;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// Represents an enumerable list of Content objects, created from a list of JSON strings.
    /// </summary>
    public class EnumerableContents : IEnumerable<Content>
    {
        private IEnumerable<string> _jsonResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableContents"/> class.
        /// </summary>
        /// <param name="jsonResults">The list of JSON strings</param>
        public EnumerableContents(IEnumerable<string> jsonResults)
        {
            _jsonResults = jsonResults;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Content> GetEnumerator()
        {
            foreach (var json in _jsonResults)
            {
                if (String.IsNullOrWhiteSpace(json))
                    continue;
                
                yield return json.ToContent();
            }
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
