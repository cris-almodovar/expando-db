using FlexLucene.Document;
using FlexLucene.Util;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ExpandoDB.Search
{
    public static class LuceneField
    {
        public const string ID_FIELD_NAME = Content.ID_FIELD_NAME;        
        public const string CREATED_TIMESTAMP_FIELD_NAME = Content.CREATED_TIMESTAMP_FIELD_NAME;
        public const string MODIFIED_TIMESTAMP_FIELD_NAME = Content.MODIFIED_TIMESTAMP_FIELD_NAME;
        public const string FULL_TEXT_FIELD_NAME = "_fullText";

        public const string DATE_TIME_FORMAT = "yyyyMMddHHmmssfffffff";  
        public const string NUMBER_FORMAT = "000000000000000.000000000000";

        /// <summary>
        /// Generates Lucene fields for the given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="indexedField">The indexed field.</param>
        /// <returns></returns>
        public static IList<Field> ToLuceneFields(this object value, IndexedField indexedField)
        {            
            if (value == null)
                return null;

            var luceneFields = new List<Field>();
            var fieldType = value.GetType();
            var fieldName = indexedField.Name.Trim();            

            switch (Type.GetTypeCode(fieldType))
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    indexedField.DataType = FieldDataType.Number;
                    var numberString = Convert.ToDouble(value).ToLuceneNumberString();
                    luceneFields.Add(new StringField(fieldName, numberString, Field.Store.NO));
                    luceneFields.Add(new SortedDocValuesField(fieldName, new BytesRef(numberString)));                    
                    break;

                case TypeCode.String:                    
                    var stringValue = (string)value;                    
                    var countOfWhiteSpaces = Regex.Matches(stringValue, @"\s").Count;
                    if (countOfWhiteSpaces == 0)
                    {
                        indexedField.DataType = FieldDataType.String;
                        luceneFields.Add(new StringField(fieldName, stringValue, Field.Store.NO));
                        var stringValueForSorting = stringValue.Trim().ToLowerInvariant();
                        luceneFields.Add(new SortedDocValuesField(fieldName, new BytesRef(stringValueForSorting)));
                    }
                    else
                    {
                        indexedField.DataType = FieldDataType.Text;
                        luceneFields.Add(new TextField(fieldName, stringValue, Field.Store.NO));
                        if (countOfWhiteSpaces <= 10)
                        {
                            var stringValueForSorting = stringValue.Trim().ToLowerInvariant();
                            luceneFields.Add(new SortedDocValuesField(fieldName, new BytesRef(stringValueForSorting)));
                        }
                    }
                    break;

                case TypeCode.DateTime:
                    indexedField.DataType = FieldDataType.DateTime;
                    var dateValue = ((DateTime)value).ToLuceneDateString();
                    luceneFields.Add(new StringField(fieldName, dateValue, Field.Store.NO));
                    luceneFields.Add(new SortedDocValuesField(fieldName, new BytesRef(dateValue)));                    
                    break;

                case TypeCode.Object:
                    if (fieldType == typeof(Guid))
                    {
                        indexedField.DataType = FieldDataType.String;
                        var idValue = ((Guid)value).ToString();
                        luceneFields.Add(new StringField(fieldName, idValue, Field.Store.YES));
                        luceneFields.Add(new SortedDocValuesField(fieldName, new BytesRef(idValue)));                        
                    }
                    // TODO: Add support for IList and IDictionary

                    break;
            }

            return luceneFields;
        }

        /// <summary>
        /// Generates the Lucene full-text representation of the Content object.
        /// </summary>
        /// <param name="content">The Content object.</param>
        /// <returns></returns>        
        public static string ToLuceneFullTextString(this Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var buffer = new System.Text.StringBuilder();

            var dictionary = content.AsDictionary();
            foreach (var fieldName in dictionary.Keys)
            {
                var fieldValue = dictionary[fieldName];
                var fieldType = fieldValue.GetType();
                var fieldTypeCode = Type.GetTypeCode(fieldType);

                switch (fieldTypeCode)
                {
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Single:
                        buffer.AppendFormat("{0}\r\n", fieldValue.ToString());
                        break;

                    case TypeCode.DateTime:
                        buffer.AppendFormat("{0}\r\n", ((DateTime)fieldValue).ToString("yyyy-MM-dd"));
                        break;

                    case TypeCode.String:
                        buffer.AppendFormat("{0}\r\n", fieldValue as string);
                        break;

                    case TypeCode.Object:
                        if (fieldType == typeof(Guid))
                            buffer.AppendFormat("{0}\r\n", ((Guid)fieldValue).ToString());

                        // TODO: Add support for IList and IDictionary
                        break;  
                }
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Generates the Lucene text representation of the given date.
        /// </summary>
        /// <param name="date">The value.</param>
        /// <returns></returns>
        public static string ToLuceneDateString(this DateTime date)
        {            
            return date.ToString(DATE_TIME_FORMAT);
        }

        /// <summary>
        /// Generates the Lucene text representation of the given number.
        /// </summary>
        /// <param name="number">The number.</param>
        /// <returns></returns>
        public static string ToLuceneNumberString(this double number)
        { 
            var numberString = number.ToString(NUMBER_FORMAT);
            if (numberString.StartsWith("-", StringComparison.InvariantCulture))
            {
                numberString = numberString.Remove(0, 1);
                numberString = "n" + InvertNegativeNumber(numberString);
            }
            else
            {
                if (numberString.StartsWith("+", StringComparison.InvariantCulture))
                    numberString = numberString.Remove(0, 1);

                numberString = "p" + numberString;
            }

            return numberString.Replace(".", "d");
        }

        private static string InvertNegativeNumber(string negativeNumber)
        {
            var buffer = new System.Text.StringBuilder();
            for (int i = 0; i < negativeNumber.Length; i++)
            {
                char digit = negativeNumber[i];
                if (digit >= '0' && digit <= '9')
                    buffer.Append(('9' - digit));
                else
                    buffer.Append(digit);
            }

            return buffer.ToString();
        }
    }
}
