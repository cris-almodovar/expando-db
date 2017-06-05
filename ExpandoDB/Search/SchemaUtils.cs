using ExpandoDB.Serialization;
using Mapster;
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
            var dictionary = document.AsDictionary();
            return dictionary.ToSchema();
        }

        /// <summary>
        /// To the schema.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns></returns>
        public static Schema ToSchema (this IDictionary<string, object> dictionary)
        {            
            var fieldsCollection = dictionary["Fields"] as IEnumerable;
            dictionary.Remove("Fields");

            var schema = TypeAdapter.Adapt<Schema>(dictionary);            

            if (fieldsCollection != null)
            {
                var enumerator = fieldsCollection.GetEnumerator();
                var fieldList = new List<Schema.Field>();

                while (enumerator.MoveNext())
                {
                    var item = enumerator.Current;
                    if (item != null)
                    {
                        var fieldDictionary = item as IDictionary<string, object>;
                        if (fieldDictionary != null)
                        {
                            var field = new Schema.Field().PopulateWith(fieldDictionary);
                            fieldList.Add(field);
                        }
                    }
                }

                if (fieldList.Count > 0)
                {
                    schema.Fields = new Schema.FieldsCollection();
                    fieldList.ForEach(field => schema.Fields.Add(field));
                }
            }

            return schema;
        }

        /// <summary>
        /// Converts the Schema object to a Document object.
        /// </summary>
        /// <param name="schema">The Schema.</param>
        /// <returns></returns>
        public static Document ToDocument(this Schema schema)
        {            
            var dictionary = schema.ToCompatibleDictionary();            
            var document = new Document(dictionary);
            return document;
        }


        /// <summary>
        /// Converts the Schema object to a dictionary.
        /// </summary>
        /// <param name="schema">The schema.</param>
        /// <returns></returns>
        internal static IDictionary<string, object> ToDictionary(this Schema schema)
        {
            var dictionary = schema.ToCompatibleDictionary();
            return dictionary;
        }

        /// <summary>
        /// Converts the Schema Field object to a dictionary.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <returns></returns>
        internal static IDictionary<string, object> ToDictionary(this Schema.Field field)
        {
            var dictionary = field.ToCompatibleDictionary();            
            return dictionary;
        }

        /// <summary>
        /// Converts the Schema FacetSettings object to a dictionary.
        /// </summary>
        /// <param name="facetSettings">The facet settings.</param>
        /// <returns></returns>
        internal static IDictionary<string, object> ToDictionary(this Schema.FacetSettings facetSettings)
        {
            var dictionary = facetSettings.ToCompatibleDictionary();
            return dictionary;
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

            if (!dictionary.ContainsKey("Name"))
                throw new SchemaException("Schema.Field.Name is mandatory.");
            if (!dictionary.ContainsKey("DataType"))
                throw new SchemaException("Schema.Field.DataType is mandatory.");

            var objectSchemaDictionary = dictionary.ContainsKey("ObjectSchema") ?
                                         dictionary["ObjectSchema"] as IDictionary<string, object> :
                                         null;            

            var facetSettingsDictionary = dictionary.ContainsKey("FacetSettings") ?
                                          dictionary["FacetSettings"] as IDictionary<string, object> :
                                          null;

            dictionary.Remove("ObjectSchema");
            dictionary.Remove("FacetSettings");

            var dataType = dictionary.ContainsKey("DataType") ?
                           (Schema.DataType)Enum.Parse(typeof(Schema.DataType), dictionary["DataType"].ToString()) :
                           Schema.DataType.Null;

            var arrayElementDataType = dictionary.ContainsKey("ArrayElementDataType") ?
                           (Schema.DataType)Enum.Parse(typeof(Schema.DataType), dictionary["ArrayElementDataType"].ToString()) :
                           Schema.DataType.Null;

            dictionary["DataType"] = dataType;
            dictionary["ArrayElementDataType"] = arrayElementDataType;

            dictionary.Adapt(field);

            if (objectSchemaDictionary != null)
                field.ObjectSchema = objectSchemaDictionary.ToSchema();
            if (facetSettingsDictionary != null)
                field.FacetSettings = new Schema.FacetSettings().PopulateWith(facetSettingsDictionary);
            
               
            return field;
        }


        /// <summary>
        /// Populates the Schema.FacetSettings object with values from the given dictionary.
        /// </summary>
        /// <param name="facetSettings">The facet settings.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">dictionary</exception>
        internal static Schema.FacetSettings PopulateWith(this Schema.FacetSettings facetSettings, IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            if (!dictionary.ContainsKey("FacetName"))
                throw new SchemaException("Schema.FacetSettings.FacetName is mandatory.");

            dictionary.Adapt(facetSettings);
            return facetSettings;
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
                foreach (var field in schema.Fields)
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
