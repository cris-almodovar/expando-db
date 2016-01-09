using FlexLucene.Analysis;
using FlexLucene.Queryparser.Classic;
using FlexLucene.Search;
using System;

namespace ExpandoDB.Search
{
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
                    part1 = part1.PadRight(LuceneField.DATE_TIME_FORMAT.Length, '0');
                    part2 = part2.PadRight(LuceneField.DATE_TIME_FORMAT.Length, '0');
                }
            }
            return base.GetRangeQuery(fieldName, part1, part2, startInclusive, endInclusive);
        }

        protected override Query GetFieldQuery(string fieldName, string queryText, bool quoted)
        {
            if (_indexSchema.Fields.ContainsKey(fieldName))
            {
                var indexedField = _indexSchema.Fields[fieldName];
                if (indexedField.DataType == FieldDataType.Number)
                    queryText = FormatAsLuceneNumberString(queryText);
                else if (indexedField.DataType == FieldDataType.DateTime)
                    queryText = queryText.PadRight(LuceneField.DATE_TIME_FORMAT.Length, '0');
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
