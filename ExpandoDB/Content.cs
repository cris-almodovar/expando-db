using System;
using System.Collections.Generic;
using System.Dynamic;

namespace ExpandoDB
{
    /// <summary>
    /// Represents a dynamic, schema-less Content object.
    /// </summary>
    public class Content : DynamicObject
    {
        public const string ID_FIELD = "_id";
        public const string CREATED_TIMESTAMP_FIELD = "_createdTimestamp";
        public const string MODIFIED_TIMESTAMP_FIELD = "_modifiedTimestamp";        

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
        /// Initializes a new instance of the <see cref="Content"/> class based on the given ExpandoObject.
        /// </summary>
        /// <param name="expando">The expando.</param>
        public Content(ExpandoObject expando)
        {
            if (expando == null)
                throw new ArgumentNullException("expando");

            _expando = expando;
            _expandoDictionary = (IDictionary<string, object>)_expando;
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
                if (_expandoDictionary.ContainsKey(ID_FIELD))
                    return (Guid?)_expandoDictionary[ID_FIELD];
                return null;                
            }
            set
            {
                if (value == null || value == Guid.Empty)
                    throw new ArgumentException("value cannot be null or empty");

                _expandoDictionary[ID_FIELD] = value;
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
                if (_expandoDictionary.ContainsKey(CREATED_TIMESTAMP_FIELD))
                    return (DateTime?)_expandoDictionary[CREATED_TIMESTAMP_FIELD];

                return null;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _expandoDictionary[CREATED_TIMESTAMP_FIELD] = value;
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
                if (_expandoDictionary.ContainsKey(MODIFIED_TIMESTAMP_FIELD))
                    return (DateTime?)_expandoDictionary[MODIFIED_TIMESTAMP_FIELD];

                return null;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                _expandoDictionary[MODIFIED_TIMESTAMP_FIELD] = value;
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
        public ExpandoObject AsExpando()
        {
            return _expando;
        }

        /// <summary>
        /// Returns the Content as a Dictionary<string, object>.
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, object> AsDictionary()
        {
            return _expandoDictionary;
        }        
    }
}
