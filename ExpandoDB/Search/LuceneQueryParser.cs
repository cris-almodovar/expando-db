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
        private readonly Schema _schema;
        private static readonly LuceneInteger _indexNullValue = new LuceneInteger(LuceneExtensions.INDEX_NULL_VALUE);

        public LuceneQueryParser(string defaultFieldName, Analyzer analyzer, Schema schema) 
            : base (defaultFieldName, analyzer)
        {
            if (String.IsNullOrWhiteSpace(defaultFieldName))
                throw new ArgumentException($"'{nameof(defaultFieldName)}' cannot be null or blank");
            if (analyzer == null)
                throw new ArgumentNullException(nameof(analyzer));
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            _schema = schema;
        }       

        protected override Query GetRangeQuery(string fieldName, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            var schemaField = _schema.FindField(fieldName);
            if (schemaField == null)
                throw new LuceneQueryParserException($"'{fieldName}' is not an indexed field.");

            if (!String.IsNullOrWhiteSpace(Config.LuceneNullToken) && (part1 == Config.LuceneNullToken || part2 == Config.LuceneNullToken))
                throw new LuceneQueryParserException($"Cannot use null token: '{Config.LuceneNullToken}' in a range query.");

            Query query = null;
            switch (schemaField.DataType)
            {
                case Schema.DataType.Number:
                    if (part1 == null || part1 == "*")
                        part1 = Double.MinValue.ToString();
                    if (part2 == null || part2 == "*")
                        part2 = Double.MaxValue.ToString();

                    var number1 = part1.ToLuceneDouble();
                    var number2 = part2.ToLuceneDouble();
                    query = LegacyNumericRangeQuery.NewDoubleRange(fieldName, number1, number2, startInclusive, endInclusive);
                    break;

                case Schema.DataType.DateTime:
                    if (part1 == null || part1 == "*")
                        part1 = DateTime.MinValue.ToUniversalTime().ToString(DateTimeFormat.DATE_HHMM_UTC);
                    if (part2 == null || part2 == "*")
                        part2 = DateTime.MaxValue.ToUniversalTime().ToString(DateTimeFormat.DATE_HHMM_UTC);

                    var ticks1 = part1.ToLuceneDateTimeTicks();
                    var ticks2 = part2.ToLuceneDateTimeTicks();
                    query = LegacyNumericRangeQuery.NewLongRange(fieldName, ticks1, ticks2, startInclusive, endInclusive);
                    break;

                case Schema.DataType.Boolean:
                    if (part1 == null || part1 == "*")
                        part1 = Boolean.FalseString.ToLower();
                    if (part2 == null || part2 == "*")
                        part2 = Boolean.TrueString.ToLower();

                    var numericBool1 = part1.ToLuceneNumericBoolean();
                    var numericBool2 = part2.ToLuceneNumericBoolean();
                    query = LegacyNumericRangeQuery.NewIntRange(fieldName, numericBool1, numericBool2, startInclusive, endInclusive);
                    break;

                case Schema.DataType.Guid:
                    if (part1 == null || part1 == "*")
                        part1 = String.Empty;
                    if (part2 == null || part2 == "*")
                        part2 = String.Empty;

                    part1 = part1.ToLuceneGuidString();
                    part2 = part2.ToLuceneGuidString();
                    break;
            }                    
            
            return query ?? base.GetRangeQuery(fieldName, part1, part2, startInclusive, endInclusive);
        } 

        protected override Query GetFieldQuery(string fieldName, string queryText, bool quoted)
        {
            var schemaField = _schema.FindField(fieldName);
            if (schemaField == null)
                throw new LuceneQueryParserException($"'{fieldName}' is not an indexed field.");
            
            if (!String.IsNullOrWhiteSpace(Config.LuceneNullToken) && queryText == Config.LuceneNullToken)
            {
                // Special case: searching for null value
                var nullFieldName = fieldName.ToNullFieldName();
                return LegacyNumericRangeQuery.NewIntRange(nullFieldName, _indexNullValue, _indexNullValue, true, true);
            }
            else
            {
                Query query = null;
                switch (schemaField.DataType)
                {
                    case Schema.DataType.Number:
                        var number = queryText.ToLuceneDouble();
                        query = LegacyNumericRangeQuery.NewDoubleRange(fieldName, number, number, true, true);
                        break;

                    case Schema.DataType.DateTime:
                        var ticks = queryText.ToLuceneDateTimeTicks();
                        query = LegacyNumericRangeQuery.NewLongRange(fieldName, ticks, ticks, true, true);
                        break;

                    case Schema.DataType.Boolean:
                        var numericBool = queryText.ToLuceneNumericBoolean();
                        query = LegacyNumericRangeQuery.NewIntRange(fieldName, numericBool, numericBool, true, true);
                        break;

                    case Schema.DataType.Guid:
                        queryText = queryText.ToLuceneGuidString();
                        break;
                }

                return query ?? base.GetFieldQuery(fieldName, queryText, quoted);
            }
        }        

        protected override Query GetFuzzyQuery(string fieldName, string termString, float minSimilarity)
        {
            var schemaField = _schema.FindField(fieldName);
            if (schemaField == null)
                throw new LuceneQueryParserException($"'{fieldName}' is not an indexed field.");

            if (!(schemaField.DataType == Schema.DataType.Text || 
                 (schemaField.DataType == Schema.DataType.Array && schemaField.ArrayElementDataType == Schema.DataType.Text)))
                throw new LuceneQueryParserException($"'{fieldName}' cannot be used for fuzzy search because it is not Text.");

            return base.GetFuzzyQuery(fieldName, termString, minSimilarity);
        }

        protected override Query GetPrefixQuery(string fieldName, string termString)
        {
            var schemaField = _schema.FindField(fieldName);
            if (schemaField == null)
                throw new LuceneQueryParserException($"'{fieldName}' is not an indexed field.");

            if (!(schemaField.DataType == Schema.DataType.Text ||
                 (schemaField.DataType == Schema.DataType.Array && schemaField.ArrayElementDataType == Schema.DataType.Text)))
                throw new LuceneQueryParserException($"'{fieldName}' cannot be used for Prefix search because it is not Text.");

            return base.GetPrefixQuery(fieldName, termString);
        }

        protected override Query GetRegexpQuery(string fieldName, string termString)
        {
            var schemaField = _schema.FindField(fieldName);
            if (schemaField == null)
                throw new LuceneQueryParserException($"'{fieldName}' is not an indexed field.");

            if (!(schemaField.DataType == Schema.DataType.Text ||
                 (schemaField.DataType == Schema.DataType.Array && schemaField.ArrayElementDataType == Schema.DataType.Text)))
                throw new LuceneQueryParserException($"'{fieldName}' cannot be used for Regex search because it is not Text.");

            return base.GetRegexpQuery(fieldName, termString);
        }

        protected override Query GetWildcardQuery(string fieldName, string termString)
        {
            if (fieldName != "*")
            {
                var schemaField = _schema.FindField(fieldName);
                if (schemaField == null)
                    throw new LuceneQueryParserException($"'{fieldName}' is not an indexed field.");

                if (!(schemaField.DataType == Schema.DataType.Text ||
                     (schemaField.DataType == Schema.DataType.Array && schemaField.ArrayElementDataType == Schema.DataType.Text)))
                    throw new LuceneQueryParserException($"'{fieldName}' cannot be used for Wildcard search because it is not Text.");
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
                throw new LuceneQueryParserException($"Invalid number in query: '{value}'");

            return new LuceneDouble(doubleValue);
        }

        public static LuceneLong ToLuceneDateTimeTicks(this string value)
        {
            var dateTimeValue = DateTime.MinValue;
            if (!DynamicJsonSerializer.TryParseDateTime(value, ref dateTimeValue))
                throw new LuceneQueryParserException($"Invalid DateTime in query: '{value}'");

            var utcDateTime = dateTimeValue.ToUniversalTime();

            return new LuceneLong(utcDateTime.Ticks);
        }

        public static LuceneInteger ToLuceneNumericBoolean(this string value)
        {
            bool boolValue;
            if (!Boolean.TryParse(value, out boolValue))
                throw new LuceneQueryParserException($"Invalid boolean in query: '{value}'");

            var numericBool =  boolValue ? 1 : 0;
            return new LuceneInteger(numericBool);
        }

        public static string ToLuceneGuidString(this string value)
        {            
            Guid guid;
            if (!Guid.TryParse(value, out guid))
                throw new LuceneQueryParserException($"Invalid GUID in query: '{value}'");

            return guid.ToString().ToLower();
        }

    }
}
