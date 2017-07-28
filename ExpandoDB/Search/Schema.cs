using ExpandoDB.Search;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using ExpandoDB.Storage;
using System.Runtime.Serialization;

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
        /// Gets the or create field.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        public Field GetOrCreateField(string fieldName)
        {
            Field field = null;
            Fields.TryGetValue(fieldName, out field);

            if (field == null)
            {
                field = new Field
                {
                    Name = fieldName
                };
                Fields.TryAdd(fieldName, field);
            }

            if (Config.IsAutoFacetEnabled)            
                field.RefreshAutoFacets();            

            return field;
        }

        /// <summary>
        /// Searches (recursively) within the Schema object, to find the Schema Field with the specified name.
        /// </summary>
        /// <param name="fieldName">Name of the Schema.Field.</param>
        /// <param name="recursive">if set to <c>true</c>, the FindField method will search child objects.</param>
        /// <returns></returns>
        public Field FindField(string fieldName, bool recursive = true)
        {
            if (Fields.ContainsKey(fieldName))
                return Fields[fieldName];

            Field foundField = null;
            if (recursive)
            {
                foreach (var field in Fields.Values)
                {
                    if (field.DataType == DataType.Array || field.DataType == DataType.Object)
                    {
                        var childSchema = field.ObjectSchema;
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
            defaultSchema.Fields[MetadataField.FULL_TEXT] = new Field { Name = MetadataField.FULL_TEXT, DataType = DataType.Text, IsTokenized = true };

            return defaultSchema;
        }

        /// <summary>
        /// Determines whether this instance is a default schema
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is default; otherwise, <c>false</c>.
        /// </value>
        internal bool IsDefault
        {
            get
            {
                if (Name == "Default")
                    return true;

                return (Fields.Count == 4 &&
                        Fields.ContainsKey(MetadataField.ID) &&
                        Fields.ContainsKey(MetadataField.CREATED_TIMESTAMP) &&
                        Fields.ContainsKey(MetadataField.MODIFIED_TIMESTAMP) &&
                        Fields.ContainsKey(MetadataField.FULL_TEXT));
            }
        }


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
            public bool IsTopLevel
            {
                get
                {
                    var countOfDots = Name?.Count(c => c == '.') ?? 0;

                    return ((countOfDots == 0 && ParentField == null) ||                            
                            (countOfDots == 1 && ParentField?.DataType == DataType.Object && DataType != DataType.Array  && DataType != DataType.Object));
                }
            }

            /// <summary>
            /// Gets a value indicating whether this Field is sortable.
            /// </summary>
            /// <value>
            /// <c>true</c> if this Field is sortable; otherwise, <c>false</c>.
            /// </value>
            public bool IsSortable { get { return IsTopLevel && DataType != DataType.Array && DataType != DataType.Object; } }

            /// <summary>
            /// Gets or sets the parent field.
            /// </summary>
            /// <value>
            /// The parent field.
            /// </value>
            internal Field ParentField { get; set; }
                        

            /// <summary>
            /// Gets or sets a value indicating whether a Text field is tokenized and analyzed by Lucene. 
            /// </summary>
            /// <value>
            /// <c>true</c> if this Field is tokenized and analyzed; otherwise, <c>false</c>.
            /// </value>
            public bool IsTokenized { get; set; }

            /// <summary>
            /// Gets a value indicating whether this Field is a Facet.
            /// </summary>
            /// <value>
            ///   <c>true</c> if this Field is a Facet; otherwise, <c>false</c>.
            /// </value>
            internal bool IsFacet { get { return FacetSettings?.IsEnabled ?? false; } }           


            /// <summary>
            /// Gets or sets the Facet settings for this Field.
            /// </summary>
            /// <value>
            /// The Facet settings for this Field.
            /// </value>
            public FacetSettings FacetSettings { get; set; }

            /// <summary>
            /// Refreshes the auto-created facets.
            /// </summary>            
            internal void RefreshAutoFacets()
            {
                // Auto Facet creation is only valid for top-level Fields
                if (!IsTopLevel)
                    return;

                // Auto Facet creation is only valid for Fields whose Name starts with a letter.
                var firstCharOfName = Name?.FirstOrDefault() ?? ' ';
                if (Char.IsLetter(firstCharOfName) == false)
                    return;                
                
                if (DataType == DataType.Null ||                    
                    (DataType == DataType.Array && 
                     (ArrayElementDataType == DataType.Array || 
                      ArrayElementDataType == DataType.Object || 
                      ArrayElementDataType == DataType.Null)))
                    return;

                if (DataType == DataType.Object && ObjectSchema?.Fields?.Count > 0)
                {
                    foreach (var childField in ObjectSchema.Fields.Values)
                        childField.RefreshAutoFacets();
                }
                else
                {
                    // Facets are valid only for scalar values, or array of scalar values.
                    if (FacetSettings == null)
                        FacetSettings = new FacetSettings { FacetName = Name, IsEnabled = true };

                    if (DataType == DataType.DateTime)
                    {
                        if (FacetSettings.FormatString == null)
                            FacetSettings.FormatString = @"yyyy/MMM/dd";

                        FacetSettings.IsHierarchical = true;

                        if (FacetSettings.HierarchySeparator == null)
                            FacetSettings.HierarchySeparator = @"/";
                    }
                }   
            }
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
        /// Defines the configuration for a Facet, which is used to categorize <see cref="Document"/> objects in buckets.
        /// </summary>
        public class FacetSettings
        {            
            /// <summary>
            /// Gets or sets the name of this Facet.
            /// </summary>
            /// <value>
            /// The name of the Facet.
            /// </value>
            public string FacetName { get; set; }


            /// <summary>
            /// Gets or sets a value indicating whether Facets is enabled.
            /// </summary>
            /// <value>
            ///   <c>true</c> if Facets is enabled; otherwise, <c>false</c>.
            /// </value>
            public bool IsEnabled { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the Facet is hierarchical, e.g. "Topic:Education/Primary/Teacher Training"
            /// </summary>
            /// <value>
            /// <c>true</c> if this Facet is hierarchical; otherwise, <c>false</c>.
            /// </value>
            public bool IsHierarchical { get; set; }

            /// <summary>
            /// Gets or sets the separator char for hierarchical Facets; e.g. the "/" character in "Topic:Education/Primary/Teacher Training"
            /// </summary>
            /// <value>
            /// The separator char.
            /// </value>
            public string HierarchySeparator { get; set; } 

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
