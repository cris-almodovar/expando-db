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
                throw new ArgumentException("defaultFieldName cannot be null or blank");
            if (analyzer == null)
                throw new ArgumentNullException(nameof(analyzer));
            if (indexSchema == null)
                throw new ArgumentNullException(nameof(indexSchema));

            _indexSchema = indexSchema;
        }       

        protected override Query GetRangeQuery(string fieldName, string part1, string part2, bool startInclusive, bool endInclusive)
        {
            if (_indexSchema.Fields.ContainsKey(fieldName))
            {
                var indexedField = _indexSchema.Fields[fieldName];
                if (indexedField.DataType == FieldDataType.Number)
                {
                    part1 = FormatAsLuceneNumberString(part1);
                    part2 = FormatAsLuceneNumberString(part2);                    
                }
                else if (indexedField.DataType == FieldDataType.DateTime)
                {
                    var date1 = DateTime.MinValue;
                    var date2 = DateTime.MinValue;

                    if (DynamicSerializer.TryParseDateTime(part1, ref date1))
                        part1 = date1.ToUniversalTime().ToString(LuceneExtensions.DATE_TIME_FORMAT);
                    if (DynamicSerializer.TryParseDateTime(part2, ref date2))
                        part2 = date2.ToUniversalTime().ToString(LuceneExtensions.DATE_TIME_FORMAT);

                    part1 = part1.PadRight(LuceneExtensions.DATE_TIME_FORMAT.Length, '0');
                    part2 = part2.PadRight(LuceneExtensions.DATE_TIME_FORMAT.Length, '0');
                }
            }
            return base.GetRangeQuery(fieldName, part1, part2, startInclusive, endInclusive);
        }

        protected override Query GetFieldQuery(string fieldName, string queryText, bool quoted)
        {
            if (_indexSchema.Fields.ContainsKey(fieldName) && queryText != Config.LuceneNullToken)
            {
                var indexedField = _indexSchema.Fields[fieldName];
                if (indexedField.DataType == FieldDataType.Number)
                {                    
                   queryText = FormatAsLuceneNumberString(queryText);
                }
                else if (indexedField.DataType == FieldDataType.DateTime)
                {
                    var date1 = DateTime.MinValue;
                    if (DynamicSerializer.TryParseDateTime(queryText, ref date1))
                        queryText = date1.ToUniversalTime().ToString(LuceneExtensions.DATE_TIME_FORMAT);

                    queryText = queryText.PadRight(LuceneExtensions.DATE_TIME_FORMAT.Length, '0');
                }
            }
            return base.GetFieldQuery(fieldName, queryText, quoted);
        }

        private static string FormatAsLuceneNumberString(string part)
        {
            double number;
            if (!Double.TryParse(part, out number))
                throw new QueryParserException("Invalid number in query: " + part);

            return number.ToLuceneNumberString();
        }        
    }
}
