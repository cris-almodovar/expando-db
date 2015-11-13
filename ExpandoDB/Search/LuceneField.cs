using FlexLucene.Document;
using FlexLucene.Util;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Collections;
using System.Text;

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
            if (indexedField == null)
                throw new ArgumentNullException("indexedField");

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
                    if (indexedField.DataType == FieldDataType.None)
                        indexedField.DataType = FieldDataType.Number;

                    var numberString = Convert.ToDouble(value).ToLuceneNumberString();
                    luceneFields.Add(new StringField(fieldName, numberString, Field.Store.NO));

                    if (indexedField.DataType != FieldDataType.Array)
                        luceneFields.Add(new SortedDocValuesField(fieldName, new BytesRef(numberString)));                    
                    break;

                case TypeCode.String:                    
                    var stringValue = (string)value;                    
                    var countOfWhiteSpaces = Regex.Matches(stringValue, @"\s").Count;
                    if (countOfWhiteSpaces == 0)
                    {
                        if (indexedField.DataType == FieldDataType.None)
                            indexedField.DataType = FieldDataType.String;

                        luceneFields.Add(new StringField(fieldName, stringValue, Field.Store.NO));
                        var stringValueForSorting = stringValue.Trim().ToLowerInvariant();

                        if (indexedField.DataType != FieldDataType.Array)
                            luceneFields.Add(new SortedDocValuesField(fieldName, new BytesRef(stringValueForSorting)));
                    }
                    else
                    {
                        if (indexedField.DataType == FieldDataType.None)
                            indexedField.DataType = FieldDataType.Text;

                        luceneFields.Add(new TextField(fieldName, stringValue, Field.Store.NO));
                        if (countOfWhiteSpaces <= 10 && indexedField.DataType != FieldDataType.Array)
                        {
                            var stringValueForSorting = stringValue.Trim().ToLowerInvariant();
                            luceneFields.Add(new SortedDocValuesField(fieldName, new BytesRef(stringValueForSorting)));
                        }
                    }
                    break;

                case TypeCode.DateTime:
                    if (indexedField.DataType == FieldDataType.None)
                        indexedField.DataType = FieldDataType.DateTime;

                    var dateValue = ((DateTime)value).ToLuceneDateString();
                    luceneFields.Add(new StringField(fieldName, dateValue, Field.Store.NO));

                    if (indexedField.DataType != FieldDataType.Array)
                        luceneFields.Add(new SortedDocValuesField(fieldName, new BytesRef(dateValue)));                    
                    break;

                case TypeCode.Object:
                    if (fieldType == typeof(Guid))
                    {
                        if (indexedField.DataType == FieldDataType.None)
                            indexedField.DataType = FieldDataType.String;

                        var idValue = ((Guid)value).ToString();
                        luceneFields.Add(new StringField(fieldName, idValue, Field.Store.YES));

                        if (indexedField.DataType != FieldDataType.Array)
                            luceneFields.Add(new SortedDocValuesField(fieldName, new BytesRef(idValue)));                        
                    }
                    else if (value is IList)
                    {
                        if (indexedField.DataType == FieldDataType.None)
                            indexedField.DataType = FieldDataType.Array;

                        var list = value as IList;
                        luceneFields.AddRange(list.ToLuceneFields(indexedField));
                    }
                    else if (value is IDictionary<string, object>)
                    {
                        if (indexedField.DataType == FieldDataType.None)
                            indexedField.DataType = FieldDataType.Object;

                        var dictionary = value as IDictionary<string, object>;
                        luceneFields.AddRange(dictionary.ToLuceneFields(indexedField));
                    }
                    break;
            }

            return luceneFields;
        }

        private static FieldDataType GetFieldDataType(object value)
        {
            if (value == null)
                return FieldDataType.None;

            var type = value.GetType();
            var typeCode = Type.GetTypeCode(type);

            switch (typeCode)
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
                    return FieldDataType.Number;

                case TypeCode.String:
                    var stringValue = (string)value;
                    var countOfWhiteSpaces = Regex.Matches(stringValue, @"\s").Count;
                    if (countOfWhiteSpaces == 0)
                        return FieldDataType.String;
                    else
                        return FieldDataType.Text;

                case TypeCode.DateTime:
                    return FieldDataType.DateTime;

                case TypeCode.Object:
                    if (type == typeof(Guid))
                        return FieldDataType.String;
                    else if (value is IList)
                        return FieldDataType.Array;
                    else if (value is IDictionary<string, object>)
                        return FieldDataType.Object;
                    break;
            }

            return FieldDataType.None;
        }

        private static List<Field> ToLuceneFields(this IList list, IndexedField indexedField)
        {
            var luceneFields = new List<Field>();
            if (list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (item == null)
                        continue;                    
                    
                    if (indexedField.ArrayElementDataType == FieldDataType.None)
                        indexedField.ArrayElementDataType = GetFieldDataType(item);
                    else if (indexedField.ArrayElementDataType != GetFieldDataType(item))
                        throw new ArgumentException(String.Format("All the elements of '{0}' must be of type '{1}'", indexedField.Name, indexedField.DataType));

                    switch (indexedField.ArrayElementDataType)
                    {
                        case FieldDataType.String:
                        case FieldDataType.Text:
                        case FieldDataType.Number:
                        case FieldDataType.DateTime:
                            luceneFields.AddRange(item.ToLuceneFields(indexedField));
                            break;

                        case FieldDataType.Object:
                            var dictionary = item as IDictionary<string, object>;
                            if (dictionary != null)
                                luceneFields.AddRange(dictionary.ToLuceneFields(indexedField));
                            break;
                    }
                }
            }

            return luceneFields;
        }

        private static List<Field> ToLuceneFields(this IDictionary<string, object> dictionary, IndexedField indexedField)
        {
            var luceneFields = new List<Field>();
            var childSchema = new IndexSchema(indexedField.Name);
            indexedField.DataType = FieldDataType.Object;
            indexedField.ObjectSchema = childSchema;

            foreach (var fieldName in dictionary.Keys)
            {
                var childField = dictionary[fieldName];
                if (childField == null)
                    continue;

                var childFieldDataType = GetFieldDataType(childField);
                switch (childFieldDataType)
                {
                    case FieldDataType.String:
                    case FieldDataType.Text:
                    case FieldDataType.Number:
                    case FieldDataType.DateTime:
                        var childIndexedField = new IndexedField
                        {
                            Name = String.Format("{0}.{1}", indexedField.Name, fieldName),
                            DataType = childFieldDataType
                        };

                        childSchema.Fields.TryAdd(childIndexedField.Name, childIndexedField);
                        luceneFields.AddRange(childField.ToLuceneFields(childIndexedField));
                        break;
                    default:
                        continue;                   
                }
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
                if (fieldValue == null)
                    continue;

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
                        {
                            buffer.AppendFormat("{0}\r\n", ((Guid)fieldValue).ToString());
                        }
                        else if (fieldValue is IList)
                        {
                            var list = fieldValue as IList;
                            buffer.AppendFormat("{0}\r\n", list.ToLuceneFullTextString());

                        }
                        else if (fieldValue is IDictionary<string, object>)
                        {
                            var dictionary2 = fieldValue as IDictionary<string, object>;
                            buffer.AppendFormat("{0}\r\n", dictionary2.ToLuceneFullTextString());
                        }                        
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

        public static string ToLuceneFullTextString(this IList list)
        {
            var buffer = new StringBuilder();

            foreach (var item in list)
            {
                if (item == null)
                    continue;

                var itemType = item.GetType();
                var itemTypeCode = Type.GetTypeCode(itemType);

                switch (itemTypeCode)
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
                        buffer.AppendFormat("{0}\r\n", item.ToString());
                        break;

                    case TypeCode.DateTime:
                        buffer.AppendFormat("{0}\r\n", ((DateTime)item).ToString("yyyy-MM-dd"));
                        break;

                    case TypeCode.String:
                        buffer.AppendFormat("{0}\r\n", item as string);
                        break;

                    case TypeCode.Object:
                        if (itemType == typeof(Guid))
                        {
                            buffer.AppendFormat("{0}\r\n", ((Guid)item).ToString());
                        }
                        else if (itemType is IList)
                        {
                            var list2 = item as IList;
                            buffer.AppendFormat("{0}\r\n", list2.ToLuceneFullTextString());

                        }
                        else if (itemType is IDictionary<string, object>)
                        {
                            var dictionary2 = item as IDictionary<string, object>;
                            buffer.AppendFormat("{0}\r\n", dictionary2.ToLuceneFullTextString());
                        }
                        break;

                }

            }

            return buffer.ToString();
        }

        public static string ToLuceneFullTextString(this IDictionary<string, object> dictionary)
        {
            var buffer = new StringBuilder();

            foreach (var fieldName in dictionary.Keys)
            {
                var field = dictionary[fieldName];
                if (field == null)
                    continue;

                var fieldType = field.GetType();
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
                        buffer.AppendFormat("{0}\r\n", field.ToString());
                        break;

                    case TypeCode.DateTime:
                        buffer.AppendFormat("{0}\r\n", ((DateTime)field).ToString("yyyy-MM-dd"));
                        break;

                    case TypeCode.String:
                        buffer.AppendFormat("{0}\r\n", field as string);
                        break;

                    case TypeCode.Object:
                        if (fieldType == typeof(Guid))
                        {
                            buffer.AppendFormat("{0}\r\n", ((Guid)field).ToString());
                        }
                        else if (fieldType is IList)
                        {
                            var list2 = field as IList;
                            buffer.AppendFormat("{0}\r\n", list2.ToLuceneFullTextString());

                        }
                        else if (fieldType is IDictionary<string, object>)
                        {
                            var dictionary2 = field as IDictionary<string, object>;
                            buffer.AppendFormat("{0}\r\n", dictionary2.ToLuceneFullTextString());
                        }
                        break;

                }

            }

            return buffer.ToString();
        }
    }
}
