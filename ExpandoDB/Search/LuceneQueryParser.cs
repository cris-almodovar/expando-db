using ExpandoDB.Serialization;
using FlexLucene.Analysis;
using FlexLucene.Queryparser.Classic;
using FlexLucene.Search;
using System;
using LuceneDouble = java.lang.Double;
using LuceneLong = java.lang.Long;
using LuceneInteger = java.lang.Integer;

namespace ExpandoDB.Search
{
    /// <summary>
    /// A custom QueryParser that automatically converts dates and numbers in query expressions to ExpandoDB's date and number formats.
    /// </summary>
    /// <seealso cref="FlexLucene.Queryparser.Classic.QueryParser" />
    public class LuceneQueryParser : QueryParser
    {
        private readonly IndexSchema _indexSchema;
        private static readonly LuceneInteger _indexNullValue = new LuceneInteger(LuceneExtensions.INDEX_NULL_VALUE);

        public LuceneQueryParser(string defaultFieldName, Analyzer analyzer, IndexSchema indexSchema) 
            : base (defaultFieldName, analyzer)
        {
            if (String.IsNullOrWhiteSpace(defaultFieldName))
                throw new ArgumentException($"'{nameof(defaultFieldName)}' cannot be null or blank");
            if (analyzer == null)
                throw new ArgumentNullException(nameof(analyzer));
            if (indexSchema == null)
                throw new ArgumentNullException(nameof(indexSchema));

            _indexSchema = indexSchema;
        }       

        protected override Query GetRangeQuery(string fieldName, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            var indexedField = _indexSchema.FindField(fieldName);
            if (indexedField == null)
                throw new QueryParserException($"'{fieldName}' is not an indexed field.");

            if (!String.IsNullOrWhiteSpace(Config.LuceneNullToken) && (part1 == Config.LuceneNullToken || part2 == Config.LuceneNullToken))
                throw new QueryParserException($"Cannot use null token: '{Config.LuceneNullToken}' in a range query.");

            Query query = null;
            switch (indexedField.DataType)
            {
                case FieldDataType.Number:
                    var number1 = part1.ToLuceneDouble();
                    var number2 = part2.ToLuceneDouble();
                    query = LegacyNumericRangeQuery.NewDoubleRange(fieldName, number1, number2, startInclusive, endInclusive);
                    break;

                case FieldDataType.DateTime:
                    var ticks1 = part1.ToLuceneDateTimeTicks();
                    var ticks2 = part2.ToLuceneDateTimeTicks();
                    query = LegacyNumericRangeQuery.NewLongRange(fieldName, ticks1, ticks2, startInclusive, endInclusive);
                    break;

                case FieldDataType.Boolean:
                    var numericBool1 = part1.ToLuceneNumericBoolean();
                    var numericBool2 = part2.ToLuceneNumericBoolean();
                    query = LegacyNumericRangeQuery.NewIntRange(fieldName, numericBool1, numericBool2, startInclusive, endInclusive);
                    break;

                case FieldDataType.Guid:
                    part1 = part1.ToLuceneGuidString();
                    part2 = part2.ToLuceneGuidString();
                    break;
            }                    
            
            return query ?? base.GetRangeQuery(fieldName, part1, part2, startInclusive, endInclusive);
        } 

        protected override Query GetFieldQuery(string fieldName, string queryText, bool quoted)
        {
            var indexedField = _indexSchema.FindField(fieldName);
            if (indexedField == null)
                throw new QueryParserException($"'{fieldName}' is not an indexed field.");
            
            if (!String.IsNullOrWhiteSpace(Config.LuceneNullToken) && queryText == Config.LuceneNullToken)
            {
                // Special case: searching for null value
                var nullFieldName = fieldName.ToNullFieldName();
                return LegacyNumericRangeQuery.NewIntRange(nullFieldName, _indexNullValue, _indexNullValue, true, true);
            }
            else
            {
                Query query = null;
                switch (indexedField.DataType)
                {
                    case FieldDataType.Number:
                        var number = queryText.ToLuceneDouble();
                        query = LegacyNumericRangeQuery.NewDoubleRange(fieldName, number, number, true, true);
                        break;

                    case FieldDataType.DateTime:
                        var ticks = queryText.ToLuceneDateTimeTicks();
                        query = LegacyNumericRangeQuery.NewLongRange(fieldName, ticks, ticks, true, true);
                        break;

                    case FieldDataType.Boolean:
                        var numericBool = queryText.ToLuceneNumericBoolean();
                        query = LegacyNumericRangeQuery.NewIntRange(fieldName, numericBool, numericBool, true, true);
                        break;

                    case FieldDataType.Guid:
                        queryText = queryText.ToLuceneGuidString();
                        break;
                }

                return query ?? base.GetFieldQuery(fieldName, queryText, quoted);
            }
        }        

