using ExpandoDB.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using ExpandoDB.Storage;
using System.Text;

namespace ExpandoDB
{
    /// <summary>
    /// Represents a dynamic JSON Document.
    /// </summary>
    [Serializable]
    public class Document : DynamicObject, IEquatable<Document>
    {
        private readonly dynamic _expando;
        private readonly IDictionary<string, object> _expandoDictionary;  

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class.
        /// </summary>
        public Document()
            : this(new ExpandoObject())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to be used to populate the Document object.</param>
        public Document(string json)
            : this (json.ToDictionary().ToExpando())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class from a Dictionary object.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        public Document(IDictionary<string, object> dictionary)
            : this (dictionary.ToExpando())
        {
        }        

        /// <summary>
        /// Initializes a new instance of the <see cref="Document"/> class from an ExpandoObject instance.
        /// </summary>
        /// <param name="expando">The ExpandoObject to be used to populate the Document object.</param>
        public Document(ExpandoObject expando)
        {
            if (expando == null)
                throw new ArgumentNullException(nameof(expando));

            _expando = expando;            
            _expandoDictionary = (IDictionary<string, object>)_expando;

            EnsureIdIsValid();
            EnsureTimestampIsValid(Schema.StandardField.CREATED_TIMESTAMP);
            EnsureTimestampIsValid(Schema.StandardField.MODIFIED_TIMESTAMP);
        }

        /// <summary>
        /// Ensures the _id field is valid.
        /// </summary>
        /// <remarks>
        /// If _id is not set, a new Guid will be assigned
        /// </remarks>        
        private void EnsureIdIsValid()
        {
            if (!_expandoDictionary.ContainsKey(Schema.StandardField.ID))
            {
                _expandoDictionary[Schema.StandardField.ID] = Guid.NewGuid();
                return;
            }

            var idValue = _expandoDictionary[Schema.StandardField.ID];
            if (idValue == null)
            {
                _expandoDictionary[Schema.StandardField.ID] = Guid.NewGuid();
                return;
            }

            var idType = idValue.GetType();
            if (idType == typeof(Guid))
            {
                if ((Guid)idValue == Guid.Empty)                
                    _expandoDictionary[Schema.StandardField.ID] = Guid.NewGuid();
                
                return;
            }

            throw new Exception("The _id field contains a value that is not a GUID.");

        }

        /// <summary>
        /// Ensures the timestamp field has a valid value.
        /// </summary>
        /// <param name="timestampFieldName">Name of the timestamp field.</param>        
        private void EnsureTimestampIsValid(string timestampFieldName)
        {
            if (!_expandoDictionary.ContainsKey(timestampFieldName))
                return;

            var timestamp = _expandoDictionary[timestampFieldName];
            if (timestamp == null)
                return;

            if (IsDateTime(timestamp))
                return;

            throw new Exception(String.Format("The {0} field contains a value that is not a DateTime.", timestampFieldName));
        }       

        /// <summary>
        /// Gets the Document's unique _id.
        /// </summary>
        /// <value>
        /// The value of the Document's _id property.
        /// </value>        
        public Guid? _id
        {
            get
            {
                if (_expandoDictionary.ContainsKey(Schema.StandardField.ID))
                    return (Guid?)_expandoDictionary[Schema.StandardField.ID];
                return null;                
            }
            set
            {
                if (value == null || value == Guid.Empty)
                    throw new ArgumentException("_id cannot be null or empty");

                _expandoDictionary[Schema.StandardField.ID] = value;
            }
        }

        /// <summary>
        /// Gets the Document's _createdTimestamp property.
        /// </summary>
        /// <value>
        /// The value of the Document's _createdTimestamp property.
        /// </value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public DateTime? _createdTimestamp
        {
            get
            {
                if (_expandoDictionary.ContainsKey(Schema.StandardField.CREATED_TIMESTAMP))
                    return (DateTime?)_expandoDictionary[Schema.StandardField.CREATED_TIMESTAMP];

                return null;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _expandoDictionary[Schema.StandardField.CREATED_TIMESTAMP] = value;
            }
        }

        /// <summary>
        /// Gets the Document's _modifiedTimestamp property.
        /// </summary>
        /// <value>
        /// The value of the Document's _modifiedTimestamp property.
        /// </value>        
        public DateTime? _modifiedTimestamp
        {
            get
            {
                if (_expandoDictionary.ContainsKey(Schema.StandardField.MODIFIED_TIMESTAMP))
                    return (DateTime?)_expandoDictionary[Schema.StandardField.MODIFIED_TIMESTAMP];

                return null;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                _expandoDictionary[Schema.StandardField.MODIFIED_TIMESTAMP] = value;
            }
        }
        

        /// <summary>
        /// Tries to get the value of a dynamic member.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var isFound = _expandoDictionary.ContainsKey(binder.Name);
            if (isFound)           
                result = _expandoDictionary[binder.Name];    
            else
                result = null;

            return isFound;                       
        }

