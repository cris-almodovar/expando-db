using ExpandoDB.Search;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

namespace ExpandoDB
{
    /// <summary>
    /// Represents the set of fields for a DocumentCollection.
    /// </summary>    
    public class Schema
    {
        public string Name { get; set; }
        public ConcurrentDictionary<string, Field> Fields { get; set; } = new ConcurrentDictionary<string, Field>();       

        public Schema(string name) 
        {
            Name = name;            
        }

        /// <summary>
        /// Creates a default IndexSchema, which contains the _id, _createdTimestamp, _modifiedTimestamp, and _full_text_ fields.
        /// </summary>
        /// <param name="name">The name of the IndexSchema.</param>
        /// <returns></returns>
        public static Schema CreateDefault(string name = null)
        {            
            if (String.IsNullOrWhiteSpace(name))
                name = "Default";

            var schema = new Schema(name);
            schema.Fields[StandardField.ID] = new Field { Name = StandardField.ID, DataType = DataType.Guid };
            schema.Fields[StandardField.CREATED_TIMESTAMP] = new Field { Name = StandardField.CREATED_TIMESTAMP, DataType = DataType.DateTime };
            schema.Fields[StandardField.CREATED_TIMESTAMP] = new Field { Name = StandardField.CREATED_TIMESTAMP, DataType = DataType.DateTime };
            schema.Fields[StandardField.FULL_TEXT] = new Field { Name = StandardField.FULL_TEXT, DataType = DataType.Text };
            
            return schema;      
        }

        /// <summary>
        /// Finds (recursively) the field with the specified name.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="recursive">if set to <c>true</c>, the FindField method will search child objects.</param>
        /// <returns></returns>
        public Field FindField(string fieldName, bool recursive = true)
        {
            if (Fields.ContainsKey(fieldName))
                return Fields[fieldName];

            Field foundField = null;
            if (recursive)
            {
                foreach (var field in Fields.Values)
                {
                    if (field.DataType == DataType.Array || field.DataType == DataType.Object)
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

        [Serializable]
        public static class StandardField
        {
            public const string ID = "_id";
            public const string CREATED_TIMESTAMP = "_createdTimestamp";
            public const string MODIFIED_TIMESTAMP = "_modifiedTimestamp";
            public const string FULL_TEXT = "_full_text_";
        }

        [Serializable]
        public class Field
        {
            public string Name { get; set; }
            public DataType DataType { get; set; }
            public DataType ArrayElementDataType { get; set; }
            public Schema ObjectSchema { get; set; }
            public bool IsTopLevel { get { return !(Name ?? String.Empty).Contains(".") && !IsArrayElement; } }
            public bool IsSortable { get { return IsTopLevel && DataType != DataType.Array && DataType != DataType.Object; } }
            public bool IsArrayElement { get; set; }
        }

        [Serializable]
        public enum DataType
        {
            Null,
            Guid,
            Text,
            Number,
            Boolean,
            DateTime,
            Array,
            Object
        }

    }   
    
    

    
    
}
