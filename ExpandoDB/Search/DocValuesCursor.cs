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
    /// Represents a "live" cursor that can be used to iterate over a Lucene search resultset.
    /// </summary>    
    internal class DocValuesCursor : IDisposable, IEnumerable<Dictionary<string, object>>
    {
        private readonly CursorSearchCriteria _criteria;
        private readonly LuceneIndex _index;
        private readonly SearcherTaxonomyManager _manager;
        private readonly SearcherTaxonomyManagerSearcherAndTaxonomy _searcherAndTaxonomyInstance;
        private TopDocs _topDocs;
        private readonly DocValuesReader _docValuesReader;        

        /// <summary>
        /// Gets the total hits; this value is only available when the cursor is open.
        /// </summary>
        /// <value>
        /// The total hits.
        /// </value>
        public int? TotalHits { get; private set; }

        /// <summary>
        /// Gets the count of items in the cursor; this value is only available when the cursor is open.
        /// </summary>
        /// <value>
        /// The document ID count.
        /// </value>
        public int? Count { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this cursor is open.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this cursor is open; otherwise, <c>false</c>.
        /// </value>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocValuesCursor" /> class.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="index">The index.</param>
        /// <param name="autoOpen">if set to <c>true</c> the cursor is automatically opened after creation.</param>
        public DocValuesCursor(CursorSearchCriteria criteria, LuceneIndex index, bool autoOpen = false)
        {
            if (criteria == null)
                throw new ArgumentNullException(nameof(criteria));
            if (index == null)
                throw new ArgumentNullException(nameof(index));

            _criteria = criteria;
            _index = index;

            IList<string> fieldNames = null;
            if (!String.IsNullOrWhiteSpace(_criteria.SelectFields))
            {
                fieldNames = _criteria.SelectFields.Trim().Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(v => v.Trim())
                                       .Union(new[] { "_id" })
                                       .Distinct()
                                       .ToList();                
            }

            _manager = _index.SearcherTaxonomyManager;
            _searcherAndTaxonomyInstance = _manager.Acquire() as SearcherTaxonomyManagerSearcherAndTaxonomy;
            _docValuesReader = new DocValuesReader(_searcherAndTaxonomyInstance.Searcher, _index.Schema, fieldNames);

            if (autoOpen)
                Open();
        }

        /// <summary>
        /// Opens the cursor by running the underlying Lucene query.
        /// </summary>
        public void Open()
        {
            if (!IsOpen)
            {
                var topN = _criteria.TopN ?? SearchCriteria.DEFAULT_TOP_N;
                var queryParser = new LuceneQueryParser(Schema.MetadataField.FULL_TEXT, _index.Analyzer, _index.Schema);
                var queryString = String.IsNullOrWhiteSpace(_criteria.Query) ? LuceneQueryParser.ALL_DOCS_QUERY : _criteria.Query;
                var query = queryParser.Parse(queryString);
                var sort = queryParser.GetSortCriteria(_criteria.SortByFields, _index.Schema);

                var searcher = _searcherAndTaxonomyInstance.Searcher;
                _topDocs = searcher.Search(query, topN, sort);

                TotalHits = _topDocs.TotalHits;
                Count = _topDocs.ScoreDocs.Length;

                IsOpen = true;
            }
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
            if (!IsOpen)
                Open();

            for (var i = 0; i < _topDocs.ScoreDocs.Length; i++)
            {
                var docNumber = _topDocs.ScoreDocs[i].Doc;
                var dictionary = _docValuesReader.GetDocValuesDictionary(docNumber);
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
