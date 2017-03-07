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
    /// Defines the schema of a Document, as it is indexed in Lucene. A schema is associated with each Document Collection.
    /// </summary>    
    [Serializable]
    public class Schema // TODO: Rename to IndexSchema => Documents are supposed to be schema-less.
    {
        internal const string COLLECTION_NAME = "_schemas";
        /// <summary>
        /// Gets or sets the unique ID of the Schema.
        /// </summary>
        /// <value>
        /// A GUID representing the unique ID of the Schema.
        /// </value>
        public Guid? _id { get; set; }

        /// <summary>
        /// Gets or sets the name of the Schema
        /// </summary>
        /// <value>
        /// The name of the Schema.
        /// </value>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the set of Fields.
        /// </summary>
        /// <value>
        /// The fields.
        /// </value>
        public ConcurrentDictionary<string, Field> Fields { get; set; } = new ConcurrentDictionary<string, Field>();
        
        /// <summary>
        /// Gets or sets a timestamp value indicating the date/time the Document was created.
        /// </summary>
        /// <value>
        /// The date/time the Document was created.
        /// </value>
        public DateTime? _createdTimestamp { get; set; }
        
        /// <summary>
        /// Gets or sets a timestamp value indicating the date/time the Document was last modified.
        /// </summary>
        /// <value>
        /// the date/time the Document was last modified.
        /// </value>
        public DateTime? _modifiedTimestamp { get; set; }

        /// <summary>
        /// Creates a default Schema, which contains the _id, _createdTimestamp, _modifiedTimestamp, and _full_text_ fields.
        /// </summary>
        /// <param name="name">The name of the Schema.</param>
        /// <returns></returns>
        public static Schema CreateDefault(string name = null)
        {            
            if (String.IsNullOrWhiteSpace(name))
                name = "Default";

            var defaultSchema = new Schema();            
            defaultSchema.Name = name;      
            defaultSchema.Fields[MetadataField.ID] = new Field { Name = MetadataField.ID, DataType = DataType.Guid };
            defaultSchema.Fields[MetadataField.CREATED_TIMESTAMP] = new Field { Name = MetadataField.CREATED_TIMESTAMP, DataType = DataType.DateTime };
            defaultSchema.Fields[MetadataField.MODIFIED_TIMESTAMP] = new Field { Name = MetadataField.MODIFIED_TIMESTAMP, DataType = DataType.DateTime };
            defaultSchema.Fields[MetadataField.FULL_TEXT] = new Field { Name = MetadataField.FULL_TEXT, DataType = DataType.Text };
            
            return defaultSchema;      
        }

        /// <summary>
        /// Gets or sets the facets.
        /// </summary>
        /// <value>
        /// The facets.
        /// </value>
        public IList<CategoryField> Facets { get; set; } = new List<CategoryField>();

        /// <summary>
        /// Defines the standard metadata fields that all Documents must have.
        /// </summary>
        [Serializable]
        public static class MetadataField
        {
            /// <summary>
            /// The unique ID
            /// </summary>
            public const string ID = "_id";
            /// <summary>
            /// The _createdTimestamp field
            /// </summary>
            public const string CREATED_TIMESTAMP = "_createdTimestamp";
            /// <summary>
            /// The _modifiedTimestamp field
            /// </summary>
            public const string MODIFIED_TIMESTAMP = "_modifiedTimestamp";
            /// <summary>
            /// The _full_text_ field; this field is hidden.
            /// </summary>
            public const string FULL_TEXT = "_full_text_";
            /// <summary>
            /// The _categories field; this field is an IList
            /// </summary>
            public const string CATEGORIES = "_categories";
        }

        /// <summary>
        /// Defines a Document field, as stored in the Lucene index.
        /// </summary>
        [Serializable]
        public class Field
        {
            /// <summary>
            /// Gets or sets the name of the Field
            /// </summary>
            /// <value>
            /// The name.
            /// </value>
            public string Name { get; set; }
            
            /// <summary>
            /// Gets or sets the data type of the Field.
            /// </summary>
            /// <value>
            /// The data type of the Field.
            /// </value>
            public DataType DataType { get; set; }
            
            /// <summary>
            /// Gets or sets the data type of array Fields.
            /// </summary>
            /// <value>
            /// The type of the array element data.
            /// </value>
            public DataType ArrayElementDataType { get; set; }

            /// <summary>
            /// Gets or sets the Schema of object Fields.
            /// </summary>
            /// <value>
            /// The object schema.
            /// </value>
            public Schema ObjectSchema { get; set; }
            
            /// <summary>
            /// Gets a value indicating whether this Field is a top level Field.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is top level; otherwise, <c>false</c>.
            /// </value>
            public bool IsTopLevel { get { return !(Name ?? String.Empty).Contains(".") && !IsArrayElement; } }
            
            /// <summary>
            /// Gets a value indicating whether this Field is sortable.
            /// </summary>
            /// <value>
            /// <c>true</c> if this Field is sortable; otherwise, <c>false</c>.
            /// </value>
            public bool IsSortable { get { return IsTopLevel && DataType != DataType.Array && DataType != DataType.Object; } }
            
            /// <summary>
            /// Gets or sets a value indicating whether this Field is an array element.
            /// </summary>
            /// <value>
            /// <c>true</c> if this Field is an array element; otherwise, <c>false</c>.
            /// </value>
            public bool IsArrayElement { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this Field is tokenized and analyzed by Lucene. 
            /// </summary>
            /// <value>
            /// <c>true</c> if this Field is tokenized and analyzed; otherwise, <c>false</c>.
            /// </value>
            public bool IsAnalyzed { get; set; }  // TODO: Use this field.
        }

        /// <summary>
        /// Specifies the data type of a Document field; this maps to the data types supported by JSON.
        /// </summary>
        [Serializable]
        public enum DataType
        {
            /// <summary>
            /// Null 
            /// </summary>
            Null,
            /// <summary>
            /// A GUID value
            /// </summary>
            Guid,
            /// <summary>
            /// A string value
            /// </summary>
            Text,
            /// <summary>
            /// A numeric value
            /// </summary>
            Number,
            /// <summary>
            /// A boolean value
            /// </summary>
            Boolean,
            /// <summary>
            /// A DateTime value
            /// </summary>
            DateTime,
            /// <summary>
            /// An array value
            /// </summary>
            Array,
            /// <summary>
            /// An object value
            /// </summary>
            Object
        }

        /// <summary>
        /// Defines a Field to be used for automatic categorization.
        /// </summary>
        public class CategoryField
        {
            /// <summary>
            /// Gets or sets the name of the field.
            /// </summary>
            /// <value>
            /// The name of the field.
            /// </value>
            public string FieldName { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this Facet hierarchical.
            /// </summary>
            /// <value>
            /// <c>true</c> if this Facet is hierarchical; otherwise, <c>false</c>.
            /// </value>
            public bool IsHierarchical { get; set; }

            /// <summary>
            /// Gets or sets the delimiter for hierarchical Facets.
            /// </summary>
            /// <value>
            /// The delimiter.
            /// </value>
            public string Delimiter { get; set; }

            /// <summary>
            /// Gets or sets the format string to be applied when converting non-string Facet fields to string.
            /// </summary>
            /// <value>
            /// The format.
            /// </value>
            public string FormatString { get; set; }
        }

    }   
    
    

    
    
}
