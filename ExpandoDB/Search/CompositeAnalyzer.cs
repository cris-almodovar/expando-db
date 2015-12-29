using FlexLucene.Analysis;
using FlexLucene.Analysis.Core;
using System;
using System.Collections.Concurrent;

namespace ExpandoDB.Search
{    
    public class CompositeAnalyzer : AnalyzerWrapper
    {
        private readonly ConcurrentDictionary<string, Analyzer> _perFieldAnalyzers;        
        private readonly Analyzer _textAnalyzer;
        private readonly Analyzer _keywordAnalyzer;
        private readonly IndexSchema _indexSchema;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAnalyzer" /> class.
        /// </summary>
        /// <param name="indexSchema">The Content schema.</param>        
        public CompositeAnalyzer(IndexSchema indexSchema) :
            base(Analyzer.PER_FIELD_REUSE_STRATEGY)
        {
            if (indexSchema == null)
                throw new ArgumentNullException("indexSchema");
                   
            _textAnalyzer = new FullTextAnalyzer();
            _keywordAnalyzer = new KeywordAnalyzer();
            _indexSchema = indexSchema;

            _perFieldAnalyzers = new ConcurrentDictionary<string, Analyzer>();
            InitializePerFieldAnalyzers();            
        }

        private void InitializePerFieldAnalyzers()
        {   
            foreach (var fieldName in _indexSchema.Fields.Keys)
            {                
                if (_perFieldAnalyzers.ContainsKey(fieldName))
                    continue;

                var indexedField = _indexSchema.Fields[fieldName];
                switch (indexedField.DataType)
                {
                    case FieldDataType.Unknown:
                    case FieldDataType.String:
                    case FieldDataType.Number:
                    case FieldDataType.DateTime:
                    case FieldDataType.Boolean:
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
        protected override Analyzer GetWrappedAnalyzer(string fieldName)
        {
            if (String.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("fieldName cannot be null or blank");

            var analyzer = _textAnalyzer;

            if (_perFieldAnalyzers.ContainsKey(fieldName))
                analyzer = _perFieldAnalyzers[fieldName];

            // Check if fieldName is new; if yes, then add it to the _perFieldAnalyzers
            if (_indexSchema.Fields.ContainsKey(fieldName))
            {
                var indexedField = _indexSchema.Fields[fieldName];
                switch (indexedField.DataType)
                {
                    case FieldDataType.String:
                    case FieldDataType.Number:
                    case FieldDataType.DateTime:
                        _perFieldAnalyzers[fieldName] = analyzer = _keywordAnalyzer;                        
                        break;
                    default:
                        _perFieldAnalyzers[fieldName] = analyzer = _textAnalyzer;
                        break;
                }       
            }

            return analyzer;
        }
    }
}

