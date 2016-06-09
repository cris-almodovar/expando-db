using ExpandoDB.Search;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using ExpandoDB.Storage;

namespace ExpandoDB
{
    /// <summary>
    /// Represents the Document fields that are automatically indexed, and are thus searchable.
    /// </summary>    
    [Serializable]
    public class Schema
    {
        public const string COLLECTION_NAME = "_schemas";
        public Guid _id { get; set; } = Guid.NewGuid();        
        public string Name { get; set; }
        public ConcurrentDictionary<string, Field> Fields { get; set; } = new ConcurrentDictionary<string, Field>();
        public DateTime? _createdTimestamp { get; set; }
        public DateTime? _modifiedTimestamp { get; set; }

        /// <summary>
        /// Creates a default Schema, which contains the _id, _createdTimestamp, _modifiedTimestamp, and _full_text_ fields.
        /// </summary>
        /// <param name="name">The name of the IndexSchema.</param>
        /// <returns></returns>
        public static Schema CreateDefault(string name = null)
        {            
            if (String.IsNullOrWhiteSpace(name))
                name = "Default";

            var defaultSchema = new Schema();
            defaultSchema.Name = name;      
            defaultSchema.Fields[StandardField.ID] = new Field { Name = StandardField.ID, DataType = DataType.Guid };
            defaultSchema.Fields[StandardField.CREATED_TIMESTAMP] = new Field { Name = StandardField.CREATED_TIMESTAMP, DataType = DataType.DateTime };
            defaultSchema.Fields[StandardField.CREATED_TIMESTAMP] = new Field { Name = StandardField.CREATED_TIMESTAMP, DataType = DataType.DateTime };
            defaultSchema.Fields[StandardField.FULL_TEXT] = new Field { Name = StandardField.FULL_TEXT, DataType = DataType.Text };
            
            return defaultSchema;      
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
