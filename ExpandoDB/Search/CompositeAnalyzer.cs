using FlexLucene.Analysis;
using FlexLucene.Analysis.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            
            foreach (var field in schema.IndexFields)
            {                
                if (_perFieldAnalyzers.ContainsKey(field.Name))
                    continue;
                
                switch (field.DataType)
                {
                    case IndexFieldDataType.String:
                    case IndexFieldDataType.Number:
                    case IndexFieldDataType.DateTime:
                        _perFieldAnalyzers[field.Name] = _keywordAnalyzer;
                        break;
                    default:
                        _perFieldAnalyzers[field.Name] = _textAnalyzer;
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

