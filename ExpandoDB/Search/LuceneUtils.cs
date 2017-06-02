using FlexLucene.Document;
using FlexLucene.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LuceneDocument = FlexLucene.Document.Document;
using JavaDouble = java.lang.Double;
using JavaLong = java.lang.Long;
using JavaInteger = java.lang.Integer;
using FlexLucene.Facet;
using Common.Logging;
using FlexLucene.Analysis;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Implements utility methods for converting Document objects to Lucene Documents.
    /// </summary>
    public static class LuceneUtils
    {        
        internal const string DEFAULT_NULL_TOKEN = "_null_";
        internal const string QUERY_PARSER_ILLEGAL_CHARS = @"[\+&|!\(\)\{\}\[\]^""~\*\?:\\/ ]";
        internal const int INDEX_NULL_VALUE = 1; // This is a marker value for NULL in the Lucene index.
        internal const int DOCVALUE_FIELD_MAX_TEXT_LENGTH = 256;
        internal static readonly JavaDouble DOUBLE_MIN_VALUE = new JavaDouble(Double.MinValue);
        internal static readonly JavaDouble DOUBLE_MAX_VALUE = new JavaDouble(Double.MaxValue);
        internal static readonly JavaLong LONG_MIN_VALUE = new JavaLong(Int64.MinValue);
        internal static readonly JavaLong LONG_MAX_VALUE = new JavaLong(Int64.MaxValue);
        private static readonly System.Text.RegularExpressions.Regex _queryParserIllegalCharsRegex = new System.Text.RegularExpressions.Regex(QUERY_PARSER_ILLEGAL_CHARS);
        private static readonly ILog _log = LogManager.GetLogger(nameof(LuceneUtils));


        /// <summary>
        /// Converts a <see cref="Document" /> object to a <see cref="LuceneDocument" /> object.
        /// </summary>
        /// <param name="document">The Document object</param>
        /// <param name="schema">The schema.</param>
        /// <param name="facetBuilder">The Lucene facet builder.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException">Cannot index a Document that does not have an _id.</exception>
        /// <exception cref="SchemaException">The fieldName '{fieldName}'</exception>        
        public static LuceneDocument ToLuceneDocument(this Document document, Schema schema = null, LuceneFacetBuilder facetBuilder = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (schema == null)
                schema = Schema.CreateDefault();            

            var documentDictionary = document.AsDictionary();
            if (!documentDictionary.ContainsKey(Schema.MetadataField.ID))
                throw new InvalidOperationException("Cannot index a Document that does not have an _id.");

            var luceneDocument = new LuceneDocument();

            // Make sure the _id field is the first field added to the Lucene document
            var keys = documentDictionary.Keys.Except(new[] { Schema.MetadataField.ID }).ToList();
            keys.Insert(0, Schema.MetadataField.ID);

            foreach (var fieldName in keys)
            {
                try
                {
                    // Validate fieldName - must not contain space or Lucene QueryParser illegal characters.
                    if (_queryParserIllegalCharsRegex.IsMatch(fieldName))
                    {
                        _log.Warn($"The fieldName '{fieldName}' contains illegal characters. This field will NOT be indexed.");
                        continue;
                    }

                    Schema.Field schemaField = null;
                    if (!schema.Fields.TryGetValue(fieldName, out schemaField))
                    {
                        schemaField = new Schema.Field
                        {
                            Name = fieldName
                        };
                        schema.Fields.TryAdd(fieldName, schemaField);
                    }

                    var fieldValue = documentDictionary[fieldName];
                    var luceneFields = fieldValue.ToLuceneFields(schemaField);
                    foreach (var luceneField in luceneFields)
                        luceneDocument.Add(luceneField);
                }
                catch (Exception ex)
                {
                    _log.Warn($"Cannot index the field '{fieldName}', for document: {document._id}. {ex}");
                }
            }

            // The full-text field is always auto-generated and added to the Lucene document.
            var fullText = document.ToLuceneFullTextString();
            luceneDocument.Add(new TextField(Schema.MetadataField.FULL_TEXT, fullText, FieldStore.NO));


            // Check if the Document has any Fields that are configured as Facets.
            // If there are then we need to create Facets for them.
            if (schema.Fields.Any(item => item.IsFacet))
            {
                try
                {
                    luceneDocument = facetBuilder.RebuildDocumentWithFacets(luceneDocument, document, schema);
                }
                catch (Exception ex)
                {
                    _log.Warn($"An error occurred while creating Facets for document: {document._id}. Details: {ex.Message}");
                }
            }

            return luceneDocument;
        }

        /// <summary>
        /// Generates Lucene fields for the given value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="schemaField">The schema field.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">schemaField</exception>
        public static IList<Field> ToLuceneFields(this object value, Schema.Field schemaField)
        {
            if (schemaField == null)
                throw new ArgumentNullException(nameof(schemaField));

            var luceneFields = new List<Field>();  // This will contain the generated Lucene fields for the passed in value.

            var fieldName = schemaField.Name.Trim();            
            var fieldDataType = GetFieldDataType(value);

            var isValid = schemaField.ValidateAndUpdateDataType(fieldDataType);
            if (isValid)
            {
                switch (schemaField.DataType)
                {
                    case Schema.DataType.Number:
                        luceneFields.AddNumberField(schemaField, value);
                        break;

                    case Schema.DataType.Boolean:
                        luceneFields.AddBooleanField(schemaField, value);
                        break;

                    case Schema.DataType.Text:                        
                        luceneFields.AddTextField(schemaField, value);
                        break;

                    case Schema.DataType.DateTime:
                        luceneFields.AddDateTimeField(schemaField, value);
                        break;

                    case Schema.DataType.Guid:
                        luceneFields.AddGuidField(schemaField, value);
                        break;

                    case Schema.DataType.Array:
                        var list = value as IList;
                        luceneFields.AddRange(list.ToLuceneFields(schemaField));
                        break;

                    case Schema.DataType.Object:
                        var dictionary = value as IDictionary<string, object>;
                        if (schemaField.ObjectSchema == null)
                            schemaField.ObjectSchema = new Schema { Name = schemaField.Name };
                        luceneFields.AddRange(dictionary.ToLuceneFields(schemaField));
                        break;

                    case Schema.DataType.Null:
                        luceneFields.AddNullField(schemaField);
                        break;
                }
            }

            return luceneFields;
        }


        /// <summary>
        /// Validates the type of the data.
        /// </summary>
        /// <param name="schemaField">The schema field.</param>
        /// <param name="newDataType">New type of the data.</param>
        /// <returns></returns>
        private static bool ValidateAndUpdateDataType(this Schema.Field schemaField, Schema.DataType newDataType)
        { 
            if (schemaField.DataType == Schema.DataType.Null)
            {
                // The field data type is not yet known; use the data type of the new data.
                schemaField.DataType = newDataType;
                schemaField.IsTokenized = (newDataType == Schema.DataType.Text);
            }
            else
            {
                if (schemaField.DataType != newDataType && newDataType != Schema.DataType.Null)
                {
                    var message = $"Cannot change the data type of the field '{schemaField.Name}' from {schemaField.DataType} to {newDataType}. The current value will not be indexed.";
                    _log.Warn(message);
                    return false;
                }
            }            

            return true;
        }

        private static Schema.DataType GetFieldDataType(object value)
        {
            if (value == null)
                return Schema.DataType.Null;

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
                    return Schema.DataType.Number;

                case TypeCode.Boolean:
                    return Schema.DataType.Boolean;

                case TypeCode.String:
                    return Schema.DataType.Text;

                case TypeCode.DateTime:
                    return Schema.DataType.DateTime;

                case TypeCode.Object:
                    if (type == typeof(Guid) || type == typeof(Guid?))
                        return Schema.DataType.Guid;
                    else if (value is IList)
                        return Schema.DataType.Array;
                    else if (value is IDictionary<string, object>)
                        return Schema.DataType.Object;
                    break;

                case TypeCode.Empty:
                    return Schema.DataType.Null;
            }

            throw new SchemaException($"Unsupported data type: '{type.Name}'");
        }

        private static List<Field> ToLuceneFields(this IList list, Schema.Field schemaField)
        {
            var luceneFields = new List<Field>();
            if (list.Count > 0)
            {
                var arrayElementSchemaField = new Schema.Field()
                {
                    Name = schemaField.Name,
                    DataType = schemaField.ArrayElementDataType,
                    IsArrayElement = true,
                    ParentField = schemaField
                };

                foreach (var element in list)
                {
                    if (element == null)
                        continue;

                    var arrayElementDataType = GetFieldDataType(element);
                    var isValid = arrayElementSchemaField.ValidateAndUpdateDataType(arrayElementDataType);

                    if (isValid)
                    {
                        if (schemaField.ArrayElementDataType == Schema.DataType.Null)
                            schemaField.ArrayElementDataType = arrayElementSchemaField.DataType;

                        switch (arrayElementSchemaField.DataType)
                        {
                            case Schema.DataType.Guid:
                            case Schema.DataType.Text:
                            case Schema.DataType.Number:
                            case Schema.DataType.DateTime:
                            case Schema.DataType.Boolean:
                                luceneFields.AddRange(element.ToLuceneFields(arrayElementSchemaField));
                                break;

                            case Schema.DataType.Array:
                                _log.Warn("JSON with nested arrays are currently not supported. The current array element will NOT be indexed.");
                                break;

                            case Schema.DataType.Object:
                                var dictionary = element as IDictionary<string, object>;
                                if (dictionary != null)
                                {                                    
                                    if (schemaField.ObjectSchema == null)
                                        schemaField.ObjectSchema = new Schema { Name = schemaField.Name };

                                    luceneFields.AddRange(dictionary.ToLuceneFields(schemaField));
                                }
                                break;
                        }
                    }
                }                
            }

            return luceneFields;
        }       

        private static List<Field> ToLuceneFields(this IDictionary<string, object> dictionary, Schema.Field schemaField)
        {
            var luceneFields = new List<Field>();                     

            foreach (var fieldName in dictionary.Keys)
            {
                var childField = dictionary[fieldName];
                var childFieldDataType = GetFieldDataType(childField);

                Schema.Field childSchemaField = null;
                schemaField.ObjectSchema.Fields.TryGetValue($"{schemaField.Name}.{fieldName}", out childSchemaField);

                if (childSchemaField == null)
                {
                    childSchemaField = new Schema.Field
                    {
                        Name = $"{schemaField.Name}.{fieldName}",
                        DataType = childFieldDataType,
                        IsTokenized = (childFieldDataType == Schema.DataType.Text)
                    };
                    schemaField.ObjectSchema.Fields.TryAdd(childSchemaField.Name, childSchemaField);
                }                
                               
                var isValid = childSchemaField.ValidateAndUpdateDataType(childFieldDataType);
                if (isValid)
                {
                    switch (childSchemaField.DataType)
                    {
                        case Schema.DataType.Null:
                        case Schema.DataType.Guid:
                        case Schema.DataType.Text:
                        case Schema.DataType.Number:
                        case Schema.DataType.DateTime:
                        case Schema.DataType.Boolean:
                            luceneFields.AddRange(childField.ToLuceneFields(childSchemaField));
                            break;

                        case Schema.DataType.Array:
                            var array = childField as IList;
                            if (array != null)
                                luceneFields.AddRange(array.ToLuceneFields(childSchemaField));
                            break;

                        case Schema.DataType.Object:
                            var nestedDictionary = childField as IDictionary<string, object>;
                            if (nestedDictionary != null)
                            {
                                if (childSchemaField.ObjectSchema == null)
                                    childSchemaField.ObjectSchema = new Schema { Name = childSchemaField.Name };

                                luceneFields.AddRange(nestedDictionary.ToLuceneFields(childSchemaField));
                            }
                            break;
                    }
                }
            }

            return luceneFields;
        }

        /// <summary>
        /// Generates the Lucene full-text representation of the Document object.
        /// </summary>
        /// <param name="document">The Document object.</param>
        /// <returns></returns>        
        public static string ToLuceneFullTextString(this Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var buffer = new StringBuilder();

            var dictionary = document.AsDictionary();
            var keys = dictionary.Keys.Except(new[] { Schema.MetadataField.ID, Schema.MetadataField.CREATED_TIMESTAMP, Schema.MetadataField.MODIFIED_TIMESTAMP });

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
                    buffer.Append($"{value.ToString()}{Environment.NewLine}");
                    break;

                case TypeCode.Boolean:
                    buffer.Append($"{value.ToString().ToLower()}{Environment.NewLine}");
                    break;

                case TypeCode.DateTime:
                    buffer.Append($"{((DateTime)value).ToString("yyyy-MM-dd")}{Environment.NewLine}");
                    break;

                case TypeCode.String:
                    buffer.Append($"{value as string}{Environment.NewLine}");
                    break;

                case TypeCode.Object:
                    if (type == typeof(Guid) || type == typeof(Guid?))
                    {
                        buffer.Append($"{((Guid)value)}{Environment.NewLine}");
                    }
                    else if (value is IList)
                    {
                        var list2 = value as IList;
                        buffer.Append($"{list2.ToLuceneFullTextString()}{Environment.NewLine}");

                    }
                    else if (value is IDictionary<string, object>)
                    {
                        var dictionary2 = value as IDictionary<string, object>;
                        buffer.Append($"{dictionary2.ToLuceneFullTextString()}{Environment.NewLine}");
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
        /// Converts the given Lucene field name to a special field name for use in grouping/aggregation.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        public static string ToGroupingFieldName(this string fieldName)
        {
            return $"__{fieldName}_grouping__";

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
        /// <param name="schemaField">The schema field.</param>
        /// <param name="value">The value.</param>
        private static void AddNumberField(this List<Field> luceneFields, Schema.Field schemaField, object value)
        {
            // We will save numeric values as doubles converted to longs.
            var doubleValue = Convert.ToDouble(value);
            var longValue = JavaDouble.doubleToRawLongBits(doubleValue);

            // We will create 3 Lucene fields for the numeric value, one each for:
            // 1. Search
            // 2. Sorting
            // 3. Grouping/aggregation

            // Create the search field
            var fieldName = schemaField.Name.Trim();
            luceneFields.Add(new DoublePoint(fieldName, doubleValue));

            // Create the sort field; only top-level and non-array fields are sortable.         
            if (schemaField.IsSortable)
            {
                var sortFieldName = fieldName.ToSortFieldName(); 
                luceneFields.Add(new NumericDocValuesField(sortFieldName, longValue));
            }

            // Create the grouping field.
            var groupingFieldName = fieldName.ToGroupingFieldName();            
            luceneFields.Add(new SortedNumericDocValuesField(groupingFieldName, longValue));
        }


        /// <summary>
        /// Adds a Booean field to the given list of Lucene fields.
        /// </summary>
        /// <param name="luceneFields">The lucene fields.</param>
        /// <param name="schemaField">The schema field.</param>
        /// <param name="value">The value.</param>        
        private static void AddBooleanField(this List<Field> luceneFields, Schema.Field schemaField, object value)
        {
            if (!(value is bool))
                return;
            
            // We will save boolean values as integers.            
            var intValue = Convert.ToBoolean(value) ? 1 : 0;

            // We will create 3 Lucene fields for the boolean value, one each for:
            // 1. Search
            // 2. Sorting
            // 3. Grouping/aggregation

            // Create the search field
            var fieldName = schemaField.Name.Trim();
            
            luceneFields.Add(new IntPoint(fieldName, intValue));

            // Create the sort field; only top-level and non-array fields are sortable.            
            if (schemaField.IsSortable)
            {
                var sortFieldName = fieldName.ToSortFieldName();
                luceneFields.Add(new NumericDocValuesField(sortFieldName, intValue));
            }

            // Create the grouping field.
            var groupingFieldName = fieldName.ToGroupingFieldName();
            luceneFields.Add(new SortedNumericDocValuesField(groupingFieldName, intValue));
        }

        /// <summary>
        /// Adds a Text field to the given list of Lucene fields.
        /// </summary>
        /// <param name="luceneFields">The lucene fields.</param>
        /// <param name="schemaField">The schema field.</param>
        /// <param name="value">The value.</param>
        private static void AddTextField(this List<Field> luceneFields, Schema.Field schemaField, object value)
        {
            // We will save Text values as strings.
            var stringValue = value as string;
            if (String.IsNullOrWhiteSpace(stringValue))
                return;

            // We will create 3 Lucene fields for the text value, one each for:
            // 1. Search
            // 2. Sorting
            // 3. Grouping/aggregation

            // Create the search field
            var fieldName = schemaField.Name.Trim();

            if (schemaField.IsTokenized)
                luceneFields.Add(new TextField(fieldName, stringValue, FieldStore.NO));
            else
                luceneFields.Add(new StringField(fieldName, stringValue, FieldStore.NO));

            // Create the sort field; only top-level and non-array fields are sortable.    
            // For sorting, we only take the first DOCVALUE_FIELD_MAX_TEXT_LENGTH of the text, converted to lowercase.        
            if (schemaField.IsSortable)
            {
                var stringValueForSorting = (stringValue.Length > DOCVALUE_FIELD_MAX_TEXT_LENGTH ? stringValue.Substring(0, DOCVALUE_FIELD_MAX_TEXT_LENGTH) : stringValue).Trim().ToLowerInvariant();
                var sortFieldName = fieldName.ToSortFieldName();
                luceneFields.Add(new SortedDocValuesField(sortFieldName, new BytesRef(stringValueForSorting)));
            }

            // Create the grouping field.
            // For grouping, we only take the first DOCVALUE_FIELD_MAX_TEXT_LENGTH of the text; we save the text value as is (not converted to lowercase).   
            var groupingFieldName = fieldName.ToGroupingFieldName();
            var stringValueForGrouping = (stringValue.Length > DOCVALUE_FIELD_MAX_TEXT_LENGTH ? stringValue.Substring(0, DOCVALUE_FIELD_MAX_TEXT_LENGTH) : stringValue).Trim();                    
            luceneFields.Add(new SortedSetDocValuesField(groupingFieldName, new BytesRef(stringValueForGrouping)));            
        }

        /// <summary>
        /// Adds a DateTime field to the given list of Lucene fields.
        /// </summary>
        /// <param name="luceneFields">The lucene fields.</param>
        /// <param name="schemaField">The schema field.</param>
        /// <param name="value">The value.</param>        
        private static void AddDateTimeField(this List<Field> luceneFields, Schema.Field schemaField, object value)
        {
            if (!(value is DateTime))
                return;

            // We will save DateTime values as long.
            var dateTimeValue = (DateTime)value;
            var dateTimeTicks = dateTimeValue.ToUniversalTime().Ticks;

            // We will create 3 Lucene fields for the DateTime value, one each for:
            // 1. Search
            // 2. Sorting
            // 3. Grouping/aggregation

            // Create the search field
            var fieldName = schemaField.Name.Trim();
            luceneFields.Add(new LongPoint(fieldName, dateTimeTicks));

            // Create the sort field; only top-level and non-array fields are sortable.   
            if (schemaField.IsSortable)
            {
                var sortFieldName = fieldName.ToSortFieldName();
                luceneFields.Add(new NumericDocValuesField(sortFieldName, dateTimeTicks));
            }

            // Create the grouping field.
            var groupingFieldName = fieldName.ToGroupingFieldName();                       
            luceneFields.Add(new SortedNumericDocValuesField(groupingFieldName, dateTimeTicks));
        }


        /// <summary>
        /// Adds a Guid field to the given list of Lucene fields.
        /// </summary>
        /// <param name="luceneFields">The lucene fields.</param>
        /// <param name="schemaField">The schema field.</param>
        /// <param name="value">The value.</param>
        private static void AddGuidField(this List<Field> luceneFields, Schema.Field schemaField, object value)
        {
            if (!(value is Guid))
                return;

            // We will save Guid values as strings.
            var guidValue = (Guid)value;
            var guidStringValue = guidValue.ToString().ToLower();
            var isStored = (schemaField.Name == Schema.MetadataField.ID ? FieldStore.YES : FieldStore.NO);

            // We will create 3 Lucene fields for the DateTime value, one each for:
            // 1. Search
            // 2. Sorting
            // 3. Grouping/aggregation

            // Create the search field
            var fieldName = schemaField.Name.Trim();
            luceneFields.Add(new StringField(fieldName, guidStringValue, isStored));

            // Create the sort field; only top-level and non-array fields are sortable.   
            if (schemaField.IsSortable)
            {
                var sortFieldName = fieldName.ToSortFieldName();                
                luceneFields.Add(new SortedDocValuesField(sortFieldName, new BytesRef(guidStringValue))); 
            }

            // Create the grouping field.
            var groupingFieldName = fieldName.ToGroupingFieldName();            
            luceneFields.Add(new SortedSetDocValuesField(groupingFieldName, new BytesRef(guidStringValue)));            
        }

        /// <summary>
        /// Adds a null field to the given list of Lucene fields.
        /// </summary>
        /// <param name="luceneFields">The lucene fields.</param>
        /// <param name="schemaField">The schema field.</param>        
        private static void AddNullField(this List<Field> luceneFields, Schema.Field schemaField)
        {
            var fieldName = schemaField.Name.Trim().ToNullFieldName();
            luceneFields.Add(new IntPoint(fieldName, INDEX_NULL_VALUE));            
        }

        /// <summary>
        /// Converts the sequence of string values to a CharArraySet
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns></returns>
        public static CharArraySet ToCharArraySet(this IEnumerable<string> values)
        {
            var charArraySet = new CharArraySet(10, false);
            values.ToList().ForEach(item => charArraySet.add(item));

            return charArraySet;
        }
    }
}
