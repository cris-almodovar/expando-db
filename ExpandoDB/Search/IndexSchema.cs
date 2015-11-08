using System;
using System.Collections.Concurrent;

namespace ExpandoDB.Search
{
    public class IndexSchema
    {
        public string Name { get; set; }
        public ConcurrentDictionary<string, IndexedField> IndexedFields { get; set; }     
        public bool IsAutoPopulated { get; set; } 

        public static IndexSchema CreateDefault()
        {
            var schema = new IndexSchema
            {
                Name = String.Empty,
                IndexedFields = new ConcurrentDictionary<string, IndexedField>(),
                IsAutoPopulated = false
            };

            schema.IndexedFields[Content.ID_FIELD_NAME] = new IndexedField { Name = Content.ID_FIELD_NAME, DataType = IndexedFieldDataType.String };
            schema.IndexedFields[Content.CREATED_TIMESTAMP_FIELD_NAME] = new IndexedField { Name = Content.CREATED_TIMESTAMP_FIELD_NAME, DataType = IndexedFieldDataType.DateTime };
            schema.IndexedFields[Content.MODIFIED_TIMESTAMP_FIELD_NAME] = new IndexedField { Name = Content.MODIFIED_TIMESTAMP_FIELD_NAME, DataType = IndexedFieldDataType.DateTime };

            return schema;      
        }               
    }   

    public class IndexedField
    {
        public string Name { get; set; }
        public IndexedFieldDataType DataType { get; set; }
        public IndexedFieldDataType ArrayElementDataType { get; set; }
        public IndexSchema ObjectSchema { get; set; }
    }

    public enum IndexedFieldDataType
    {
        String,
        Text,
        Number,
        DateTime,
        Array,
        Object
    }   
    
}