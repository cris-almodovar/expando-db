using ExpandoDB.Serialization;
using FlexLucene.Analysis;
using FlexLucene.Queryparser.Classic;
using FlexLucene.Search;
using System;
using LuceneDouble = java.lang.Double;
using LuceneLong = java.lang.Long;
using LuceneInteger = java.lang.Integer;
using FlexLucene.Document;

namespace ExpandoDB.Search
{
    /// <summary>
    /// A custom Lucene QueryParser that automatically converts dates and numbers in query expressions to ExpandoDB's date and number formats.
    /// </summary>
    /// <seealso cref="FlexLucene.Queryparser.Classic.QueryParser" />
    public class LuceneQueryParser : QueryParser
    {
        private readonly Schema _schema;

        /// <summary>
        /// Initializes a new instance of the <see cref="LuceneQueryParser"/> class.
        /// </summary>
        /// <param name="defaultFieldName">Default name of the field.</param>
        /// <param name="analyzer">The analyzer.</param>
        /// <param name="schema">The schema.</param>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="System.ArgumentNullException">
        /// </exception>
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

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected override Query GetRangeQuery(string fieldName, string part1, string part2, bool startInclusive, bool endInclusive)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
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

                    var number1 = part1.ToDouble();
                    var number2 = part2.ToDouble();

                    if (!startInclusive)
                        number1 = java.lang.Math.nextUp(number1);
                    if (!endInclusive)
                        number2 = java.lang.Math.nextDown(number2);

                    query = DoublePoint.NewRangeQuery(fieldName, number1, number2);
                    break;

                case Schema.DataType.DateTime:
                    if (part1 == null || part1 == "*")
                        part1 = DateTime.MinValue.ToUniversalTime().ToString(DateTimeFormat.DATE_HHMM_UTC);
                    if (part2 == null || part2 == "*")
                        part2 = DateTime.MaxValue.ToUniversalTime().ToString(DateTimeFormat.DATE_HHMM_UTC);

                    var ticks1 = part1.ToDateTimeLongTicks();
                    var ticks2 = part2.ToDateTimeLongTicks();

                    if (!startInclusive)
                        ticks1 += 1;
                    if (!endInclusive)
                        ticks1 -= 1;

                    query = LongPoint.NewRangeQuery(fieldName, ticks1, ticks2);
                    break;

                case Schema.DataType.Boolean:
                    if (part1 == null || part1 == "*")
                        part1 = "false";
                    if (part2 == null || part2 == "*")
                        part2 = "true";

                    var numericBool1 = part1.ToInteger();
                    var numericBool2 = part2.ToInteger();

                    if (!startInclusive)
                        numericBool1 += 1;
                    if (!endInclusive)
                        numericBool2 -= 1;

                    query = IntPoint.NewRangeQuery(fieldName, numericBool1, numericBool2);
                    break;

                case Schema.DataType.Guid:
                    if (part1 == null || part1 == "*")
                        part1 = String.Empty;
                    if (part2 == null || part2 == "*")
                        part2 = String.Empty;

                    part1 = part1.ToGuidString();
                    part2 = part2.ToGuidString();
                    break;
            }                    
            
            return query ?? base.GetRangeQuery(fieldName, part1, part2, startInclusive, endInclusive);
        } 

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected override Query GetFieldQuery(string fieldName, string queryText, bool quoted)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var schemaField = _schema.FindField(fieldName);
            if (schemaField == null)
                throw new LuceneQueryParserException($"'{fieldName}' is not an indexed field.");
            
            if (!String.IsNullOrWhiteSpace(Config.LuceneNullToken) && queryText == Config.LuceneNullToken)
            {
                // Special case: searching for null value
                var nullFieldName = fieldName.ToNullFieldName();
                return IntPoint.NewExactQuery(nullFieldName, LuceneUtils.INDEX_NULL_VALUE);
            }
            else
            {
                Query query = null;
                switch (schemaField.DataType)
                {
                    case Schema.DataType.Number:
                        var number = queryText.ToDouble();
                        query = DoublePoint.NewExactQuery(fieldName, number);
                        break;

                    case Schema.DataType.DateTime:
                        var ticks = queryText.ToDateTimeLongTicks();
                        query = LongPoint.NewExactQuery(fieldName, ticks);
                        break;

                    case Schema.DataType.Boolean:
                        var numericBool = queryText.ToInteger();
                        query = IntPoint.NewExactQuery(fieldName, numericBool);
                        break;

                    case Schema.DataType.Guid:
                        queryText = queryText.ToGuidString();
                        break;
                }

                return query ?? base.GetFieldQuery(fieldName, queryText, quoted);
            }
        }        

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected override Query GetFuzzyQuery(string fieldName, string termString, float minSimilarity)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var schemaField = _schema.FindField(fieldName);
            if (schemaField == null)
                throw new LuceneQueryParserException($"'{fieldName}' is not an indexed field.");

            if (!(schemaField.DataType == Schema.DataType.Text || 
                 (schemaField.DataType == Schema.DataType.Array && schemaField.ArrayElementDataType == Schema.DataType.Text)))
                throw new LuceneQueryParserException($"'{fieldName}' cannot be used for fuzzy search because it is not Text.");

            return base.GetFuzzyQuery(fieldName, termString, minSimilarity);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected override Query GetPrefixQuery(string fieldName, string termString)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var schemaField = _schema.FindField(fieldName);
            if (schemaField == null)
                throw new LuceneQueryParserException($"'{fieldName}' is not an indexed field.");

            if (!(schemaField.DataType == Schema.DataType.Text ||
                 (schemaField.DataType == Schema.DataType.Array && schemaField.ArrayElementDataType == Schema.DataType.Text)))
                throw new LuceneQueryParserException($"'{fieldName}' cannot be used for Prefix search because it is not Text.");

            return base.GetPrefixQuery(fieldName, termString);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected override Query GetRegexpQuery(string fieldName, string termString)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        {
            var schemaField = _schema.FindField(fieldName);
            if (schemaField == null)
                throw new LuceneQueryParserException($"'{fieldName}' is not an indexed field.");

            if (!(schemaField.DataType == Schema.DataType.Text ||
                 (schemaField.DataType == Schema.DataType.Array && schemaField.ArrayElementDataType == Schema.DataType.Text)))
                throw new LuceneQueryParserException($"'{fieldName}' cannot be used for Regex search because it is not Text.");

            return base.GetRegexpQuery(fieldName, termString);
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected override Query GetWildcardQuery(string fieldName, string termString)
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
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

   
}
