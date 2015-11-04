using FlexLucene.Analysis;
using FlexLucene.Analysis.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PostItDB.Search
{
    /// <summary>
    /// 
    /// </summary>
    public class CompositeAnalyzer : AnalyzerWrapper
    {
        private readonly ConcurrentDictionary<string, Analyzer> _perFieldAnalyzers;        
        private readonly Analyzer _defaultTextAnalyzer;
        private readonly Analyzer _defaultKeywordAnalyzer;
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
                   
            _defaultTextAnalyzer = new FullTextAnalyzer();
            _defaultKeywordAnalyzer = new KeywordAnalyzer();
            _getIndexSchema = getIndexSchema;

            _perFieldAnalyzers = new ConcurrentDictionary<string, Analyzer>();
            InitializePerFieldAnalyzers();            
        }

        private void InitializePerFieldAnalyzers()
        {
            var schema = _getIndexSchema();
            // TODO: Iterate through the schema fields
            //foreach (var field in schema.Fields)
            //        {
            //            Field.Index indexOption = Field.Index.NO;
            //            if (field.IsIndexed && field.IsTokenized == true)
            //                indexOption = Field.Index.ANALYZED;
            //            else if (field.IsIndexed)
            //                indexOption = Field.Index.NOT_ANALYZED;

            //            var luceneFieldName = String.Format("{0}.{1}", schema.Name, field.Name);

            //            if (indexOption != Field.Index.NO && !analyzerWrapper.PerFieldAnalyzers.ContainsKey(luceneFieldName))
            //            {
            //                if (indexOption == Field.Index.ANALYZED)
            //                    analyzerWrapper.PerFieldAnalyzers[luceneFieldName] = _porterStemAnalyzer;
            //                else if (indexOption == Field.Index.NOT_ANALYZED)
            //                    analyzerWrapper.PerFieldAnalyzers[luceneFieldName] = _keywordAnalyzer;
            //            }
            //        }

            _perFieldAnalyzers["_id"] = _defaultKeywordAnalyzer;
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

            return _defaultTextAnalyzer;
        }
    }
}

