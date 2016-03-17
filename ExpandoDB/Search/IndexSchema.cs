using ExpandoDB.Search;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

namespace ExpandoDB
{
    /// <summary>
    /// Represents the set of indexed fields for a ContentCollection.
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
            indexSchema.Fields[Content.ID_FIELD_NAME] = new IndexedField { Name = Content.ID_FIELD_NAME, DataType = FieldDataType.Guid };
            indexSchema.Fields[Content.CREATED_TIMESTAMP_FIELD_NAME] = new IndexedField { Name = Content.CREATED_TIMESTAMP_FIELD_NAME, DataType = FieldDataType.DateTime };
            indexSchema.Fields[Content.MODIFIED_TIMESTAMP_FIELD_NAME] = new IndexedField { Name = Content.MODIFIED_TIMESTAMP_FIELD_NAME, DataType = FieldDataType.DateTime };
            indexSchema.Fields[LuceneExtensions.FULL_TEXT_FIELD_NAME] = new IndexedField { Name = LuceneExtensions.FULL_TEXT_FIELD_NAME, DataType = FieldDataType.Text };

            return indexSchema;      
        }         
                   
    }   

    public class IndexedField
    {
        public string Name { get; set; }
        public FieldDataType DataType { get; set; }
        public FieldDataType ArrayElementDataType { get; set; }
        public IndexSchema ObjectSchema { get; set; }
        public bool IsTopLevel { get { return (Name ?? String.Empty).IndexOf('.') < 0; } }
    }

    public enum FieldDataType
    {       
        Unknown,      
        Guid,
        Text,
        Number,
        Boolean,
        DateTime,
        Array,
        Object
    }   
    
}