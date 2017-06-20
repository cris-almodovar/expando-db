using FlexLucene.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuceneDocument = FlexLucene.Document.Document;
using System.Collections;

namespace ExpandoDB.Search
{
    /// <summary>
    /// 
    /// </summary>    
    internal class DocsCursor : IEnumerable<LuceneDocument>
    {
        /// <summary>
        /// Gets the total hits.
        /// </summary>
        /// <value>
        /// The total hits.
        /// </value>
        public int TotalHits { get; private set; }

        /// <summary>
        /// Gets the count of items in the cursor.
        /// </summary>
        /// <value>
        /// The document ID count.
        /// </value>
        public int Count { get; internal set; }


        private TopDocs _topDocs;
        private Func<int, LuceneDocument> _getDoc;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocsCursor"/> class.
        /// </summary>
        /// <param name="topDocs">The top docs.</param>
        /// <param name="getDoc">A Func that accepts a ScoreDoc doc number and returns a Lucene document.</param>
        public DocsCursor(TopDocs topDocs, Func<int, LuceneDocument> getDoc)
        {
            _topDocs = topDocs;
            _getDoc = getDoc;

            Count = topDocs.ScoreDocs.Length;
            TotalHits = topDocs.TotalHits;
        }

        public IEnumerator<LuceneDocument> GetEnumerator()
        {
            for (var i = 0; i < _topDocs.ScoreDocs.Length; i++)
            {
                var scoreDoc = _topDocs.ScoreDocs[i];
                var luceneDoc = _getDoc(scoreDoc.Doc);

                if (luceneDoc == null)
                    continue;                

                yield return luceneDoc;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
