using ExpandoDB.Search;
using ExpandoDB.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB
{
    /// <summary>
    /// 
    /// </summary>   
    public class DocumentCursor : IDisposable, IEnumerable<Document>
    {
        private readonly DocValuesCursor _docValuesCursor;        

        /// <summary>
        /// Gets or sets the total hits.
        /// </summary>
        /// <value>
        /// The total hits.
        /// </value>
        public int TotalHits { get; private set; }

        /// <summary>
        /// Gets or sets the count of Documents.
        /// </summary>
        /// <value>
        /// The document count.
        /// </value>
        public int Count { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentCursor" /> class.
        /// </summary>
        /// <param name="docValuesCursor">The document identifier cursor.</param>        
        internal DocumentCursor(DocValuesCursor docValuesCursor)
        {
            _docValuesCursor = docValuesCursor;            

            TotalHits = _docValuesCursor.TotalHits;
            Count = _docValuesCursor.Count;
        }

        #region IEnumerable
        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerator<Document> GetEnumerator()
        {
            foreach (var dictionary in _docValuesCursor)
            {
                var document = new Document(dictionary);
                yield return document;
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
            return this.GetEnumerator();
        }
        #endregion

        #region IDisposable Support
        private bool _isDisposed = false; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _docValuesCursor.Dispose();
                }              

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);           
        }
        #endregion
    }
}
