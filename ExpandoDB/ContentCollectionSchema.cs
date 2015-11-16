using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB
{
    public class ContentCollectionSchema : IEquatable<ContentCollectionSchema>
    {
        public string Name { get; set; }
        public List<IndexedField> IndexedFields { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentCollectionSchema"/> class.
        /// </summary>
        public ContentCollectionSchema()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentCollectionSchema"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public ContentCollectionSchema(string name)
        {
            Name = name;
            IndexedFields = new List<IndexedField>();
        }

        /// <summary>
        /// Determines whether the specified <see cref="ContentCollectionSchema" />, is equal to this instance.
        /// </summary>
        /// <param name="other">The ContentCollectionSchema to compare against this instance.</param>
        /// <returns></returns>
        public bool Equals(ContentCollectionSchema other)
        {
            if (other == null)
                return false;

            if (Name != other.Name)
                return false;

            var thisFields = IndexedFields.Select(f => f.Name).ToList();
            thisFields.Sort();

            var otherFields = other.IndexedFields.Select(f => f.Name).ToList();
            otherFields.Sort();

            if (thisFields.SequenceEqual(otherFields))
                return true;
            else
                return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var schema = obj as ContentCollectionSchema;
            if (schema == null)
                return false;
            else
                return Equals(schema);
        }

        /// <summary>
        /// Implements the == operator 
        /// </summary>
        /// <param name="schema1">The schema1.</param>
        /// <param name="schema2">The schema2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator == (ContentCollectionSchema schema1, ContentCollectionSchema schema2)
        {            
            if (((object)schema1) == null || ((object)schema2) == null)
                return Object.Equals(schema1, schema2);

            return schema1.Equals(schema2);
        }

        /// <summary>
        /// Implements the != operator.
        /// </summary>
        /// <param name="schema1">The schema1.</param>
        /// <param name="schema2">The schema2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(ContentCollectionSchema schema1, ContentCollectionSchema schema2)
        {
            if (((object)schema1) == null || ((object)schema2) == null)
                return !Object.Equals(schema1, schema2);

            return ! schema1.Equals(schema2);
        }

        ///// <summary>
        ///// Returns a hash code for this instance.
        ///// </summary>
        ///// <returns>
        ///// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        ///// </returns>
        //public override int GetHashCode()
        //{
        //    return Name.GetHashCode() + IndexedFields.GetHashCode();
        //}

        /// <summary>
        /// Gets the IndexSchema based on the IndexedFields of this instance.
        /// </summary>
        /// <returns></returns>
        public IndexSchema GetIndexSchema()
        {
            var indexSchema = new IndexSchema(Name);
            foreach (var field in IndexedFields)            
                indexSchema.Fields.TryAdd(field.Name, field);

            return indexSchema;
        }

    }
}
