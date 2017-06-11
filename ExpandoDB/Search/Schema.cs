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
        public FieldsCollection Fields { get; set; } = new FieldsCollection();

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
            {
                if (field.Name != MetadataField.ID && 
                    field.Name != MetadataField.CREATED_TIMESTAMP &&
                    field.Name != MetadataField.MODIFIED_TIMESTAMP &&
                    field.Name != MetadataField.FULL_TEXT)
                {
                    if (field.FacetSettings == null)
                        field.FacetSettings = new FacetSettings { FacetName = field.Name };

                    if (field.DataType == DataType.DateTime)
                    {
                        field.FacetSettings.FormatString = @"yyyy/MMM/dd";
                        field.FacetSettings.IsHierarchical = true;
                        field.FacetSettings.HierarchySeparator = @"/";
                    }
                }
            }

            return field;
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
        /// 
        /// </summary>        
        public class FieldsCollection : ICollection<Field> 
        {
            private readonly ConcurrentDictionary<string, Field> _fieldsDictionary = new ConcurrentDictionary<string, Field>(); 

            /// <summary>
            /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
            /// </summary>
            public int Count { get { return _fieldsDictionary.Count; } }

            /// <summary>
            /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
            /// </summary>
            public bool IsReadOnly { get { return false; } }

            /// <summary>
            /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
            /// </summary>
            /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
            public void Add(Field item)
            {
                TryAdd(item.Name, item);
            }

            /// <summary>
            /// Tries the add.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="item">The item.</param>
            /// <returns></returns>
            public bool TryAdd(string key, Field item)
            {
                return _fieldsDictionary.TryAdd(key, item);
            }

            /// <summary>
            /// Gets or sets the <see cref="Field"/> with the specified key.
            /// </summary>
            /// <value>
            /// The <see cref="Field"/>.
            /// </value>
            /// <param name="key">The key.</param>
            /// <returns></returns>
            public Field this[string key]
            {
                get { return _fieldsDictionary[key]; }
                set { _fieldsDictionary[key] = value; }
            }

            /// <summary>
            /// Tries the get value.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            /// <returns></returns>
            public bool TryGetValue(string key, out Field value)
            {
                return _fieldsDictionary.TryGetValue(key, out value);
            }

            /// <summary>
            /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
            /// </summary>
            /// <exception cref="System.NotImplementedException"></exception>
            public void Clear()
            {
                _fieldsDictionary.Clear();
            }

            /// <summary>
            /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
            /// <returns>
            /// true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.
            /// </returns>
            /// <exception cref="System.NotImplementedException"></exception>
            public bool Contains(Field item)
            {
                return ContainsKey(item.Name);
            }

            /// <summary>
            /// Determines whether the specified key contains key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns>
            ///   <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
            /// </returns>
            public bool ContainsKey(string key)
            {
                return _fieldsDictionary.ContainsKey(key);
            }

            /// <summary>
            /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
            /// </summary>
            /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
            public void CopyTo(Field[] array, int arrayIndex)
            {
                _fieldsDictionary.Values.CopyTo(array, arrayIndex);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// An enumerator that can be used to iterate through the collection.
            /// </returns>
            /// <exception cref="System.NotImplementedException"></exception>
            public IEnumerator<Field> GetEnumerator()
            {
                return _fieldsDictionary.Values.GetEnumerator();
            }           

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
            /// </returns>            
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            /// <summary>
            /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
            /// </summary>
            /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
            /// <returns>
            /// true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.
            /// </returns>            
            public bool Remove(Field item)
            {
                Field value = null;
                return TryRemove(item.Name, out value);
            }

            /// <summary>
            /// Removes the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns></returns>
            public bool Remove(string key)
            {
                Field value = null;
                return TryRemove(key, out value);
            }

            /// <summary>
            /// Tries the remove.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="value">The value.</param>
            /// <returns></returns>
            public bool TryRemove(string key, out Field value)
            {
                return _fieldsDictionary.TryRemove(key, out value);
            }

            /// <summary>
            /// Gets or sets the <see cref="Field"/> at the specified index.
            /// </summary>
            /// <value>
            /// The <see cref="Field"/>.
            /// </value>
            /// <param name="index">The index.</param>
            /// <returns></returns>
            /// <exception cref="System.NotImplementedException">
            /// </exception>
            public Field this[int index]
            {
                get
                {
                    var enumerator = _fieldsDictionary.GetEnumerator();
                    for (var i = 0; i < index+1; i++)
                        enumerator.MoveNext();

                    return enumerator.Current.Value;
                }
                set
                {
                    var enumerator = _fieldsDictionary.GetEnumerator();
                    for (var i = 0; i < index + 1; i++)
                        enumerator.MoveNext();

                    _fieldsDictionary[enumerator.Current.Key] = value;
                }
            }

            public int IndexOf(Field item)
            {
                throw new NotImplementedException();
            }

            public void Insert(int index, Field item)
            {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotImplementedException();
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
            internal bool IsArrayElement { get; set; }

            /// <summary>
            /// Gets or sets the parent field.
            /// </summary>
            /// <value>
            /// The parent field.
            /// </value>
            internal Schema.Field ParentField { get; set; }

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
            internal bool IsFacet { get { return FacetSettings != null; } }


            /// <summary>
            /// Gets or sets the Facet settings for this Field.
            /// </summary>
            /// <value>
            /// The Facet settings for this Field.
            /// </value>
            public FacetSettings FacetSettings { get; set; }

            /// <summary>
            /// Refreshes the facet settings.
            /// </summary>
            /// <exception cref="System.NotImplementedException"></exception>
            internal void RefreshFacetSettings()
            {
                if (Name != MetadataField.ID &&
                    Name != MetadataField.CREATED_TIMESTAMP &&
                    Name != MetadataField.MODIFIED_TIMESTAMP &&
                    Name != MetadataField.FULL_TEXT)
                {
                    if (FacetSettings == null)
                        FacetSettings = new FacetSettings { FacetName = Name };

                    if (DataType == DataType.DateTime)
                    {
                        FacetSettings.FormatString = @"yyyy/MMM/dd";
                        FacetSettings.IsHierarchical = true;
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
