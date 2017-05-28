using FlexLucene.Facet.Taxonomy;
using FlexLucene.Search;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Search
{
    /// <summary>
    /// 
    /// </summary>    
    public class DocumentIdCursor : IDisposable, IEnumerable<Guid>
    {       
        private readonly SearcherTaxonomyManager _manager;
        private readonly SearcherTaxonomyManagerSearcherAndTaxonomy _searcherAndTaxonomyInstance;
        private readonly TopDocs _topDocs;

        /// <summary>
        /// Gets or sets the total hits.
        /// </summary>
        /// <value>
        /// The total hits.
        /// </value>
        public int TotalHits { get; private set; }

        /// <summary>
        /// Gets or sets the count of DocumentIds.
        /// </summary>
        /// <value>
        /// The document ID count.
        /// </value>
        public int Count { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentIdCursor" /> class.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="index">The index.</param>
        internal DocumentIdCursor(CursorSearchCriteria criteria, LuceneIndex index)
        {            
            _manager = index.SearcherTaxonomyManager;
            _searcherAndTaxonomyInstance = _manager.Acquire() as SearcherTaxonomyManagerSearcherAndTaxonomy;           

            var topN = criteria.TopN ?? SearchCriteria.DEFAULT_TOP_N; 
            var queryParser = new LuceneQueryParser(Schema.MetadataField.FULL_TEXT, index.Analyzer, index.Schema);
            var queryString = String.IsNullOrWhiteSpace(criteria.Query) ? LuceneQueryParser.ALL_DOCS_QUERY : criteria.Query;
            var query = queryParser.Parse(queryString);
            var sort = queryParser.GetSortCriteria(criteria.SortByFields, index.Schema);

            _topDocs = _searcherAndTaxonomyInstance.Searcher.Search(query, topN, sort);

            TotalHits = _topDocs.TotalHits;
            Count = _topDocs.ScoreDocs.Length;
        }

        #region IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerator<Guid> GetEnumerator()
        {
            for (var i = 0; i < _topDocs.ScoreDocs.Length; i++)
            {
                var sd = _topDocs.ScoreDocs[i];
                var doc = _searcherAndTaxonomyInstance.Searcher.Doc(sd.Doc);
                if (doc == null)
                    continue;

                var idField = doc.GetField(Schema.MetadataField.ID);
                var guid = Guid.Parse(idField.StringValue());

                yield return guid;
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
                    _manager.Release(_searcherAndTaxonomyInstance);
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
