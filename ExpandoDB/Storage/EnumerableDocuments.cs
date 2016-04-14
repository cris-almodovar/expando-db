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
        private IEnumerable<StorageRow> _rows;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumerableDocuments"/> class.
        /// </summary>
        /// <param name="rows">The list of JSON strings</param>
        public EnumerableDocuments(IEnumerable<StorageRow> rows)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));

            _rows = rows;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<Document> GetEnumerator()
        {
            foreach (var row in _rows)           
                yield return row.ToDocument();            
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
