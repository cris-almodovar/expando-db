using FlexLucene.Document;
using FlexLucene.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuceneDocument = FlexLucene.Document.Document;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Implements extension methods for converting objects to Lucene Fields.
    /// </summary>
    public static class LuceneExtensions
    {       
        public const string FULL_TEXT_FIELD_NAME = "_full_text_";
        public const string DATE_TIME_FORMAT = "yyyyMMddHHmmssfffffff";  
        public const string NUMBER_FORMAT = "000000000000000.000000000000";
        public const string DEFAULT_NULL_TOKEN = "_null_";

        /// <summary>
        /// Converts a <see cref="Content"/> object to a <see cref="LuceneDocument"/> object.
        /// </summary>
        /// <param name="content">The Content object</param>
        /// <param name="indexSchema">The index schema.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.InvalidOperationException">Cannot index a Content that does not have an _id.</exception>
        public static LuceneDocument ToLuceneDocument(this Content content, IndexSchema indexSchema = null)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            if (indexSchema == null)
                indexSchema = IndexSchema.CreateDefault();

            var contentDictionary = content.AsDictionary();
            if (!contentDictionary.ContainsKey(Content.ID_FIELD_NAME))
                throw new InvalidOperationException("Cannot index a Content that does not have an _id.");

            var luceneDocument = new LuceneDocument();

            // Make sure the _id field is the first field added to the Lucene document
            var keys = contentDictionary.Keys.Except(new[] { Content.ID_FIELD_NAME }).ToList();
            keys.Insert(0, Content.ID_FIELD_NAME);

            foreach (var fieldName in keys)
            {
                IndexedField indexedField = null;
                if (!indexSchema.Fields.TryGetValue(fieldName, out indexedField))
                {
                    indexedField = new IndexedField
                    {
                        Name = fieldName
                    };
                    indexSchema.Fields.TryAdd(fieldName, indexedField);
                }

                var fieldValue = contentDictionary[fieldName];
                var luceneFields = fieldValue.ToLuceneFields(indexedField);
                foreach (var luceneField in luceneFields)
                    luceneDocument.Add(luceneField);
            }

            // The full-text field is always generated and added to the lucene document,
            // even though it is not part of the index schema exposed to the user.
            var fullText = content.ToLuceneFullTextString();
            luceneDocument.Add(new TextField(FULL_TEXT_FIELD_NAME, fullText, FieldStore.NO));

            return luceneDocument;
        }

        /// <summary>
        /// Creates Lucene fields for the given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="indexedField">The indexed field.</param>
        /// <returns></returns>
        public static IList<Field> ToLuceneFields(this object value, IndexedField indexedField)
        {
            if (indexedField == null)
                throw new ArgumentNullException(nameof(indexedField));

            var luceneFields = new List<Field>();            
            var fieldName = indexedField.Name.Trim();

            
            var fieldType = value?.GetType();
            var typeCode = fieldType != null ? Type.GetTypeCode(fieldType) : TypeCode.Empty;

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
                    if (indexedField.DataType == FieldDataType.Unknown)
                        indexedField.DataType = FieldDataType.Number;
                    else if (indexedField.DataType != FieldDataType.Array)
                        EnsureSameFieldDataType(indexedField, FieldDataType.Number);

                    var numberString = Convert.ToDouble(value).ToLuceneNumberString();
                    luceneFields.Add(new StringField(fieldName, numberString, FieldStore.NO));

                    // Only top-level and non-array / non-object fields are sortable
                    if (indexedField.IsTopLevel && indexedField.DataType != FieldDataType.Array && indexedField.DataType != FieldDataType.Object)
                        luceneFields.Add(new SortedDocValuesField(fieldName.ToSortFieldName(), new BytesRef(numberString)));
                    break;

                case TypeCode.Boolean:
                    if (indexedField.DataType == FieldDataType.Unknown)
                        indexedField.DataType = FieldDataType.Boolean;
                    else if (indexedField.DataType != FieldDataType.Array)
                        EnsureSameFieldDataType(indexedField, FieldDataType.Boolean);

                    var booleanString = value.ToString().ToLower();
                    luceneFields.Add(new StringField(fieldName, booleanString, FieldStore.NO));

                    // Only top-level and non-array fields are sortable
                    if (indexedField.IsTopLevel && indexedField.DataType != FieldDataType.Array && indexedField.DataType != FieldDataType.Object)
                        luceneFields.Add(new SortedDocValuesField(fieldName.ToSortFieldName(), new BytesRef(booleanString)));
                    break;

                case TypeCode.String:
                    var stringValue = (string)value;

                    if (indexedField.DataType == FieldDataType.Unknown)
                        indexedField.DataType = FieldDataType.Text;
                    else if (indexedField.DataType != FieldDataType.Array)
                        EnsureSameFieldDataType(indexedField, FieldDataType.Text);

                    luceneFields.Add(new TextField(fieldName, stringValue, FieldStore.NO));

                    // Only top-level and non-array fields are sortable
                    if (indexedField.IsTopLevel && indexedField.DataType != FieldDataType.Array && indexedField.DataType != FieldDataType.Object)
                    {
                        var stringValueForSorting = (stringValue.Length > 50 ? stringValue.Substring(0, 50) : stringValue).Trim().ToLowerInvariant();
                        luceneFields.Add(new SortedDocValuesField(fieldName.ToSortFieldName(), new BytesRef(stringValueForSorting)));
                    }
                    break;

                case TypeCode.DateTime:
                    if (indexedField.DataType == FieldDataType.Unknown)
                        indexedField.DataType = FieldDataType.DateTime;
                    else if (indexedField.DataType != FieldDataType.Array)
                        EnsureSameFieldDataType(indexedField, FieldDataType.DateTime);

                    var dateValue = ((DateTime)value).ToLuceneDateString();
                    luceneFields.Add(new StringField(fieldName, dateValue, FieldStore.NO));

                    // Only top-level and non-array fields are sortable
                    if (indexedField.IsTopLevel && indexedField.DataType != FieldDataType.Array && indexedField.DataType != FieldDataType.Object)
                        luceneFields.Add(new SortedDocValuesField(fieldName.ToSortFieldName(), new BytesRef(dateValue)));
                    break;

                case TypeCode.Object:
                    if (fieldType == typeof(Guid) || fieldType == typeof(Guid?))
                    {
                        if (indexedField.DataType == FieldDataType.Unknown)
                            indexedField.DataType = FieldDataType.Guid;
                        else if (indexedField.DataType != FieldDataType.Array)
                            EnsureSameFieldDataType(indexedField, FieldDataType.Guid);

                        var guidValue = ((Guid)value).ToString();
                        var isStored = (indexedField.Name == Content.ID_FIELD_NAME ? FieldStore.YES : FieldStore.NO);
                        luceneFields.Add(new StringField(fieldName, guidValue, isStored));

                        // Only top-level and non-array fields are sortable
                        if (indexedField.IsTopLevel && indexedField.DataType != FieldDataType.Array && indexedField.DataType != FieldDataType.Object)
                            luceneFields.Add(new SortedDocValuesField(fieldName.ToSortFieldName(), new BytesRef(guidValue)));
                    }
                    else if (value is IList)
                    {
                        if (indexedField.DataType == FieldDataType.Unknown)
                            indexedField.DataType = FieldDataType.Array;
                        else
                            EnsureSameFieldDataType(indexedField, FieldDataType.Array);

                        var list = value as IList;
                        luceneFields.AddRange(list.ToLuceneFields(indexedField));
                    }
                    else if (value is IDictionary<string, object>)
                    {
                        if (indexedField.DataType == FieldDataType.Unknown)
                            indexedField.DataType = FieldDataType.Object;
                        else if (indexedField.DataType != FieldDataType.Array)
                            EnsureSameFieldDataType(indexedField, FieldDataType.Object);

                        var dictionary = value as IDictionary<string, object>;
                        luceneFields.AddRange(dictionary.ToLuceneFields(indexedField));
                    }
                    break;

                case TypeCode.Empty:
                    if (!String.IsNullOrWhiteSpace(Config.LuceneNullToken))
                    {
                        var nullString = Config.LuceneNullToken;
                        switch (indexedField.DataType)
                        {
                            case FieldDataType.Number:
                            case FieldDataType.Boolean:
                            case FieldDataType.DateTime:
                            case FieldDataType.Guid:
                                luceneFields.Add(new StringField(fieldName, nullString, FieldStore.NO));
                                break;
                            case FieldDataType.Text:
                            case FieldDataType.Unknown:
                                luceneFields.Add(new TextField(fieldName, nullString, FieldStore.NO));
                                break;
                        }

                        if (indexedField.IsTopLevel && indexedField.DataType != FieldDataType.Array && indexedField.DataType != FieldDataType.Object)
                            luceneFields.Add(new SortedDocValuesField(fieldName.ToSortFieldName(), new BytesRef(nullString)));
                    }

                    break;
            }            

            return luceneFields;
        }

        /// <summary>
        /// Converts a Lucene field name to one that is suitable for use as a sort field. 
        /// </summary>
        /// <remarks>
        /// The convention is to enclose the fieldname in underscores.
        /// </remarks>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static string ToSortFieldName(this string fieldName)
        {
            if (String.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException($"{nameof(fieldName)} cannot be null or whitespace.");
            
            // Enclose the fieldname in underscores
            return $"_{fieldName}_";
        }

        private static void EnsureSameFieldDataType(IndexedField indexedField, FieldDataType dataType)
        { 
            if (indexedField.DataType != dataType)
            {
                var message = String.Format("Cannot change the data type of the field '{0}' from {1} to {2}.", indexedField.Name, indexedField.DataType, dataType);
                throw new IndexSchemaException(message);
            }
        }

        private static FieldDataType GetFieldDataType(object value)
        {
            if (value == null)
                return FieldDataType.Unknown;

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

                case TypeCode.Boolean:
                    return FieldDataType.Boolean;

                case TypeCode.String:                   
                    return FieldDataType.Text;

                case TypeCode.DateTime:
                    return FieldDataType.DateTime;

                case TypeCode.Object:
                    if (type == typeof(Guid) || type == typeof(Guid?))
                        return FieldDataType.Guid;
                    else if (value is IList)
                        return FieldDataType.Array;
                    else if (value is IDictionary<string, object>)
                        return FieldDataType.Object;
                    break;
            }

            return FieldDataType.Unknown;
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
                    
                    if (indexedField.ArrayElementDataType == FieldDataType.Unknown)
                        indexedField.ArrayElementDataType = GetFieldDataType(item);
                    else if (indexedField.ArrayElementDataType != GetFieldDataType(item))
                        throw new IndexSchemaException(String.Format("All the elements of '{0}' must be of type '{1}'", indexedField.Name, indexedField.DataType));

                    switch (indexedField.ArrayElementDataType)
                    {
                        case FieldDataType.Guid:
                        case FieldDataType.Text:
                        case FieldDataType.Number:
                        case FieldDataType.DateTime:
                        case FieldDataType.Boolean:
                            luceneFields.AddRange(item.ToLuceneFields(indexedField));
                            break;

                        case FieldDataType.Array:
                            throw new IndexSchemaException("JSON with nested arrays are currently not supported.");

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

        private static List<Field> ToLuceneFields(this IDictionary<string, object> dictionary, IndexedField parentIndexedField)
        {
            var luceneFields = new List<Field>();
            var childSchema = parentIndexedField.ObjectSchema ?? new IndexSchema(parentIndexedField.Name);            
            if (parentIndexedField.DataType == FieldDataType.Array)
                parentIndexedField.ArrayElementDataType = FieldDataType.Object;
            parentIndexedField.ObjectSchema = childSchema;

            foreach (var fieldName in dictionary.Keys)
            {
                var childField = dictionary[fieldName];
                var childFieldDataType = GetFieldDataType(childField);

                var childIndexedField = new IndexedField
                {
                    Name = String.Format("{0}.{1}", parentIndexedField.Name, fieldName),
                    DataType = childFieldDataType
                };
                childSchema.Fields.TryAdd(childIndexedField.Name, childIndexedField);

                switch (childFieldDataType)
                {
                    case FieldDataType.Unknown:
                    case FieldDataType.Guid:
                    case FieldDataType.Text:
                    case FieldDataType.Number:
                    case FieldDataType.DateTime:
                    case FieldDataType.Boolean:
                        luceneFields.AddRange(childField.ToLuceneFields(childIndexedField));
                        break;

                    case FieldDataType.Array:
                        var list = childField as IList;
                        if (list != null)
                            luceneFields.AddRange(list.ToLuceneFields(childIndexedField));
                        break;

                    case FieldDataType.Object:
                        var nestedDictionary = childField as IDictionary<string, object>;
                        if (nestedDictionary != null)
                            luceneFields.AddRange(nestedDictionary.ToLuceneFields(childIndexedField));
                        break;                        
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
                throw new ArgumentNullException(nameof(content));

            var buffer = new StringBuilder();

            var dictionary = content.AsDictionary();
            var keys = dictionary.Keys.Except(new[] { Content.ID_FIELD_NAME, Content.CREATED_TIMESTAMP_FIELD_NAME, Content.MODIFIED_TIMESTAMP_FIELD_NAME });

            foreach (var fieldName in keys)
            {
                var fieldValue = dictionary[fieldName];
                if (fieldValue == null)
                    continue;

                buffer.Append(fieldValue.ToLuceneFullTextString());
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
            if (Double.IsNaN(number))
                number = 0;
            else if (Double.IsPositiveInfinity(number))
                number = Double.MaxValue;
            else if (Double.IsNegativeInfinity(number))
                number = Double.MinValue;

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

        /// <summary>
        /// Produces a string representation (for Lucene indexing) of all items in the list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns></returns>
        public static string ToLuceneFullTextString(this IList list)
        {
            var buffer = new StringBuilder();

            foreach (var item in list)
            {
                if (item == null)
                    continue;

                buffer.Append(item.ToLuceneFullTextString());
            }

            return buffer.ToString();
        }

        private static string ToLuceneFullTextString(this object value)
        {
            if (value == null)
                return String.Empty;

            var buffer = new StringBuilder();
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
                    buffer.AppendFormat("{0}\r\n", value.ToString());
                    break;

                case TypeCode.Boolean:
                    buffer.AppendFormat("{0}\r\n", value.ToString().ToLower());
                    break;

                case TypeCode.DateTime:
                    buffer.AppendFormat("{0}\r\n", ((DateTime)value).ToString("yyyy-MM-dd"));
                    break;

                case TypeCode.String:
                    buffer.AppendFormat("{0}\r\n", value as string);
                    break;

                case TypeCode.Object:
                    if (type == typeof(Guid) || type == typeof(Guid?))
                    {
                        buffer.AppendFormat("{0}\r\n", ((Guid)value).ToString());
                    }
                    else if (type is IList)
                    {
                        var list2 = value as IList;
                        buffer.AppendFormat("{0}\r\n", list2.ToLuceneFullTextString());

                    }
                    else if (type is IDictionary<string, object>)
                    {
                        var dictionary2 = value as IDictionary<string, object>;
                        buffer.AppendFormat("{0}\r\n", dictionary2.ToLuceneFullTextString());
                    }
                    break;

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

                buffer.Append(field.ToLuceneFullTextString());
            }

            return buffer.ToString();
        }
    }
}
