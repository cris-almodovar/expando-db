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
    internal class DocValuesCursor : IDisposable, IEnumerable<Dictionary<string, object>>
    {
        private readonly CursorSearchCriteria _criteria;
        private readonly LuceneIndex _index;
        private readonly SearcherTaxonomyManager _manager;
        private readonly SearcherTaxonomyManagerSearcherAndTaxonomy _searcherAndTaxonomyInstance;
        private TopDocs _topDocs;

        private readonly DocValuesFieldReader _docValuesFieldReader;
        private readonly IList<string> _docValueFields;

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
        /// Initializes a new instance of the <see cref="DocValuesCursor" /> class.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="index">The index.</param>
        public DocValuesCursor(CursorSearchCriteria criteria, LuceneIndex index)
        {
            _criteria = criteria;
            _index = index;
            
            if (!String.IsNullOrWhiteSpace(_criteria.SelectFields))
            {
                _docValueFields = _criteria.SelectFields.Trim().Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(v => v.Trim())
                                           .Union(new[] { "_id" })
                                           .Distinct()
                                           .ToList();                
            }

            _manager = _index.SearcherTaxonomyManager;
            _searcherAndTaxonomyInstance = _manager.Acquire() as SearcherTaxonomyManagerSearcherAndTaxonomy;
            _docValuesFieldReader = new DocValuesFieldReader(_searcherAndTaxonomyInstance.Searcher.GetIndexReader(), _index.Schema, _docValueFields);
        }

        /// <summary>
        /// Runs the Lucene query to return the topDocs sequence.
        /// </summary>
        /// <returns></returns>
        private TopDocs GetTopDocs()
        {
            var topN = _criteria.TopN ?? SearchCriteria.DEFAULT_TOP_N;
            var queryParser = new LuceneQueryParser(Schema.MetadataField.FULL_TEXT, _index.Analyzer, _index.Schema);
            var queryString = String.IsNullOrWhiteSpace(_criteria.Query) ? LuceneQueryParser.ALL_DOCS_QUERY : _criteria.Query;
            var query = queryParser.Parse(queryString);
            var sort = queryParser.GetSortCriteria(_criteria.SortByFields, _index.Schema);

            var searcher = _searcherAndTaxonomyInstance.Searcher;
            var topDocs = searcher.Search(query, topN, sort);

            TotalHits = topDocs.TotalHits;
            Count = topDocs.ScoreDocs.Length;

            return topDocs;
        }

        #region IEnumerable

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An enumerator that can be used to iterate through the collection.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public IEnumerator<Dictionary<string, object>> GetEnumerator()
        {
            if (_topDocs == null)
                _topDocs = GetTopDocs();

            for (var i = 0; i < _topDocs.ScoreDocs.Length; i++)
            {
                var sd = _topDocs.ScoreDocs[i];
                var dictionary = _docValuesFieldReader.GetDocValuesDictionary(sd.Doc);
                if (dictionary == null)
                    continue;                

                yield return dictionary;
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
