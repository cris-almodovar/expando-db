using FlexLucene.Analysis;
using FlexLucene.Analysis.Core;
using System;
using System.Collections.Concurrent;

namespace ExpandoDB.Search
{
    /// <summary>
    /// 
    /// </summary>
    public class CompositeAnalyzer : AnalyzerWrapper
    {
        private readonly ConcurrentDictionary<string, Analyzer> _perFieldAnalyzers;        
        private readonly Analyzer _textAnalyzer;
        private readonly Analyzer _keywordAnalyzer;
        private readonly Func<IndexSchema> _getIndexSchema;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAnalyzer"/> class.
        /// </summary>
        /// <param name="defaultAnalyzer">The default analyzer.</param>
        public CompositeAnalyzer(Func<IndexSchema> getIndexSchema) :
            base(Analyzer.PER_FIELD_REUSE_STRATEGY)
        {
            if (getIndexSchema == null)
                throw new ArgumentNullException("getIndexSchema");
                   
            _textAnalyzer = new FullTextAnalyzer();
            _keywordAnalyzer = new KeywordAnalyzer();
            _getIndexSchema = getIndexSchema;

            _perFieldAnalyzers = new ConcurrentDictionary<string, Analyzer>();
            InitializePerFieldAnalyzers();            
        }

        private void InitializePerFieldAnalyzers()
        {
            var schema = _getIndexSchema();
            if (schema == null)
                return;
            
            foreach (var fieldName in schema.IndexedFields.Keys)
            {                
                if (_perFieldAnalyzers.ContainsKey(fieldName))
                    continue;

                var indexedField = schema.IndexedFields[fieldName];
                switch (indexedField.DataType)
                {
                    case IndexedFieldDataType.String:
                    case IndexedFieldDataType.Number:
                    case IndexedFieldDataType.DateTime:
                        _perFieldAnalyzers[fieldName] = _keywordAnalyzer;
                        break;
                    default:
                        _perFieldAnalyzers[fieldName] = _textAnalyzer;
                        break;
                }                  
            }            
        }

        /// <summary>
        /// Gets the wrapped analyzer.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        protected override Analyzer getWrappedAnalyzer(string fieldName)
        {            
            if (_perFieldAnalyzers.ContainsKey(fieldName))
                return _perFieldAnalyzers[fieldName];

            // TODO: Check if fieldName is new; if yes, then add it to the _perFieldAnalyzers
            var schema = _getIndexSchema();            

            return _textAnalyzer;
        }
    }
}

