using System;
using System.Collections.Concurrent;

namespace ExpandoDB.Search
{
    public class SearchSchema
    {
        public string Name { get; set; }
        public ConcurrentDictionary<string, IndexedField> IndexedFields { get; set; }
        public static SearchSchema Default { get; private set; }

        static SearchSchema()
        {
            Default = new SearchSchema
            {
                Name = String.Empty,
                IndexedFields = new ConcurrentDictionary<string, IndexedField>()
            };
            
            Default.IndexedFields[Content.ID_FIELD_NAME] = new IndexedField { Name = Content.ID_FIELD_NAME, DataType = IndexedFieldDataType.String, IsSortable = true, IsTokenized = false, IsStored = true };  
            Default.IndexedFields[Content.CREATED_TIMESTAMP_FIELD_NAME] = new IndexedField { Name = Content.CREATED_TIMESTAMP_FIELD_NAME, DataType = IndexedFieldDataType.DateTime, IsSortable = true, IsTokenized = false };
            Default.IndexedFields[Content.MODIFIED_TIMESTAMP_FIELD_NAME] = new IndexedField { Name = Content.MODIFIED_TIMESTAMP_FIELD_NAME, DataType = IndexedFieldDataType.DateTime, IsSortable = true, IsTokenized = false };            
        }       
    }   

    public class IndexedField
    {
        public string Name { get; set; }
        public IndexedFieldDataType DataType { get; set; }
        public IndexedFieldDataType ArrayElementDataType { get; set; }
        public SearchSchema ObjectSchema { get; set; }
        public bool IsStored { get; set; }
        public bool IsTokenized { get; set; }
        public bool IsSortable { get; set; }                
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