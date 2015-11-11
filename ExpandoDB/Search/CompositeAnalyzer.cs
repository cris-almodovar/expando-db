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
        private readonly ContentSchema _schema;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAnalyzer" /> class.
        /// </summary>
        /// <param name="schema">The Content schema.</param>        
        public CompositeAnalyzer(ContentSchema schema) :
            base(Analyzer.PER_FIELD_REUSE_STRATEGY)
        {
            if (schema == null)
                throw new ArgumentNullException("schema");
                   
            _textAnalyzer = new FullTextAnalyzer();
            _keywordAnalyzer = new KeywordAnalyzer();
            _schema = schema;

            _perFieldAnalyzers = new ConcurrentDictionary<string, Analyzer>();
            InitializePerFieldAnalyzers();            
        }

        private void InitializePerFieldAnalyzers()
        {   
            foreach (var fieldName in _schema.Fields.Keys)
            {                
                if (_perFieldAnalyzers.ContainsKey(fieldName))
                    continue;

                var fieldDef = _schema.Fields[fieldName];
                switch (fieldDef.DataType)
                {
                    case FieldDataType.String:
                    case FieldDataType.Number:
                    case FieldDataType.DateTime:
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
            var analyzer = _textAnalyzer;

            if (_perFieldAnalyzers.ContainsKey(fieldName))
                analyzer = _perFieldAnalyzers[fieldName];

            // Check if fieldName is new; if yes, then add it to the _perFieldAnalyzers
            if (_schema.Fields.ContainsKey(fieldName))
            {
                var fieldDef = _schema.Fields[fieldName];
                switch (fieldDef.DataType)
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

