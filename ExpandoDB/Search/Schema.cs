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
    /// Defines the Lucene index schema of a <see cref="Document"/> <see cref="Collection"/>.
    /// </summary>    
    [Serializable]
    public class Schema
    {
        internal const string COLLECTION_NAME = "_schemas";

        /// <summary>
        /// Gets or sets the unique ID of the <see cref="Schema"/>.
        /// </summary>
        /// <value>
        /// A GUID value.
        /// </value>
        public Guid? _id { get; set; }

        /// <summary>
        /// Gets or sets the name of the <see cref="Schema"/>.
        /// </summary>
        /// <value>
        /// The name of the <see cref="Schema"/>.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the collection of <see cref="Field"/>s that defines the <see cref="Schema"/>.
        /// </summary>
        /// <value>
        /// The fields.
        /// </value>
        public ConcurrentDictionary<string, Field> Fields { get; set; } = new ConcurrentDictionary<string, Field>();

        /// <summary>
        /// Gets or sets the creation date/time of the <see cref="Schema"/> object.
        /// </summary>
        /// <value>
        /// The creation date/time of the <see cref="Schema"/> object.
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
        /// Creates a default <see cref="Schema"/>, which contains the _id, _createdTimestamp, _modifiedTimestamp, and _full_text_ fields.
        /// </summary>
        /// <param name="name">The name of the <see cref="Schema"/>.</param>
        /// <returns></returns>
        public static Schema CreateDefault(string name = null)
        {
            if (String.IsNullOrWhiteSpace(name))
                name = "Default";

            var defaultSchema = new Schema() { Name = name };

            defaultSchema.Fields[MetadataField.ID] = new Field { Name = MetadataField.ID, DataType = DataType.Guid };
            defaultSchema.Fields[MetadataField.CREATED_TIMESTAMP] = new Field { Name = MetadataField.CREATED_TIMESTAMP, DataType = DataType.DateTime };
            defaultSchema.Fields[MetadataField.MODIFIED_TIMESTAMP] = new Field { Name = MetadataField.MODIFIED_TIMESTAMP, DataType = DataType.DateTime };
            defaultSchema.Fields[MetadataField.FULL_TEXT] = new Field { Name = MetadataField.FULL_TEXT, DataType = DataType.Text };

            return defaultSchema;
        }

        /// <summary>
        /// Gets or sets the <see cref="Facet"/> definitions.
        /// </summary>
        /// <value>
        /// The facets.
        /// </value>
        public IList<Facet> Facets { get; set; } = new List<Facet>();

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
        /// Defines a field of a <see cref="Document"/>, as stored in the Lucene index.
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
            /// Gets or sets the <see cref="Schema"/> of Fields with DataType=Object.
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
            /// Gets or sets a value indicating whether a Text field is tokenized and analyzed by Lucene. 
            /// </summary>
            /// <value>
            /// <c>true</c> if this Field is tokenized and analyzed; otherwise, <c>false</c>.
            /// </value>
            public bool IsAnalyzed { get; set; } = true;
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
        /// Defines a category which will be used to group <see cref="Document"/> objects.
        /// </summary>
        public class Facet
        {
            /// <summary>
            /// Gets or sets name of the field that will be the source of values for this Facet.
            /// </summary>
            /// <value>
            /// The field name.
            /// </value>
            public string FieldName { get; set; }

            /// <summary>
            /// Gets or sets the display name of this Facet, if different from the FieldName.
            /// </summary>
            /// <value>
            /// The display name of the Facet.
            /// </value>
            public string DisplayName { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the Facet is hierarchical, e.g. "Topic:Education/Primary/Teacher Training"
            /// </summary>
            /// <value>
            /// <c>true</c> if this Facet is hierarchical; otherwise, <c>false</c>.
            /// </value>
            public bool IsHierarchical { get; set; }

            /// <summary>
            /// Gets or sets the separator string for hierarchical Facets; e.g. the "/" character in "Topic:Education/Primary/Teacher Training"
            /// </summary>
            /// <value>
            /// The separator string.
            /// </value>
            public string HierarchySeparator { get; set; } = "/";

            /// <summary>
            /// Gets or sets the format string to be applied when converting non-string Facet fields to string.
            /// </summary>
            /// <remarks>
            /// This primarily applies to DateTime fields, e.g. "yyyy/MMM/dd"
            /// </remarks>
            /// <value>
            /// The format.
            /// </value>
            public string FormatString { get; set; }
        }

    }


}
