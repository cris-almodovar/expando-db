using ExpandoDB.Search;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

namespace ExpandoDB
{
    /// <summary>
    /// Represents the set of indexed fields for a DocumentCollection.
    /// </summary>    
    public class IndexSchema
    {
        public string Name { get; set; }
        public ConcurrentDictionary<string, IndexedField> Fields { get; set; }

        public IndexSchema()
        {
            Fields = new ConcurrentDictionary<string, IndexedField>();
        }

        public IndexSchema(string name) : this()
        {
            Name = name;            
        }

        /// <summary>
        /// Creates a default IndexSchema, which contains the _id, _createdTimestamp, _modifiedTimestamp, and _full_text_ fields.
        /// </summary>
        /// <param name="name">The name of the IndexSchema.</param>
        /// <returns></returns>
        public static IndexSchema CreateDefault(string name = null)
        {            
            if (String.IsNullOrWhiteSpace(name))
                name = "Default";

            var indexSchema = new IndexSchema(name);
            indexSchema.Fields[Document.ID_FIELD_NAME] = new IndexedField { Name = Document.ID_FIELD_NAME, DataType = FieldDataType.Guid };
            indexSchema.Fields[Document.CREATED_TIMESTAMP_FIELD_NAME] = new IndexedField { Name = Document.CREATED_TIMESTAMP_FIELD_NAME, DataType = FieldDataType.DateTime };
            indexSchema.Fields[Document.MODIFIED_TIMESTAMP_FIELD_NAME] = new IndexedField { Name = Document.MODIFIED_TIMESTAMP_FIELD_NAME, DataType = FieldDataType.DateTime };
            indexSchema.Fields[LuceneExtensions.FULL_TEXT_FIELD_NAME] = new IndexedField { Name = LuceneExtensions.FULL_TEXT_FIELD_NAME, DataType = FieldDataType.Text };
            
            return indexSchema;      
        }

        /// <summary>
        /// Finds (recursively) the field with the specified name.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="recursive">if set to <c>true</c>, the FindField method will search child objects.</param>
        /// <returns></returns>
        public IndexedField FindField(string fieldName, bool recursive = true)
        {
            if (Fields.ContainsKey(fieldName))
                return Fields[fieldName];

            IndexedField foundField = null;
            if (recursive)
            {
                foreach (var indexedField in Fields.Values)
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

    }   
    
    public class IndexedField
    {
        public string Name { get; set; }
        public FieldDataType DataType { get; set; }
        public FieldDataType ArrayElementDataType { get; set; }
        public IndexSchema ObjectSchema { get; set; }
        public bool IsTopLevel { get { return (Name ?? String.Empty).IndexOf('.') < 0 && !IsArrayElement; } }
        public bool IsArrayElement { get; set; }
    }

    [Serializable]
    public enum FieldDataType
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
