using System;
using System.Collections.Concurrent;

namespace ExpandoDB
{
    public class ContentSchema
    {
        public string Name { get; set; }
        public ConcurrentDictionary<string, FieldDefinition> Fields { get; set; }  

        public static ContentSchema CreateDefault(string name = null)
        {
            var schema = new ContentSchema
            {
                Name = name ?? "Default",
                Fields = new ConcurrentDictionary<string, FieldDefinition>()                
            };

            schema.Fields[Content.ID_FIELD_NAME] = new FieldDefinition { Name = Content.ID_FIELD_NAME, DataType = FieldDataType.String };
            schema.Fields[Content.CREATED_TIMESTAMP_FIELD_NAME] = new FieldDefinition { Name = Content.CREATED_TIMESTAMP_FIELD_NAME, DataType = FieldDataType.DateTime };
            schema.Fields[Content.MODIFIED_TIMESTAMP_FIELD_NAME] = new FieldDefinition { Name = Content.MODIFIED_TIMESTAMP_FIELD_NAME, DataType = FieldDataType.DateTime };

            return schema;      
        }               
    }   

    public class FieldDefinition
    {
        public string Name { get; set; }
        public FieldDataType DataType { get; set; }
        public FieldDataType ArrayElementDataType { get; set; }
        public ContentSchema ObjectSchema { get; set; }
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