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
    /// which in turn is associated with an instance of <see cref="Collection"/>.
    /// </remarks>
    /// <seealso cref="FlexLucene.Analysis.AnalyzerWrapper" />
    public class CompositeAnalyzer : AnalyzerWrapper
    {
        private readonly ConcurrentDictionary<string, Analyzer> _perFieldAnalyzers;
        private readonly Analyzer _fullTextAnalyzer;
        private readonly Analyzer _keywordAnalyzer;
        private readonly Schema _schema;
        private readonly IDictionary<string, Schema.DataType> _knownDataTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAnalyzer" /> class.
        /// </summary>
        /// <param name="schema">The Schema of the Lucene index associated with this <see cref="CompositeAnalyzer"/>.</param>        
        public CompositeAnalyzer(Schema schema) :
            base(Analyzer.PER_FIELD_REUSE_STRATEGY)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            _fullTextAnalyzer = new FullTextAnalyzer();
            _keywordAnalyzer = new KeywordAnalyzer();
            _schema = schema;

            _perFieldAnalyzers = new ConcurrentDictionary<string, Analyzer>();
            _knownDataTypes = new ConcurrentDictionary<string, Schema.DataType>();

            // Assign suitable Analyzers for each field in the schema.
            RefreshAnalyzer(_schema);
        }

        private void RefreshAnalyzer(Schema schema)
        {            
            foreach (var schemaField in schema.Fields.Values)  
                if (!_knownDataTypes.ContainsKey(schemaField.Name))         
                     RefreshAnalyzer(schemaField);            
        }

        private void RefreshAnalyzer(Schema.Field schemaField)
        { 
            var dataType = schemaField.DataType;
            if (dataType == Schema.DataType.Array)
                dataType = schemaField.ArrayElementDataType; 

            switch (dataType)
            {
                case Schema.DataType.Null:
                case Schema.DataType.Guid:
                case Schema.DataType.Number:
                case Schema.DataType.DateTime:
                case Schema.DataType.Boolean:
                    _perFieldAnalyzers[schemaField.Name] = _keywordAnalyzer;
                    _knownDataTypes[schemaField.Name] = dataType;
                    break;

                case Schema.DataType.Text:                    
                    _perFieldAnalyzers[schemaField.Name] = _fullTextAnalyzer;
                    _knownDataTypes[schemaField.Name] = dataType;
                    break;

                case Schema.DataType.Object:
                    RefreshAnalyzer(schemaField.ObjectSchema);
                    break;                
            }            
        }

        private void RefreshAnalyzer(string fieldName)
        {
            var schemaField = _schema.FindField(fieldName);
            if (schemaField == null)
                return;

            // Here we will try to assign an Analyzer to a new field,
            // i.e. a field that has just been added to the schema.
            if (!_perFieldAnalyzers.ContainsKey(fieldName))
                RefreshAnalyzer(schemaField);

            // If there is an Analyzer already assigned to the field,
            // check if it's the correct one.
            if (_perFieldAnalyzers.ContainsKey(fieldName))
            {
                // This is only true when the initial DataType for the field is Null, 
                // and then the DataType becomes known (i.e. Text, Number, Guid, Boolean, DateTime)
                if (_knownDataTypes[fieldName] != schemaField.DataType)
                    RefreshAnalyzer(schemaField);
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

            return _fullTextAnalyzer;
        }

        


    }
}

