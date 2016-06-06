using ExpandoDB.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using ExpandoDB.Storage;

namespace ExpandoDB
{
    // TODO: Merge DocumentCollectionSchema and IndexSchema
    [Serializable]
    public class DocumentCollectionSchema : IEquatable<DocumentCollectionSchema>
    {
        public string Name { get; set; }
        public List<DocumentCollectionSchemaField> Fields { get; set; }        

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentCollectionSchema"/> class.
        /// </summary>
        public DocumentCollectionSchema()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentCollectionSchema"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public DocumentCollectionSchema(string name)
        {
            Name = name;
            Fields = new List<DocumentCollectionSchemaField>();
        }

        /// <summary>
        /// Determines whether the specified <see cref="DocumentCollectionSchema" />, is equal to this instance.
        /// </summary>
        /// <param name="other">The DocumentCollectionSchema to compare against this instance.</param>
        /// <returns></returns>
        public bool Equals(DocumentCollectionSchema other)
        {
            if (other == null)
                return false;

            if (Name != other.Name)
                return false;

            var thisHash = this.ComputeMd5Hash();
            var otherHash = other.ComputeMd5Hash();
            
            return string.Compare(thisHash, otherHash, StringComparison.Ordinal) == 0;
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

            var schema = obj as DocumentCollectionSchema;
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
        public static bool operator == (DocumentCollectionSchema schema1, DocumentCollectionSchema schema2)
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
        public static bool operator !=(DocumentCollectionSchema schema1, DocumentCollectionSchema schema2)
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
        public override int GetHashCode()
        {
            return this.ComputeMd5Hash().GetHashCode();
        }        

    }

    [Serializable]
    public class DocumentCollectionSchemaField
    {
        public string Name { get; set; }
        public Schema.DataType DataType { get; set; }
        public Schema.DataType ArrayElementDataType { get; set; }
        public DocumentCollectionSchema ObjectSchema { get; set; }        
    }

    public static class DocumentCollectionSchemaUtil
    {
        public static DocumentCollectionSchema ToDocumentCollectionSchema(this Schema fromSchema)
        {
            if (fromSchema == null)
                return null;

            var toSchema = new DocumentCollectionSchema(fromSchema.Name);
            foreach (var fieldName in fromSchema.Fields.Keys)
            {
                var field = fromSchema.Fields[fieldName];
                var toSchemaField = new DocumentCollectionSchemaField
                {
                    Name = field.Name,
                    DataType = field.DataType,
                    ArrayElementDataType = field.ArrayElementDataType,
                    ObjectSchema = field.ObjectSchema.ToDocumentCollectionSchema()
                };

                toSchema.Fields.Add(toSchemaField);
            }

            return toSchema;

        }

        /// <summary>
        /// Gets the IndexSchema based on the IndexedFields of this instance.
        /// </summary>
        /// <returns></returns>
        public static Schema ToIndexSchema(this DocumentCollectionSchema fromSchema)
        {
            if (fromSchema == null)
                return null;

            var toSchema = new Schema(fromSchema.Name);
            foreach (var fromField in fromSchema.Fields)
            {
                var toField = new Schema.Field
                {
                    Name = fromField.Name,
                    DataType = fromField.DataType,
                    ArrayElementDataType = fromField.ArrayElementDataType,
                    ObjectSchema = fromField.ObjectSchema.ToIndexSchema()
                };

                toSchema.Fields.TryAdd(toField.Name, toField);
            }

            return toSchema;
        }

        public static string ComputeMd5Hash(this DocumentCollectionSchema schema)
        {
            if (schema == null)
                return String.Empty;

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var data = schema.ToCompressedByteArray();
                byte[] hash = md5.ComputeHash(data);

                var buffer = new System.Text.StringBuilder();
                for (int i = 0; i < data.Length; i++)
                    buffer.Append(data[i].ToString("x2"));

                // Return the hexadecimal string.
                return buffer.ToString();
            }
        }

    }
}
