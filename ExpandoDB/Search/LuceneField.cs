using FlexLucene.Document;
using FlexLucene.Index;
using FlexLucene.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExpandoDB.Search
{
    public static class LuceneField
    {
        public const string ID_FIELD_NAME = Content.ID_FIELD_NAME;        
        public const string CREATED_TIMESTAMP_FIELD_NAME = Content.CREATED_TIMESTAMP_FIELD_NAME;
        public const string MODIFIED_TIMESTAMP_FIELD_NAME = Content.MODIFIED_TIMESTAMP_FIELD_NAME;
        public const string FULL_TEXT_FIELD_NAME = "_fullText";
        
        public static readonly FieldType FULL_TEXT_FIELD_TYPE;        
        public static readonly FieldType STRING_FIELD_TYPE;
        public static readonly FieldType STORED_STRING_FIELD_TYPE;

        const string dateTimeFormat = "yyyyMMddHHmm";
        const string numberFormat = "000000000000000.000000000000";

        static LuceneField()
        {
            FULL_TEXT_FIELD_TYPE = new FieldType().Initialize(true, true, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS, false, false, DocValuesType.NONE);  
            STRING_FIELD_TYPE = new FieldType().Initialize(false, true, IndexOptions.DOCS, false, false, DocValuesType.SORTED);
            STORED_STRING_FIELD_TYPE = new FieldType().Initialize(false, true, IndexOptions.DOCS, true, false, DocValuesType.SORTED);
        }

        private static FieldType Initialize(this FieldType fieldType, bool tokenized = false, bool omitNorms = true, IndexOptions indexOptions = null, bool stored = false, bool storeTermVectors = false, DocValuesType docValuesType = null)
        {
            fieldType.SetTokenized(tokenized);
            fieldType.SetOmitNorms(omitNorms);
            fieldType.SetIndexOptions(indexOptions ?? IndexOptions.DOCS);
            fieldType.SetStored(stored);
            fieldType.SetStoreTermVectors(storeTermVectors);
            fieldType.SetDocValuesType(docValuesType ?? DocValuesType.NONE);
            fieldType.Freeze();

            return fieldType;
        }

        public static IList<Field> ToLuceneFields(this object value, IndexedField indexedField)
        {
            if (value == null)
                return null;

            var luceneFields = new List<Field>();
            var fieldType = value.GetType();
            var fieldName = indexedField.Name;

            switch (Type.GetTypeCode(fieldType))
            {
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    indexedField.DataType = IndexedFieldDataType.Number;
                    var numberString = Convert.ToDouble(value).ToLuceneNumberString();
                    luceneFields.Add(new Field(fieldName, new BytesRef(numberString), LuceneField.STRING_FIELD_TYPE));
                    break;

                case TypeCode.String:
                    indexedField.DataType = IndexedFieldDataType.String;
                    var stringValue = (string)value;
                    var countOfWhiteSpaces = Regex.Matches(stringValue, @"\s").Count;
                    if (countOfWhiteSpaces <= 2)
                        luceneFields.Add(new Field(fieldName, new BytesRef(stringValue), LuceneField.STRING_FIELD_TYPE));
                    else
                        luceneFields.Add(new Field(fieldName, stringValue, LuceneField.FULL_TEXT_FIELD_TYPE));
                    break;

                case TypeCode.DateTime:
                    indexedField.DataType = IndexedFieldDataType.DateTime;
                    var dateValue = ((DateTime)value).ToLuceneDateString();
                    luceneFields.Add(new Field(fieldName, new BytesRef(dateValue), LuceneField.STRING_FIELD_TYPE));
                    break;

                case TypeCode.Object:
                    if (fieldType == typeof(Guid))
                    {
                        indexedField.DataType = IndexedFieldDataType.String;
                        var idValue = ((Guid)value).ToString();
                        luceneFields.Add(new Field(fieldName, new BytesRef(idValue), LuceneField.STORED_STRING_FIELD_TYPE));
                    }
                    break;
            }

            return luceneFields;
        }

        public static string ToFullTextString(this Content content)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            var buffer = new StringBuilder();

            var dictionary = content.AsDictionary();
            foreach (var fieldName in dictionary.Keys)
            {
                var fieldValue = dictionary[fieldName];
                var fieldType = fieldValue.GetType();
                var fieldTypeCode = Type.GetTypeCode(fieldType);

                switch (fieldTypeCode)
                {
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
                        break;

                    default:
                        break;

                }
            }

            return buffer.ToString();
        }

        public static string ToLuceneDateString(this DateTime value)
        {            
            return value.ToString(dateTimeFormat);
        }

        public static string ToLuceneNumberString(this double value)
        { 
            var numberString = value.ToString(numberFormat);
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
            var buffer = new StringBuilder();
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
