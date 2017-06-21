using FlexLucene.Index;
using FlexLucene.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JavaDouble = java.lang.Double;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Encapsulates loading and reading of DocValues fields from the Lucene index, from an open IndexSearcher object.
    /// </summary>
    internal class DocValuesReader
    {
        private readonly Schema _schema;
        private readonly IEnumerable<string> _fieldNames;        

        private readonly Dictionary<string, SortedSetDocValues> _textValues;
        private readonly Dictionary<string, SortedSetDocValues> _guidValues;
        private readonly Dictionary<string, SortedNumericDocValues> _booleanValues;
        private readonly Dictionary<string, SortedNumericDocValues> _dateTimeValues;
        private readonly Dictionary<string, SortedNumericDocValues> _numberValues;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocValuesReader" /> class.
        /// </summary>
        /// <param name="indexSearcher">The IndexSearcher object.</param>
        /// <param name="schema">The schema.</param>
        /// <param name="fieldNames">The document value fields.</param>
        public DocValuesReader(IndexSearcher indexSearcher, Schema schema, IEnumerable<string> fieldNames)
        {
            if (indexSearcher == null)
                throw new ArgumentNullException(nameof(indexSearcher));
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));
            if (fieldNames == null || fieldNames.Count() == 0)
                throw new ArgumentException($"'{nameof(fieldNames)}' cannot be null or empty.");

            var indexReader = indexSearcher.GetIndexReader();

            _schema = schema;
            _fieldNames = fieldNames;

            _textValues = new Dictionary<string, SortedSetDocValues>();
            _guidValues = new Dictionary<string, SortedSetDocValues>();
            _booleanValues = new Dictionary<string, SortedNumericDocValues>();
            _dateTimeValues = new Dictionary<string, SortedNumericDocValues>();
            _numberValues = new Dictionary<string, SortedNumericDocValues>();

            // Load the docValues from the index.
            foreach (var fieldName in fieldNames.Distinct())
            {
                if (_schema.Fields.ContainsKey(fieldName))
                {
                    var schemaField = _schema.Fields[fieldName];                    
                    switch (schemaField.DataType)
                    {
                        case Schema.DataType.Text:
                            _textValues[fieldName] = MultiDocValues.GetSortedSetValues(indexReader, fieldName.ToGroupingFieldName());
                            break;

                        case Schema.DataType.Guid:
                            _guidValues[fieldName] = MultiDocValues.GetSortedSetValues(indexReader, fieldName.ToGroupingFieldName());
                            break;

                        case Schema.DataType.Boolean:
                            _booleanValues[fieldName] = MultiDocValues.GetSortedNumericValues(indexReader, fieldName.ToGroupingFieldName());
                            break;

                        case Schema.DataType.DateTime:
                            _dateTimeValues[fieldName] = MultiDocValues.GetSortedNumericValues(indexReader, fieldName.ToGroupingFieldName());
                            break;

                        case Schema.DataType.Number:
                            _numberValues[fieldName] = MultiDocValues.GetSortedNumericValues(indexReader, fieldName.ToGroupingFieldName());
                            break;

                        case Schema.DataType.Array:
                            switch (schemaField.ArrayElementDataType)
                            {
                                case Schema.DataType.Text:
                                    _textValues[fieldName] = MultiDocValues.GetSortedSetValues(indexReader, fieldName.ToGroupingFieldName());
                                    break;

                                case Schema.DataType.Guid:
                                    _guidValues[fieldName] = MultiDocValues.GetSortedSetValues(indexReader, fieldName.ToGroupingFieldName());
                                    break;

                                case Schema.DataType.Boolean:
                                    _booleanValues[fieldName] = MultiDocValues.GetSortedNumericValues(indexReader, fieldName.ToGroupingFieldName());
                                    break;

                                case Schema.DataType.DateTime:
                                    _dateTimeValues[fieldName] = MultiDocValues.GetSortedNumericValues(indexReader, fieldName.ToGroupingFieldName());
                                    break;

                                case Schema.DataType.Number:
                                    _numberValues[fieldName] = MultiDocValues.GetSortedNumericValues(indexReader, fieldName.ToGroupingFieldName());
                                    break;

                                case Schema.DataType.Array:
                                case Schema.DataType.Object:
                                    throw new InvalidOperationException($"Array element data type: '{schemaField.ArrayElementDataType}' is not supported at this time.");
                            }
                            break;

                        case Schema.DataType.Object:
                            throw new InvalidOperationException($"Data type: '{schemaField.DataType}' is not supported at this time.");
                    }
                }
            }
        }


        /// <summary>
        /// Gets the values of DocValue fields as a dictionary object wherein the keys are the field names 
        /// and the values are the DocValue values.
        /// </summary>
        /// <param name="docNumber">The <see cref="ScoreDoc"/> document number.</param>
        /// <returns></returns>
        public Dictionary<string, object> GetDocValuesDictionary(int docNumber)
        {
            var dictionary = new Dictionary<string, object>();

            foreach (var fieldName in _fieldNames)
            {
                if (!_schema.Fields.ContainsKey(fieldName))
                    continue;

                var schemaField = _schema.Fields[fieldName];
                switch (schemaField.DataType)
                {
                    case Schema.DataType.Text:
                        if (_textValues.ContainsKey(fieldName))
                        {
                            var sortedDocValues = _textValues[fieldName];
                            if (sortedDocValues != null && sortedDocValues.GetValueCount() > 0)
                            {
                                sortedDocValues.SetDocument(docNumber);

                                var first = sortedDocValues.NextOrd();
                                if (first != SortedSetDocValues.NO_MORE_ORDS)
                                {
                                    var textValue = sortedDocValues.LookupOrd(first)?.Utf8ToString();
                                    dictionary[fieldName] = textValue;
                                }
                            }
                        }
                        break;

                    case Schema.DataType.Guid:
                        if (_guidValues.ContainsKey(fieldName))
                        {
                            var sortedDocValues = _guidValues[fieldName];
                            if (sortedDocValues != null && sortedDocValues.GetValueCount() > 0)
                            {
                                sortedDocValues.SetDocument(docNumber);

                                var first = sortedDocValues.NextOrd();
                                if (first != SortedSetDocValues.NO_MORE_ORDS)
                                {
                                    var textValue = sortedDocValues.LookupOrd(first)?.Utf8ToString();
                                    dictionary[fieldName] = (!String.IsNullOrWhiteSpace(textValue)) ? (Guid?)Guid.Parse(textValue) : null;
                                }
                            }
                        }                      
                        break;

                    case Schema.DataType.Boolean:
                        if (_booleanValues.ContainsKey(fieldName))
                        {
                            var sortedDocValues = _booleanValues[fieldName];
                            if (sortedDocValues != null)
                            {
                                sortedDocValues.SetDocument(docNumber);
                                if (sortedDocValues.Count() > 0)
                                {
                                    var first = 0;
                                    var longValue = sortedDocValues.ValueAt(first);
                                    dictionary[fieldName] = (longValue == 1);
                                }
                            }
                        }
                        break;

                    case Schema.DataType.DateTime:
                        if (_dateTimeValues.ContainsKey(fieldName))
                        {
                            var sortedDocValues = _dateTimeValues[fieldName];
                            if (sortedDocValues != null)
                            {
                                sortedDocValues.SetDocument(docNumber);
                                if (sortedDocValues.Count() > 0)
                                {
                                    var first = 0;
                                    var longValue = sortedDocValues.ValueAt(first);
                                    dictionary[fieldName] = new DateTime(longValue, DateTimeKind.Utc);
                                }
                            }
                        }                        
                        break;

                    case Schema.DataType.Number:
                        if (_numberValues.ContainsKey(fieldName))
                        {
                            var sortedDocValues = _numberValues[fieldName];
                            if (sortedDocValues != null)
                            {
                                sortedDocValues.SetDocument(docNumber);
                                if (sortedDocValues.Count() > 0)
                                {
                                    var first = 0;
                                    var longValue = sortedDocValues.ValueAt(first);
                                    dictionary[fieldName] = JavaDouble.longBitsToDouble(longValue);
                                }
                            }
                        }                       
                        break;

                    case Schema.DataType.Array:   
                        switch (schemaField.ArrayElementDataType)
                        {
                            case Schema.DataType.Text:
                                if (_textValues.ContainsKey(fieldName))
                                {
                                    var sortedDocValues = _textValues[fieldName];
                                    if (sortedDocValues != null)
                                    {
                                        sortedDocValues.SetDocument(docNumber);
                                        var textList = new List<string>();

                                        var ordinal = SortedSetDocValues.NO_MORE_ORDS;
                                        do
                                        {
                                            ordinal = sortedDocValues.NextOrd();
                                            var textValue = sortedDocValues.LookupOrd(ordinal)?.Utf8ToString();
                                            if (textValue != null)
                                                textList.Add(textValue);
                                        }
                                        while (ordinal != SortedSetDocValues.NO_MORE_ORDS);

                                        dictionary[fieldName] = textList;
                                    }
                                }                               
                                break;

                            case Schema.DataType.Guid:
                                if (_guidValues.ContainsKey(fieldName))
                                {
                                    var sortedDocValues = _guidValues[fieldName];
                                    if (sortedDocValues != null)
                                    {
                                        sortedDocValues.SetDocument(docNumber);
                                        var guidList = new List<Guid>();

                                        var ordinal = SortedSetDocValues.NO_MORE_ORDS;
                                        do
                                        {
                                            ordinal = sortedDocValues.NextOrd();
                                            var textValue = sortedDocValues.LookupOrd(ordinal)?.Utf8ToString();
                                            if (!String.IsNullOrWhiteSpace(textValue))
                                            {
                                                Guid guid;
                                                if (Guid.TryParse(textValue, out guid))
                                                    guidList.Add(guid);
                                            }
                                        }
                                        while (ordinal != SortedSetDocValues.NO_MORE_ORDS);

                                        dictionary[fieldName] = guidList;
                                    }
                                }
                                break;

                            case Schema.DataType.Boolean:
                                if (_booleanValues.ContainsKey(fieldName))
                                {
                                    var sortedDocValues = _booleanValues[fieldName];
                                    if (sortedDocValues != null)
                                    {
                                        sortedDocValues.SetDocument(docNumber);                                        
                                        var booleanList = new List<bool>();

                                        for (var i = 0; i < sortedDocValues.Count(); i++)
                                        {                                            
                                            var longValue = sortedDocValues.ValueAt(i);                                            
                                            booleanList.Add(longValue == 1);
                                        }                                        

                                        dictionary[fieldName] = booleanList;
                                    }
                                }
                                break;

                            case Schema.DataType.DateTime:
                                if (_dateTimeValues.ContainsKey(fieldName))
                                {
                                    var sortedDocValues = _dateTimeValues[fieldName];
                                    if (sortedDocValues != null)
                                    {
                                        sortedDocValues.SetDocument(docNumber);
                                        var dateTimeList = new List<DateTime>();

                                        for (var i = 0; i < sortedDocValues.Count(); i++)
                                        {
                                            var longValue = sortedDocValues.ValueAt(i);
                                            var dateTimeValue = new DateTime(longValue, DateTimeKind.Utc);
                                            dateTimeList.Add(dateTimeValue);
                                        }

                                        dictionary[fieldName] = dateTimeList;
                                    }
                                }
                                break;

                            case Schema.DataType.Number:
                                if (_numberValues.ContainsKey(fieldName))
                                {
                                    var sortedDocValues = _numberValues[fieldName];
                                    if (sortedDocValues != null)
                                    {
                                        sortedDocValues.SetDocument(docNumber);
                                        var numberList = new List<double>();

                                        for (var i = 0; i < sortedDocValues.Count(); i++)
                                        {
                                            var longValue = sortedDocValues.ValueAt(i);
                                            var doubleValue = JavaDouble.longBitsToDouble(longValue);
                                            numberList.Add(doubleValue);
                                        }

                                        dictionary[fieldName] = numberList;
                                    }
                                }
                                break;

                            case Schema.DataType.Array:
                            case Schema.DataType.Object:
                                throw new InvalidOperationException($"Array element data type: '{schemaField.ArrayElementDataType}' is not supported at this time.");
                        }
                        break;

                    case Schema.DataType.Object:
                        throw new InvalidOperationException($"Data type: '{schemaField.DataType}' is not supported at this time.");
                }
            }


            return dictionary;
        }
        
    }
}
