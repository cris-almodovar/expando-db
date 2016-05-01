using FlexLucene.Analysis;
using FlexLucene.Analysis.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ExpandoDB.Search
{
    /// <summary>
    /// A custom Lucene AnalyzerWrapper that returns a <see cref="FullTextAnalyzer"/>
    /// or a <see cref="KeywordAnalyzer"/>, depending on the field being analyzed.
    /// </summary>
    /// <remarks>
    /// An instance of CompositeAnalyzer is associated with an instance of <see cref="LuceneIndex"/>,
    /// which in turn is associated with an instance of <see cref="DocumentCollection"/>.
    /// </remarks>
    /// <seealso cref="FlexLucene.Analysis.AnalyzerWrapper" />
    public class CompositeAnalyzer : AnalyzerWrapper
    {
        private readonly ConcurrentDictionary<string, Analyzer> _perFieldAnalyzers;
        private readonly Analyzer _textAnalyzer;
        private readonly Analyzer _keywordAnalyzer;
        private readonly IndexSchema _indexSchema;
        private readonly IDictionary<string, FieldDataType> _knownDataTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAnalyzer" /> class.
        /// </summary>
        /// <param name="indexSchema">The IndexSchema of the Lucene index associated with this <see cref="CompositeAnalyzer"/>.</param>        
        public CompositeAnalyzer(IndexSchema indexSchema) :
            base(Analyzer.PER_FIELD_REUSE_STRATEGY)
        {
            if (indexSchema == null)
                throw new ArgumentNullException(nameof(indexSchema));

            _textAnalyzer = new FullTextAnalyzer();
            _keywordAnalyzer = new KeywordAnalyzer();
            _indexSchema = indexSchema;

            _perFieldAnalyzers = new ConcurrentDictionary<string, Analyzer>();
            _knownDataTypes = new ConcurrentDictionary<string, FieldDataType>();

            // Assign suitable Analyzers for each field in the schema.
            RefreshAnalyzer(_indexSchema);
        }

        private void RefreshAnalyzer(IndexSchema indexSchema)
        {            
            foreach (var indexedField in indexSchema.Fields.Values)  
                if (!_knownDataTypes.ContainsKey(indexedField.Name))         
                     RefreshAnalyzer(indexedField);            
        }

        private void RefreshAnalyzer(IndexedField indexedField)
        { 
            var dataType = indexedField.DataType;
            if (dataType == FieldDataType.Array)
                dataType = indexedField.ArrayElementDataType; 

            switch (dataType)
            {
                case FieldDataType.Null:
                case FieldDataType.Guid:
                case FieldDataType.Number:
                case FieldDataType.DateTime:
                case FieldDataType.Boolean:
                    _perFieldAnalyzers[indexedField.Name] = _keywordAnalyzer;
                    _knownDataTypes[indexedField.Name] = dataType;
                    break;

                case FieldDataType.Text:                    
                    _perFieldAnalyzers[indexedField.Name] = _textAnalyzer;
                    _knownDataTypes[indexedField.Name] = dataType;
                    break;

                case FieldDataType.Object:
                    RefreshAnalyzer(indexedField.ObjectSchema);
                    break;                
            }            
        }

        private void RefreshAnalyzer(string fieldName)
        {
            var indexedField = _indexSchema.FindField(fieldName);
            if (indexedField == null)
                return;

            // Here we will try to assign an Analyzer to a new field,
            // i.e. a field that has just been added to the schema.
            if (!_perFieldAnalyzers.ContainsKey(fieldName))
                RefreshAnalyzer(indexedField);

            // If there is an Analyzer already assigned to the field,
            // check if it's the correct one.
            if (_perFieldAnalyzers.ContainsKey(fieldName))
            {
                // This is only true when the initial DataType for the field is Null, 
                // and then the DataType becomes known (i.e. Text, Number, Guid, Boolean, DateTime)
                if (_knownDataTypes[fieldName] != indexedField.DataType)
                    RefreshAnalyzer(indexedField);
            }
        }

        /// <summary>
        /// Gets the Analyzer for the specified field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        protected override Analyzer GetWrappedAnalyzer(string fieldName)
        {
            if (String.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException($"{nameof(fieldName)} cannot be null or blank");
            
            RefreshAnalyzer(fieldName);

            if (_perFieldAnalyzers.ContainsKey(fieldName))
                return _perFieldAnalyzers[fieldName];

            return _textAnalyzer;
        }

        


    }
}

