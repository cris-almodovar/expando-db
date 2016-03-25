using FlexLucene.Document;
using FlexLucene.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuceneDocument = FlexLucene.Document.Document;
using LuceneDouble = java.lang.Double;
using LuceneLong = java.lang.Long;
using LuceneInteger = java.lang.Integer;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Implements extension methods for converting Content objects to Lucene documents.
    /// </summary>
    public static class LuceneExtensions
    {
        public const string FULL_TEXT_FIELD_NAME = "_full_text_";     
        public const string DEFAULT_NULL_TOKEN = "_null_";
        public const int INDEX_NULL_VALUE = 1;
        public static readonly LuceneDouble DOUBLE_MIN_VALUE = new LuceneDouble(LuceneDouble.MIN_VALUE);
        public static readonly LuceneDouble DOUBLE_MAX_VALUE = new LuceneDouble(LuceneDouble.MAX_VALUE);
        public static readonly LuceneLong LONG_MIN_VALUE = new LuceneLong(LuceneLong.MIN_VALUE);
        public static readonly LuceneLong LONG_MAX_VALUE = new LuceneLong(LuceneLong.MAX_VALUE);

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

            // The full-text field is always auto-generated and added to the Lucene document.
            var fullText = content.ToLuceneFullTextString();
            luceneDocument.Add(new TextField(FULL_TEXT_FIELD_NAME, fullText, FieldStore.NO));

            return luceneDocument;
        }

        /// <summary>
        /// Generates Lucene fields for the given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="indexedField">The indexed field.</param>
        /// <returns></returns>
        public static IList<Field> ToLuceneFields(this object value, IndexedField indexedField)
        {
            if (indexedField == null)
                throw new ArgumentNullException(nameof(indexedField));

            var luceneFields = new List<Field>();  // This will contain the generated Lucene fields for the passed in value.

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
                    indexedField.ValidateDataType(FieldDataType.Number);
                    luceneFields.AddNumberField(indexedField, value);
                    break;

                case TypeCode.Boolean:
                    indexedField.ValidateDataType(FieldDataType.Boolean);
                    luceneFields.AddBooleanField(indexedField, value);                   
                    break;

                case TypeCode.String:
                    indexedField.ValidateDataType(FieldDataType.Text);
                    luceneFields.AddTextField(indexedField, value);
                    break;

                case TypeCode.DateTime:
                    indexedField.ValidateDataType(FieldDataType.DateTime);
                    luceneFields.AddDateTimeField(indexedField, value);                    
                    break;

                case TypeCode.Object:
                    if (fieldType == typeof(Guid) || fieldType == typeof(Guid?))
                    {
                        indexedField.ValidateDataType(FieldDataType.Guid);
                        luceneFields.AddGuidField(indexedField, value);
                    }
                    else if (value is IList)
                    {
                        indexedField.ValidateDataType(FieldDataType.Array);

                        var list = value as IList;
                        luceneFields.AddRange(list.ToLuceneFields(indexedField));
                    }
                    else if (value is IDictionary<string, object>)
                    {
                        indexedField.ValidateDataType(FieldDataType.Object);

                        var dictionary = value as IDictionary<string, object>;
                        luceneFields.AddRange(dictionary.ToLuceneFields(indexedField));
                    }
                    break;

                case TypeCode.Empty: // value is null
                    luceneFields.AddNullField(indexedField);
                    break;
            }

            return luceneFields;
        }
        

        private static void ValidateDataType(this IndexedField indexedField, FieldDataType dataType)
        {
            if (indexedField.DataType == FieldDataType.Unknown)
            {
                indexedField.DataType = dataType;
            }
            else
            {
                if ( !(indexedField.DataType == dataType || indexedField.DataType == FieldDataType.Array) )
                {
                    var message = $"Cannot change the data type of the field '{indexedField.Name}' from {indexedField.DataType} to {dataType}.";
                    throw new IndexSchemaException(message);
                }
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
                        throw new IndexSchemaException($"All the elements of '{indexedField.Name}' must be of type '{indexedField.DataType}'");

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
                    Name = $"{parentIndexedField.Name}.{fieldName}",
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
                    else if (value is IList)
                    {
                        var list2 = value as IList;
                        buffer.AppendFormat("{0}\r\n", list2.ToLuceneFullTextString());

                    }
                    else if (value is IDictionary<string, object>)
                    {
                        var dictionary2 = value as IDictionary<string, object>;
                        buffer.AppendFormat("{0}\r\n", dictionary2.ToLuceneFullTextString());
                    }
                    break;

            }

            return buffer.ToString();
        }

        /// <summary>
        /// Produces a string representation (for Lucene indexing) of all items in the dictionary.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Finds (recursively) the field with the specified name.
        /// </summary>
        /// <param name="indexSchema">The indexSchema.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        public static IndexedField FindField(this IndexSchema indexSchema, string fieldName, bool recursive = true)
        {
            if (indexSchema.Fields.ContainsKey(fieldName))
                return indexSchema.Fields[fieldName];

            IndexedField foundField = null;
            if (recursive)
            {
                foreach (var indexedField in indexSchema.Fields.Values)
                {
                    if (indexedField.DataType == FieldDataType.Array || indexedField.DataType == FieldDataType.Object)
                    {
                        var childSchema = indexedField.ObjectSchema;
                        if (childSchema != null)
                        {
                            foundField = childSchema.FindField(fieldName, true);
                            if (foundField != null)
                                break;
                        }
                    }
                }
            }

            return foundField;
        }

        /// <summary>
        /// Converts the given Lucene field name to a special field name for use in sorting.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        public static string ToSortFieldName(this string fieldName)
        {
            return $"__{fieldName}_sort__";

        }

        /// <summary>
        /// Converts the given Lucene field name to a special field name for use in tagging fields with null value.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        public static string ToNullFieldName(this string fieldName)
        {
            return $"__{fieldName}_null__";

        }

        /// <summary>
        /// Adds a Number field to the given list of Lucene fields.
        /// </summary>
        /// <param name="luceneFields">The lucene fields.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="value">The value.</param>           
        private static void AddNumberField(this List<Field> luceneFields, IndexedField indexedField, object value)
        {
            var doubleValue = Convert.ToDouble(value);
            var fieldName = indexedField.Name.Trim();

            luceneFields.Add(new DoubleField(fieldName, doubleValue, FieldStore.NO));

            // Only top-level and non-array fields are sortable
            if (indexedField.IsTopLevel && indexedField.DataType != FieldDataType.Array && indexedField.DataType != FieldDataType.Object)
            {
                var sortFieldName = fieldName.ToSortFieldName();
                luceneFields.Add(new DoubleDocValuesField(sortFieldName, doubleValue));
            }
        }


        /// <summary>
        /// Adds a Booean field to the given list of Lucene fields.
        /// </summary>
        /// <param name="luceneFields">The lucene fields.</param>
        /// <param name="indexedField">The indexed field.</param>
        /// <param name="value">The value.</param>        
        private static void AddBooleanField(this List<Field> luceneFields, IndexedField indexedField, object value)
        {
            var booleanString = value.ToString().ToLower();
            var fieldName = indexedField.Name.Trim();

            luceneFields.Add(new StringField(fieldName, booleanString, FieldStore.NO));

            // Only top-level and non-array fields are sortable
            if (indexedField.IsTopLevel && indexedField.DataType != FieldDataType.Array && indexedField.DataType != FieldDataType.Object)
            {
                var sortFieldName = fieldName.ToSortFieldName();
                luceneFields.Add(new SortedDocValuesField(sortFieldName, new BytesRef(booleanString)));
            }
        }

        /// <summary>
        /// Adds a Text field to the given list of Lucene fields.
        /// </summary>
        /// <param name="luceneFields">The lucene fields.</param>
        /// <param name="indexedField">The indexed field.</param>
        /// <param name="value">The value.</param>
        private static void AddTextField(this List<Field> luceneFields, IndexedField indexedField, object value)
        {
            var stringValue = (string)value;
            var fieldName = indexedField.Name.Trim();

            luceneFields.Add(new TextField(fieldName, stringValue, FieldStore.NO));

            // Only top-level and non-array fields are sortable
            if (indexedField.IsTopLevel && indexedField.DataType != FieldDataType.Array && indexedField.DataType != FieldDataType.Object)
            {
                var stringValueForSorting = (stringValue.Length > 50 ? stringValue.Substring(0, 50) : stringValue).Trim().ToLowerInvariant();
                var sortFieldName = fieldName.ToSortFieldName();
                luceneFields.Add(new SortedDocValuesField(sortFieldName, new BytesRef(stringValueForSorting)));
            }
        }

        /// <summary>
        /// Adds a DateTime field to the given list of Lucene fields.
        /// </summary>
        /// <param name="luceneFields">The lucene fields.</param>
        /// <param name="indexedField">The indexed field.</param>
        /// <param name="value">The value.</param>        
        private static void AddDateTimeField(this List<Field> luceneFields, IndexedField indexedField, object value)
        {
            var dateTimeValue = (DateTime)value;
            var dateTimeTicks = dateTimeValue.Ticks;
            var fieldName = indexedField.Name.Trim();

            luceneFields.Add(new LongField(fieldName, dateTimeTicks, FieldStore.NO));

            // Only top-level and non-array fields are sortable
            if (indexedField.IsTopLevel && indexedField.DataType != FieldDataType.Array && indexedField.DataType != FieldDataType.Object)
            {
                var sortFieldName = fieldName.ToSortFieldName();
                luceneFields.Add(new NumericDocValuesField(sortFieldName, dateTimeTicks));
            }
        }


        /// <summary>
        /// Adds a Guid field to the given list of Lucene fields.
        /// </summary>
        /// <param name="luceneFields">The lucene fields.</param>
        /// <param name="indexedField">The indexed field.</param>
        /// <param name="value">The value.</param>
        private static void AddGuidField(this List<Field> luceneFields, IndexedField indexedField, object value)
        {
            var guidValue = ((Guid)value).ToString().ToLower();
            var isStored = (indexedField.Name == Content.ID_FIELD_NAME ? FieldStore.YES : FieldStore.NO);
            var fieldName = indexedField.Name.Trim();

            luceneFields.Add(new StringField(fieldName, guidValue, isStored));

            // Only top-level and non-array fields are sortable
            if (indexedField.IsTopLevel && indexedField.DataType != FieldDataType.Array && indexedField.DataType != FieldDataType.Object)
            {
                var sortFieldName = fieldName.ToSortFieldName();
                luceneFields.Add(new SortedDocValuesField(sortFieldName, new BytesRef(guidValue)));
            }
        }

        /// <summary>
        /// Adds a null field to the given list of Lucene fields.
        /// </summary>
        /// <param name="luceneFields">The lucene fields.</param>
        /// <param name="indexedField">The indexed field.</param>        
        private static void AddNullField(this List<Field> luceneFields, IndexedField indexedField)
        {
            var fieldName = indexedField.Name.Trim().ToNullFieldName();
            luceneFields.Add(new IntField(fieldName, INDEX_NULL_VALUE, FieldStore.NO));            
        }
    }
}
