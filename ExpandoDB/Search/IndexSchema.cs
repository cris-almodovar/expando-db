using System.Collections.Generic;

namespace ExpandoDB.Search
{
    public class IndexSchema
    {
        public string Name { get; set; }
        public IList<IndexField> IndexFields { get; set; }        

        static public IndexSchema CreateDefault(string schemaName)
        {
            return new IndexSchema
            {
                Name = schemaName,
                IndexFields = new List<IndexField> { 
                    new IndexField { Name = "_id", DataType = IndexFieldDataType.String, IsSortable = true, IsTokenized = false },
                    new IndexField { Name = "_createdTimestamp", DataType = IndexFieldDataType.DateTime, IsSortable = true, IsTokenized = false },
                    new IndexField { Name = "_modifiedTimestamp", DataType = IndexFieldDataType.DateTime, IsSortable = true, IsTokenized = false },
                }
            };
        }
    }   

    public class IndexField
    {
        public string Name { get; set; }
        public IndexFieldDataType DataType { get; set; }
        public IndexFieldDataType ArrayElementDataType { get; set; }
        public IndexSchema ObjectSchema { get; set; }
        public bool IsTokenized { get; set; }
        public bool IsSortable { get; set; }
    }

    public enum IndexFieldDataType
    {
        String,
        Text,
        Number,
        DateTime,
        Array,
        Object
    }    
}