        /// <summary>
        /// Tries to set the value of a dynamic member.
        /// </summary>
        /// <param name="binder">The binder.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            EnsureValueIsAllowedForMember(binder.Name, value);
            _expandoDictionary[binder.Name] = value;
            return true; 
        }

        /// <summary>
        /// Gets or sets the value of the specified Document field.
        /// </summary>
        /// <value>
        /// The <see cref="System.Object"/>.
        /// </value>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        public object this [string fieldName]
        {
            get 
            { 
                if (!_expandoDictionary.ContainsKey(fieldName))
                    return null;

                return _expandoDictionary[fieldName];
            }
            set
            {
                EnsureValueIsAllowedForMember(fieldName, value);
                _expandoDictionary[fieldName] = value;
            }
        }

        private void EnsureValueIsAllowedForMember(string memberName, object value)
        {
            if (value == null)
                return;

            var isAllowed = false;
            switch (memberName)
            {
                case Schema.StandardField.ID:
                    isAllowed = IsGuid(value);
                    break;

                case Schema.StandardField.CREATED_TIMESTAMP:                    
                case Schema.StandardField.MODIFIED_TIMESTAMP:
                    isAllowed = IsDateTime(value);
                    break;

                default:
                    isAllowed = IsAllowedValue(value);
                    break;
            }

            if (!isAllowed)
                throw new InvalidOperationException(String.Format("The value '{0}' is not allowed for field '{1}'", value, memberName));
        }

        private bool IsDateTime(object value)
        {
            if (value == null)
                return false;

            var objectType = value.GetType();
            return (objectType == typeof(DateTime) || objectType == typeof(DateTime?));
        }

        private bool IsGuid(object value)
        {
            if (value == null)
                return false;

            var objectType = value.GetType();
            return (objectType == typeof(Guid) || objectType == typeof(Guid?));
        }

        /// <summary>
        /// Determines whether the specified value is allowed as a Document field
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// The Document object only allows String, Guid, DateTime, numeric types,
        /// IList, and IDictionary. </remarks>
        private bool IsAllowedValue(object value)
        {
            if (value == null)
                return true;

            var objectType = value.GetType();
            return IsAllowedType(objectType);
        }

        private bool IsAllowedType(Type type)
        {
            var typeCode = Type.GetTypeCode(type);

            switch (typeCode)
            {
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.String:
                case TypeCode.DateTime:
                case TypeCode.Boolean:
                    return true;

                case TypeCode.Object:
                    if (type == typeof(Guid))
                        return true;                                        
                    if (typeof(IDictionary<string, object>).IsAssignableFrom(type))
                        return true;
                    if (typeof(IList).IsAssignableFrom(type))
                    {
                        if (type.IsArray)
                            return IsAllowedType(type.GetElementType());
                        if (type.IsGenericType)
                            return IsAllowedType(type.GetGenericArguments()[0]);                        
                    }
                    break;
            }

            return false;

        }

        /// <summary>
        /// Gets the dynamic member names.
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return _expandoDictionary.Keys;
        }

        /// <summary>
        /// Returns the Document as an ExpandoObject.
        /// </summary>
        /// <returns></returns>
        internal ExpandoObject AsExpando()
        {
            return _expando;
        }

        /// <summary>
        /// Returns the Document as a Dictionary.
        /// </summary>
        /// <returns></returns>
        internal IDictionary<string, object> AsDictionary()
        {
            return _expandoDictionary;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _id.ToString();
        }


        /// <summary>
        /// Indicates whether the current Document is equal to another Document.
        /// </summary>
        /// <param name="other">A Document object to compare with this Document.</param>
        /// <returns>
        /// true if the current Document is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(Document other)
        {
            if (other == null)
                return false;

            if (_id != other._id)
                return false;

            var thisJson = DynamicJsonSerializer.Serialize(this);
            var otherJson = DynamicJsonSerializer.Serialize(other);

            if (thisJson.Length != otherJson.Length)
                return false;

            var thisHash = ComputeMd5Hash(thisJson);
            var otherHash = other.ComputeMd5Hash(otherJson);

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

            var document = obj as Document;
            if (document == null)
                return false;
            else
                return Equals(document);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="document1">The document1.</param>
        /// <param name="document2">The document2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(Document document1, Document document2)
        {
            if (((object)document1) == null || ((object)document2) == null)
                return Object.Equals(document1, document2);

            return document1.Equals(document2);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="document1">The document1.</param>
        /// <param name="document2">The document2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(Document document1, Document document2)
        {
            if (((object)document1) == null || ((object)document2) == null)
                return !Object.Equals(document1, document2);

            return !document1.Equals(document2);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return ComputeMd5Hash().GetHashCode();
        }

        /// <summary>
        /// Computes the MD5 hash of the JSON representation of this Document
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        internal string ComputeMd5Hash(string json = null)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                if (String.IsNullOrWhiteSpace(json))
                    json = DynamicJsonSerializer.Serialize(this);

                byte[] data = Encoding.UTF8.GetBytes(json);
                byte[] hash = md5.ComputeHash(data);

                var buffer = new System.Text.StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                    buffer.Append(hash[i].ToString("x2"));

                // Return the hexadecimal string.
                return buffer.ToString();
            }
        }

    }
}