        protected override Query GetFuzzyQuery(string fieldName, string termString, float minSimilarity)
        {
            var indexedField = _indexSchema.FindField(fieldName);
            if (indexedField == null)
                throw new QueryParserException($"'{fieldName}' is not an indexed field.");

            if (!(indexedField.DataType == FieldDataType.Text || 
                 (indexedField.DataType == FieldDataType.Array && indexedField.ArrayElementDataType == FieldDataType.Text)))
                throw new QueryParserException($"'{fieldName}' cannot be used for fuzzy search because it is not Text.");

            return base.GetFuzzyQuery(fieldName, termString, minSimilarity);
        }

        protected override Query GetPrefixQuery(string fieldName, string termString)
        {
            var indexedField = _indexSchema.FindField(fieldName);
            if (indexedField == null)
                throw new QueryParserException($"'{fieldName}' is not an indexed field.");

            if (!(indexedField.DataType == FieldDataType.Text ||
                 (indexedField.DataType == FieldDataType.Array && indexedField.ArrayElementDataType == FieldDataType.Text)))
                throw new QueryParserException($"'{fieldName}' cannot be used for Prefix search because it is not Text.");

            return base.GetPrefixQuery(fieldName, termString);
        }

        protected override Query GetRegexpQuery(string fieldName, string termString)
        {
            var indexedField = _indexSchema.FindField(fieldName);
            if (indexedField == null)
                throw new QueryParserException($"'{fieldName}' is not an indexed field.");

            if (!(indexedField.DataType == FieldDataType.Text ||
                 (indexedField.DataType == FieldDataType.Array && indexedField.ArrayElementDataType == FieldDataType.Text)))
                throw new QueryParserException($"'{fieldName}' cannot be used for Regex search because it is not Text.");

            return base.GetRegexpQuery(fieldName, termString);
        }

        protected override Query GetWildcardQuery(string fieldName, string termString)
        {
            if (fieldName != "*")
            {
                var indexedField = _indexSchema.FindField(fieldName);
                if (indexedField == null)
                    throw new QueryParserException($"'{fieldName}' is not an indexed field.");

                if (!(indexedField.DataType == FieldDataType.Text ||
                     (indexedField.DataType == FieldDataType.Array && indexedField.ArrayElementDataType == FieldDataType.Text)))
                    throw new QueryParserException($"'{fieldName}' cannot be used for Wildcard search because it is not Text.");
            }

            return base.GetWildcardQuery(fieldName, termString);
        }        
    }

    internal static class LuceneQueryParserExtensions
    {
        public static LuceneDouble ToLuceneDouble(this string value)
        {
            double doubleValue;
            if (!Double.TryParse(value, out doubleValue))
                throw new QueryParserException($"Invalid number in query: '{value}'");

            return new LuceneDouble(doubleValue);

        }

        public static LuceneLong ToLuceneDateTimeTicks(this string value)
        {
            var dateTimeValue = DateTime.MinValue;
            if (!DynamicJsonSerializer.TryParseDateTime(value, ref dateTimeValue))
                throw new QueryParserException($"Invalid DateTime in query: '{value}'");

            var utcDateTime = dateTimeValue.ToUniversalTime();

            return new LuceneLong(utcDateTime.Ticks);
        }

        public static LuceneInteger ToLuceneNumericBoolean(this string value)
        {
            bool boolValue;
            if (!Boolean.TryParse(value, out boolValue))
                throw new QueryParserException($"Invalid boolean in query: '{value}'");

            var numericBool =  boolValue ? 1 : 0;
            return new LuceneInteger(numericBool);
        }

        public static string ToLuceneGuidString(this string value)
        {            
            Guid guid;
            if (!Guid.TryParse(value, out guid))
                throw new QueryParserException($"Invalid GUID in query: '{value}'");

            return guid.ToString().ToLower();
        }

    }
}
