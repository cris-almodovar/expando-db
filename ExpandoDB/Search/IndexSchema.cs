using System;
using System.Collections.Concurrent;

namespace ExpandoDB
{
    public class IndexSchema
    {
        public string Name { get; set; }
        public ConcurrentDictionary<string, IndexedField> Fields { get; set; }  

        public static IndexSchema CreateDefault(string name = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                name = "Default";

            var indexSchema = new IndexSchema
            {
                Name = name,
                Fields = new ConcurrentDictionary<string, IndexedField>()                
            };

            indexSchema.Fields[Content.ID_FIELD_NAME] = new IndexedField { Name = Content.ID_FIELD_NAME, DataType = FieldDataType.String };
            indexSchema.Fields[Content.CREATED_TIMESTAMP_FIELD_NAME] = new IndexedField { Name = Content.CREATED_TIMESTAMP_FIELD_NAME, DataType = FieldDataType.DateTime };
            indexSchema.Fields[Content.MODIFIED_TIMESTAMP_FIELD_NAME] = new IndexedField { Name = Content.MODIFIED_TIMESTAMP_FIELD_NAME, DataType = FieldDataType.DateTime };

            return indexSchema;      
        }               
    }   

    public class IndexedField
    {
        public string Name { get; set; }
        public FieldDataType DataType { get; set; }
        public FieldDataType ArrayElementDataType { get; set; }
        public IndexSchema ObjectSchema { get; set; }
    }

    public enum FieldDataType
    {
        String,
        Text,
        Number,
        DateTime,
        Array,
        Object
    }   
    
}