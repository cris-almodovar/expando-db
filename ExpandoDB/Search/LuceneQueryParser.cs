using ExpandoDB.Serialization;
using FlexLucene.Analysis;
using FlexLucene.Queryparser.Classic;
using FlexLucene.Search;
using System;

namespace ExpandoDB.Search
{
    /// <summary>
    /// A custom QueryParser that automatically converts dates and numbers in query expressions to ExpandoDB's date and number formats.
    /// </summary>
    /// <seealso cref="FlexLucene.Queryparser.Classic.QueryParser" />
    public class LuceneQueryParser : QueryParser
    {
        private readonly IndexSchema _indexSchema;

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

            switch (indexedField.DataType)
            {
                case FieldDataType.Number:
                    part1 = ValidateAndFormatAsLuceneNumberString(part1);
                    part2 = ValidateAndFormatAsLuceneNumberString(part2);
                    break;

                case FieldDataType.DateTime:
                    part1 = ValidateAndFormatAsLuceneDateTimeString(part1);
                    part2 = ValidateAndFormatAsLuceneDateTimeString(part2);
                    break;

                case FieldDataType.Boolean:
                    part1 = ValidateAndFormatAsLuceneBooleanString(part1);
                    part2 = ValidateAndFormatAsLuceneBooleanString(part2);
                    break;

                case FieldDataType.Guid:
                    part1 = ValidateAndFormatAsLuceneGuidString(part1);
                    part2 = ValidateAndFormatAsLuceneGuidString(part2);
                    break;
            }                    
            
            return base.GetRangeQuery(fieldName, part1, part2, startInclusive, endInclusive);
        }       

        protected override Query GetFieldQuery(string fieldName, string queryText, bool quoted)
        {
            var indexedField = _indexSchema.FindField(fieldName);
            if (indexedField == null)
                throw new QueryParserException($"'{fieldName}' is not an indexed field.");

            switch (indexedField.DataType)
            {
                case FieldDataType.Number:
                    queryText = ValidateAndFormatAsLuceneNumberString(queryText);
                    break;

                case FieldDataType.DateTime:
                    queryText = ValidateAndFormatAsLuceneDateTimeString(queryText);
                    break;

                case FieldDataType.Boolean:
                    queryText = ValidateAndFormatAsLuceneBooleanString(queryText);
                    break;

                case FieldDataType.Guid:
                    queryText = ValidateAndFormatAsLuceneGuidString(queryText);
                    break;
            }
            
            return base.GetFieldQuery(fieldName, queryText, quoted);
        }       

        private static string ValidateAndFormatAsLuceneNumberString(string value)
        {
            if (value == Config.LuceneNullToken)
                return value;

            double number;
            if (!Double.TryParse(value, out number))
                throw new QueryParserException($"Invalid number in query: '{value}'");

            return number.ToLuceneNumberString();
        }

        private static string ValidateAndFormatAsLuceneDateTimeString(string value)
        {
            if (value == Config.LuceneNullToken)
                return value;

            var dateTime = DateTime.MinValue;

            if (!DynamicSerializer.TryParseDateTime(value, ref dateTime))
                throw new QueryParserException($"Invalid DateTime in query: '{value}'");

            var luceneDateTimeString = dateTime.ToUniversalTime().ToString(LuceneExtensions.DATE_TIME_FORMAT);
            if (luceneDateTimeString.Length < LuceneExtensions.DATE_TIME_FORMAT.Length)
                luceneDateTimeString = luceneDateTimeString.PadRight(LuceneExtensions.DATE_TIME_FORMAT.Length, '0');

            return luceneDateTimeString;
        }

        private string ValidateAndFormatAsLuceneBooleanString(string value)
        {
            if (value == Config.LuceneNullToken)
                return value;

            bool boolValue;
            if (!Boolean.TryParse(value, out boolValue))
                throw new QueryParserException($"Invalid boolean in query: '{value}'");

            return boolValue.ToString().ToLower();
        }

        private string ValidateAndFormatAsLuceneGuidString(string value)
        {
            if (value == Config.LuceneNullToken)
                return value;

            Guid guid;
            if (!Guid.TryParse(value, out guid))
                throw new QueryParserException($"Invalid GUID in query: '{value}'");

            return guid.ToString().ToLower();
        }
    }
}
