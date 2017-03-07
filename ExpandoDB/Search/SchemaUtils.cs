using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Implements utility methods for coverting between Schema and Document objects.
    /// </summary>
    public static class SchemaUtils
    {
        /// <summary>
        /// Converts the Document object to a Schema object.
        /// </summary>
        /// <param name="document">The Document.</param>
        /// <returns></returns>
        public static Schema ToSchema(this Document document)
        {
            var schema = new Schema().PopulateWith(document.AsDictionary());    
            return schema;
        }

        /// <summary>
        /// Converts the Schema object to a Document object.
        /// </summary>
        /// <param name="schema">The Schema.</param>
        /// <returns></returns>
        public static Document ToDocument(this Schema schema)
        {            
            var document = new Document();
            document._id = schema._id ?? Guid.NewGuid();

            dynamic expando = document.AsExpando();
            expando.Name = schema.Name;            

            var fieldsList = schema.Fields.Values.Select(f => f.ToDictionary()).ToList();
            expando.Fields = fieldsList;

            expando._createdTimestamp = schema._createdTimestamp;
            expando._modifiedTimestamp = schema._modifiedTimestamp;

            return document;
        }


        /// <summary>
        /// Converts the Schema object to a dictionary.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <returns></returns>
        internal static IDictionary<string, object> ToDictionary(this Schema schema)
        {
            var dictionary = new Dictionary<string, object>();
            dictionary[Schema.MetadataField.ID] = schema._id;
            dictionary["Name"] = schema.Name;

            var fieldsList = schema.Fields.Values.Select(f => f.ToDictionary()).ToList();
            dictionary["Fields"] = fieldsList;

            dictionary[Schema.MetadataField.CREATED_TIMESTAMP] = schema._createdTimestamp;
            dictionary[Schema.MetadataField.MODIFIED_TIMESTAMP] = schema._modifiedTimestamp;

            return dictionary;
        }

        /// <summary>
        /// Converts the Schema Field object to a dictionary.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        internal static IDictionary<string, object> ToDictionary(this Schema.Field field)
        {
            var dictionary = new Dictionary<string, object>();
            dictionary["Name"] = field.Name;
            dictionary["DataType"] = field.DataType;
            dictionary["ArrayElementDataType"] = field.ArrayElementDataType;
            dictionary["IsArrayElement"] = field.IsArrayElement;

            if (field.ObjectSchema != null)
                dictionary["ObjectSchema"] = field.ObjectSchema.ToDictionary();

            return dictionary;
        }

        /// <summary>
        /// Populates the Schema object with values from the given dictionary.
        /// </summary>
        /// <param name="schema">The Schema.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns></returns>
        internal static Schema PopulateWith(this Schema schema, IDictionary<string, object> dictionary)
        {
            schema._id = dictionary.ContainsKey(Schema.MetadataField.ID) ? (Guid?)dictionary[Schema.MetadataField.ID] : null;
            schema.Name = dictionary["Name"] as string;

            var fields = dictionary["Fields"] as IList;
            if (fields != null)
            {
                foreach (var field in fields)
                {
                    var fieldDictionary = field as IDictionary<string, object>;
                    if (fieldDictionary != null)
                    {
                        var schemaField = new Schema.Field().PopulateWith(fieldDictionary);
                        schema.Fields.TryAdd(schemaField.Name, schemaField);
                    }
                }
            }

            schema._createdTimestamp = (DateTime?)dictionary[Schema.MetadataField.CREATED_TIMESTAMP];
            schema._modifiedTimestamp = (DateTime?)dictionary[Schema.MetadataField.MODIFIED_TIMESTAMP];

            return schema;
        }

        /// <summary>
        /// Populates the Schema.Field object with values from the given dictionary.
        /// </summary>
        /// <param name="field">The Schema.Field.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        internal static Schema.Field PopulateWith(this Schema.Field field, IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            field.Name = dictionary["Name"] as string;
            field.DataType = (Schema.DataType)dictionary["DataType"];
            field.ArrayElementDataType = (Schema.DataType)dictionary["ArrayElementDataType"];
            field.IsArrayElement = (bool)dictionary["IsArrayElement"];

            if (dictionary.ContainsKey("ObjectSchema"))
            {
                var schemaDictionary = dictionary["ObjectSchema"] as IDictionary<string, object>;
                if (schemaDictionary != null)
                    field.ObjectSchema = new Schema().PopulateWith(schemaDictionary);
            }

            return field;
        }

        /// <summary>
        /// Searches (recursively) within the Schema object, to find the Schema Field with the specified name.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <param name="fieldName">Name of the Schema.Field.</param>
        /// <param name="recursive">if set to <c>true</c>, the FindField method will search child objects.</param>
        /// <returns></returns>
        public static Schema.Field FindField(this Schema schema, string fieldName, bool recursive = true)
        {
            if (schema.Fields.ContainsKey(fieldName))
                return schema.Fields[fieldName];

            Schema.Field foundField = null;
            if (recursive)
            {
                foreach (var field in schema.Fields.Values)
                {
                    if (field.DataType == Schema.DataType.Array || field.DataType == Schema.DataType.Object)
                    {
                        var childSchema = field.ObjectSchema;
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
        /// Determines whether this instance is a default schema
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <returns></returns>
        internal static bool IsDefault(this Schema schema)
        {
            if (schema.Name == "Default")
                return true;

            return (schema.Fields.Count == 4 &&
                    schema.Fields.ContainsKey(Schema.MetadataField.ID) &&
                    schema.Fields.ContainsKey(Schema.MetadataField.CREATED_TIMESTAMP) &&
                    schema.Fields.ContainsKey(Schema.MetadataField.MODIFIED_TIMESTAMP) &&
                    schema.Fields.ContainsKey(Schema.MetadataField.FULL_TEXT));
        }
    }
}
