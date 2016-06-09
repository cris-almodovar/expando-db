using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Search
{
    public static class SchemaExtensions
    {
        public static Schema ToSchema(this Document document)
        {
            var dictionary = document.AsDictionary();
            var schema = new Schema();

            schema._id = (Guid)dictionary[Schema.StandardField.ID];
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

            schema._createdTimestamp = (DateTime?)dictionary[Schema.StandardField.CREATED_TIMESTAMP];
            schema._modifiedTimestamp = (DateTime?)dictionary[Schema.StandardField.MODIFIED_TIMESTAMP];

            return schema;
        }

        public static Document ToDocument(this Schema schema)
        {            
            var document = new Document();
            document._id = schema._id;            

            dynamic expando = document.AsExpando();
            expando.Name = schema.Name;            

            var fieldsList = schema.Fields.Values.Select(f => f.ToDictionary()).ToList();
            expando.Fields = fieldsList;

            expando._createdTimestamp = schema._createdTimestamp;
            expando._modifiedTimestamp = schema._modifiedTimestamp;

            return document;
        }

        public static IDictionary<string, object> ToDictionary(this Schema schema)
        {
            var dictionary = new Dictionary<string, object>();
            dictionary[Schema.StandardField.ID] = schema._id;
            dictionary["Name"] = schema.Name;

            var fieldsList = schema.Fields.Values.Select(f => f.ToDictionary()).ToList();
            dictionary["Fields"] = fieldsList;

            dictionary[Schema.StandardField.CREATED_TIMESTAMP] = schema._createdTimestamp;
            dictionary[Schema.StandardField.MODIFIED_TIMESTAMP] = schema._modifiedTimestamp;

            return dictionary;
        }

        public static IDictionary<string, object> ToDictionary(this Schema.Field field)
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

        public static Schema PopulateWith(this Schema schema, IDictionary<string, object> dictionary)
        {
            schema._id = (Guid)dictionary[Schema.StandardField.ID];
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

            schema._createdTimestamp = (DateTime?)dictionary[Schema.StandardField.CREATED_TIMESTAMP];
            schema._modifiedTimestamp = (DateTime?)dictionary[Schema.StandardField.MODIFIED_TIMESTAMP];

            return schema;
        }

        public static Schema.Field PopulateWith(this Schema.Field field, IDictionary<string, object> dictionary)
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
        /// Finds (recursively) the field with the specified name.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
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
    }
}
