using ExpandoDB.Search;
using ExpandoDB.Storage;
using System;
using System.Collections.Generic;
using System.Dynamic;

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
        /// Initializes a new instance of the <see cref="Content"/> class based on the given ExpandoObject.
        /// </summary>
        /// <param name="expando">The ExpandoObject to be used to populate the Content object.</param>
        public Content(ExpandoObject expando)
        {
            if (expando == null)
                throw new ArgumentNullException("expando");

            _expando = expando;            
            _expandoDictionary = (IDictionary<string, object>)_expando;

            EnsureIdIsValid();
        }

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
                if ((Guid)idValue == Guid.Empty)
                    _expandoDictionary[ID_FIELD_NAME] = Guid.NewGuid();
                return;
            }

            throw new Exception("The _id field contains a value that is not a GUID.");

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
            _expandoDictionary[binder.Name] = value;
            return true;
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
