using ExpandoDB.Search;
using ExpandoDB.Storage;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Collections;

namespace ExpandoDB
{
    /// <summary>
    /// Represents a dynamic Content object.
    /// </summary>
    public class Content : DynamicObject
    {
        public const string ID_FIELD_NAME = "_id";
        public const string CREATED_TIMESTAMP_FIELD_NAME = "_createdTimestamp";
        public const string MODIFIED_TIMESTAMP_FIELD_NAME = "_modifiedTimestamp";
        public const string ERROR_MESSAGE_FIELD_NAME = "_errorMessage";
        public const string ERROR_JSON_FIELD_NAME = "_errorJson";

        private readonly dynamic _expando;
        private readonly IDictionary<string, object> _expandoDictionary;        


        /// <summary>
        /// Initializes a new instance of the <see cref="Content"/> class.
        /// </summary>
        public Content()
            : this(new ExpandoObject())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Content"/> class from a JSON string.
        /// </summary>
        /// <param name="json">The JSON string to be used to populate the Content object.</param>
        public Content(string json)
            : this (json.ToExpando())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Content"/> class from a Dictionary object.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        public Content(IDictionary<string, object> dictionary)
            : this (dictionary.ToExpando())
        {
        }        

        /// <summary>
        /// Initializes a new instance of the <see cref="Content"/> class from an ExpandoObject instance.
        /// </summary>
        /// <param name="expando">The ExpandoObject to be used to populate the Content object.</param>
        public Content(ExpandoObject expando)
        {
            if (expando == null)
                throw new ArgumentNullException("expando");

            _expando = expando;            
            _expandoDictionary = (IDictionary<string, object>)_expando;

            EnsureIdIsValid();
            EnsureTimestampIsValid(CREATED_TIMESTAMP_FIELD_NAME);
            EnsureTimestampIsValid(MODIFIED_TIMESTAMP_FIELD_NAME);
        }

        /// <summary>
        /// Ensures the _id field is valid.
        /// </summary>
        /// <remarks>
        /// If _id is not set, a new Guid will be assigned
        /// </remarks>        
        private void EnsureIdIsValid()
        {
            if (!_expandoDictionary.ContainsKey(ID_FIELD_NAME))
            {
                _expandoDictionary[ID_FIELD_NAME] = Guid.NewGuid();
                return;
            }

            var idValue = _expandoDictionary[ID_FIELD_NAME];
            if (idValue == null)
            {
                _expandoDictionary[ID_FIELD_NAME] = Guid.NewGuid();
                return;
            }

            var idType = idValue.GetType();
            if (idType == typeof(Guid))
            {
                if ((Guid)idValue == Guid.Empty &&
                    !_expandoDictionary.ContainsKey(ERROR_MESSAGE_FIELD_NAME) &&
                    !_expandoDictionary.ContainsKey(ERROR_JSON_FIELD_NAME))
                {
                    _expandoDictionary[ID_FIELD_NAME] = Guid.NewGuid();
                }
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
        /// Gets the Content's _id property.
        /// </summary>
        /// <value>
        /// The value of the Content's _id property.
        /// </value>        
        public Guid? _id
        {
            get
            {
                if (_expandoDictionary.ContainsKey(ID_FIELD_NAME))
                    return (Guid?)_expandoDictionary[ID_FIELD_NAME];
                return null;                
            }
            set
            {
                if (value == null || value == Guid.Empty)
                    throw new ArgumentException("value cannot be null or empty");

                _expandoDictionary[ID_FIELD_NAME] = value;
            }
        }

        /// <summary>
        /// Gets the Content's _createdTimestamp property.
        /// </summary>
        /// <value>
        /// The value of the Content's _createdTimestamp property.
        /// </value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public DateTime? _createdTimestamp
        {
            get
            {
                if (_expandoDictionary.ContainsKey(CREATED_TIMESTAMP_FIELD_NAME))
                    return (DateTime?)_expandoDictionary[CREATED_TIMESTAMP_FIELD_NAME];

                return null;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _expandoDictionary[CREATED_TIMESTAMP_FIELD_NAME] = value;
            }
        }

        /// <summary>
        /// Gets the Content's _modifiedTimestamp property.
        /// </summary>
        /// <value>
        /// The value of the Content's _modifiedTimestamp property.
        /// </value>        
        public DateTime? _modifiedTimestamp
        {
            get
            {
                if (_expandoDictionary.ContainsKey(MODIFIED_TIMESTAMP_FIELD_NAME))
                    return (DateTime?)_expandoDictionary[MODIFIED_TIMESTAMP_FIELD_NAME];

                return null;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _expandoDictionary[MODIFIED_TIMESTAMP_FIELD_NAME] = value;
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
        /// Gets or sets the value of the specified Content field.
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
                case ID_FIELD_NAME:
                    isAllowed = IsGuid(value);
                    break;

                case CREATED_TIMESTAMP_FIELD_NAME:                    
                case MODIFIED_TIMESTAMP_FIELD_NAME:
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
        /// Determines whether the specified value is allowed as a Content field
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <remarks>
        /// The Content object only allows String, Guid, DateTime, numeric types,
        /// IList, and IDictionary. </remarks>
        private bool IsAllowedValue(object value)
        {
            if (value == null)
                return true;

            var objectType = value.GetType();
            var objectTypeCode = Type.GetTypeCode(objectType);

            switch (objectTypeCode)
            {
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.String:
                case TypeCode.DateTime:
                    return true;

                case TypeCode.Object:
                    if (objectType == typeof(Guid))
                        return true;
                    if (value is IList || value is IDictionary<string, object>)
                        return true;
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
        /// Returns the Content as an ExpandoObject.
        /// </summary>
        /// <returns></returns>
        internal ExpandoObject AsExpando()
        {
            return _expando;
        }

        /// <summary>
        /// Returns the Content as a Dictionary<string, object>.
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
    }
}
