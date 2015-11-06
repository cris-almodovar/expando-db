using FlexLucene.Index;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using LuceneDocument = FlexLucene.Document.Document;
using LuceneField = FlexLucene.Document.Field;
using LuceneFieldType = FlexLucene.Document.FieldType;

namespace ExpandoDB.Search
{
    public static class SearchExtensions
    {
        public static LuceneDocument ToLuceneDocument(this Content content, SearchSchema searchSchema = null)
        {
            if (content == null)
                throw new ArgumentNullException("content");

            if (searchSchema == null)
                searchSchema = SearchSchema.Default;

            var dictionary = content.AsDictionary();
            if (!dictionary.ContainsKey(Content.ID_FIELD))
                throw new InvalidOperationException("Cannot index a Content that does not have an _id.");

            var luceneDocument = new LuceneDocument();
            foreach (var fieldName in dictionary.Keys)
            {
                IndexedField indexedField = null;
                if (!searchSchema.IndexedFields.TryGetValue(fieldName, out indexedField))                
                {
                    indexedField = new IndexedField { 
                        Name = fieldName                        
                    };
                    searchSchema.IndexedFields.TryAdd(fieldName, indexedField);
                }

                var fieldValue = dictionary[fieldName];
                
                var luceneFields = fieldValue.ToLuceneFields(indexedField);
                foreach (var luceneField in luceneFields)
                    luceneDocument.Add(luceneField);
            }

            // Add the full text field            
            var fullTextField = new LuceneField(LuceneIndex.FULL_TEXT_FIELD, content.ToFullTextString(), LuceneIndex.TEXT_FIELD_TYPE);

            return luceneDocument;
        }

        public static IList<LuceneField> ToLuceneFields(this object value, IndexedField indexedField)
        {
            if (value == null)
                return null;

            var luceneFields = new List<LuceneField>();
            var fieldType = value.GetType();
            var fieldName = indexedField.Name;
            
            switch (Type.GetTypeCode(fieldType))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    indexedField.DataType = IndexedFieldDataType.Number;
                    luceneFields.Add(new LuceneField(fieldName, Convert.ToDouble(value).ToLuceneNumericString(), LuceneIndex.NUMERIC_FIELD_TYPE));                    
                    break;                    

                case TypeCode.String:
                    indexedField.DataType = IndexedFieldDataType.String;
                    var stringValue = value.ToString();
                    var countOfWhiteSpaces = Regex.Matches(stringValue, @"\s").Count;
                    if (countOfWhiteSpaces <= 2)
                        luceneFields.Add(new LuceneField(fieldName, stringValue, LuceneIndex.STRING_FIELD_TYPE));
                    else
                        luceneFields.Add(new LuceneField(fieldName, stringValue, LuceneIndex.TEXT_FIELD_TYPE));
                    break;

                case TypeCode.DateTime:
                    indexedField.DataType = IndexedFieldDataType.DateTime;
                    luceneFields.Add(new LuceneField(fieldName, ((DateTime)value).ToLuceneDateString(), LuceneIndex.DATE_FIELD_TYPE));
                    break;

                default:
                    if (fieldType == typeof(Guid))
                    {
                        indexedField.DataType = IndexedFieldDataType.String;
                        luceneFields.Add(new LuceneField(fieldName, ((Guid)value).ToString(), LuceneIndex.ID_FIELD_TYPE));
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

                    default:
                        break;
                        
                }
            }

            return buffer.ToString();
        }

        public static void SetValues(this LuceneFieldType fieldType, bool tokenized = false, bool omitNorms = true, IndexOptions indexOptions = null, bool stored = false, bool storeTermVectors = false, DocValuesType docValuesType = null)
        {   
            fieldType.SetTokenized(tokenized);
            fieldType.SetOmitNorms(omitNorms);
            fieldType.SetIndexOptions(indexOptions ?? IndexOptions.DOCS);
            fieldType.SetStored(stored);
            fieldType.SetStoreTermVectors(storeTermVectors);
            fieldType.SetDocValuesType(docValuesType ?? DocValuesType.NONE);
            fieldType.Freeze();
        }

        public static string ToLuceneDateString(this DateTime value)
        {
            return value.ToString("yyyyMMddHHmm");
        }       

        public static string ToLuceneNumericString(this double value)
        {
            const string numberFormat = "000000000000000.000000000000";

            var numberString = value.ToString(numberFormat);
            if (numberString.StartsWith("-"))
            {
                numberString = numberString.Remove(0, 1);
                numberString = InvertNegativeNumber("n" + numberString);
            }
            else
            {
                if (numberString.StartsWith("+"))
                    numberString = numberString.Remove(0, 1);

                numberString = "p" + numberString;
            }

            return numberString.Replace(".", "d");
        }

        private static string InvertNegativeNumber(string negativeNumber)
        {
            var numberString = "";
            for (int i = 0; i < negativeNumber.Length; i++)
            {
                char digit = negativeNumber[i];
                if (digit >= '0' && digit <= '9')
                    numberString += (('9' - digit)).ToString();
                else
                    numberString += digit;
            }

            return numberString;
        }
    }
}
