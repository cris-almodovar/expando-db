using Jil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ExpandoDB.Serialization
{
    /// <summary>
    /// Provides methods for serializing/deserializing dynamic Content object to/from JSON.
    /// </summary>
    public static class DynamicSerializer
    {

        #region Date Format constants
                
        const string DATE_TIME_FORMAT_DATE_ONLY = "yyyy-MM-dd";

        const string DATE_TIME_FORMAT_DATE_HHMM = "yyyy-MM-ddTHH:mm";
        const string DATE_TIME_FORMAT_DATE_HHMM_UTC = "yyyy-MM-ddTHH:mmZ";
        const string DATE_TIME_FORMAT_DATE_HHMM_TIMEZONE = "yyyy-MM-ddTHH:mmzzz";

        const string DATE_TIME_FORMAT_DATE_HHMMSS = "yyyy-MM-ddTHH:mm:ss";
        const string DATE_TIME_FORMAT_DATE_HHMMSS_UTC = "yyyy-MM-ddTHH:mm:ssZ";
        const string DATE_TIME_FORMAT_DATE_HHMMSS_TIMEZONE = "yyyy-MM-ddTHH:mm:sszzz";

        const string DATE_TIME_FORMAT_DATE_HHMMSSF = "yyyy-MM-ddTHH:mm:ss.f";
        const string DATE_TIME_FORMAT_DATE_HHMMSSF_UTC = "yyyy-MM-ddTHH:mm:ss.fZ";
        const string DATE_TIME_FORMAT_DATE_HHMMSSF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fzzz";

        const string DATE_TIME_FORMAT_DATE_HHMMSSFF = "yyyy-MM-ddTHH:mm:ss.ff";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffZ";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffzzz";

        const string DATE_TIME_FORMAT_DATE_HHMMSSFFF = "yyyy-MM-ddTHH:mm:ss.fff";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffZ";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffzzz";

        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFF = "yyyy-MM-ddTHH:mm:ss.ffff";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffffZ";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffffzzz";

        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFFF = "yyyy-MM-ddTHH:mm:ss.fffff";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffffZ";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffffzzz";

        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFFFF = "yyyy-MM-ddTHH:mm:ss.ffffff";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffffffZ";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffffffzzz";

        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFFFFF = "yyyy-MM-ddTHH:mm:ss.fffffff";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        const string DATE_TIME_FORMAT_DATE_HHMMSSFFFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";

        #endregion

        const int GUID_STRING_LENGTH = 36;

        /// <summary>
        /// Serializes the specified Content object to JSON.
        /// </summary>
        /// <param name="content">The Content object.</param>
        /// <returns></returns>
        public static string Serialize(Content content)
        {
            return JSON.SerializeDynamic(content.AsExpando(), Options.ISO8601IncludeInherited);
        }

        /// <summary>
        /// Serializes the specified Content object to JSON and writes to the provided TextWriter.
        /// </summary>
        /// <param name="content">The Content object.</param>
        /// <param name="writer">The TextWriter object.</param>
        public static void Serialize(Content content, TextWriter writer)
        {
            JSON.SerializeDynamic(content.AsExpando(), writer, Options.ISO8601IncludeInherited);
        }

        /// <summary>
        /// Serializes the specified data object to JSON.
        /// </summary>
        /// <param name="data">The data object.</param>
        /// <returns></returns>
        public static string Serialize(object data)
        {
            return JSON.SerializeDynamic(data, Options.ISO8601IncludeInherited);
        }

        /// <summary>
        /// Serializes the specified data object to JSON and writes to the provided TextWriter.
        /// </summary>
        /// <param name="data">The data object.</param>
        /// <param name="writer">The TextWriter object.</param>
        public static void Serialize(object data, TextWriter writer)
        {
            JSON.SerializeDynamic(data, writer, Options.ISO8601IncludeInherited);
        }


        /// <summary>
        /// Deserializes the specified JSON into an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">The JSON string.</param>
        /// <returns></returns>
        public static T Deserialize<T>(string json)
        {
            if (typeof(T) == typeof(IDictionary<string, object>))
            {
                var dictionary = JSON.Deserialize<T>(json) as IDictionary<string, object>;
                return (T)dictionary.Unwrap();
            }

            return JSON.Deserialize<T>(json);
        }

        /// <summary>
        /// Deserializes JSON from the specified TextReader.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader">The TextReader.</param>
        /// <returns></returns>
        public static T Deserialize<T>(TextReader reader)
        {
            if (typeof(T) == typeof(IDictionary<string, object>))
            {
                var dictionary = JSON.Deserialize<T>(reader) as IDictionary<string, object>;
                return (T)dictionary.Unwrap();
            }

            return JSON.Deserialize<T>(reader);
        }       

        public static IDictionary<string, object> Unwrap(this IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            var unwrappedDictionary = new Dictionary<string, object>();
            foreach (var kvp in dictionary)
            {
                var key = kvp.Key;
                var value = kvp.Value.Unwrap();

                unwrappedDictionary.Add(key, value);
            }

            return unwrappedDictionary;
        }

        public static IList<object> Unwrap(this IEnumerable enumerable)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                list.Add(item.Unwrap());
            }
            return list;
        }

        public static object Unwrap(this object value)
        {
            if (value == null)
                return null;

            if (value.GetType().Name != "JsonObject")
                return value;

            var converter = TypeDescriptor.GetConverter(value);

            var destinationType = converter.GetDestinationType(value);
            if (destinationType == null)
                return null;

            if (destinationType == typeof(IDictionary<string, object>))
            {
                var dictionary = converter.ConvertTo(value, destinationType) as IDictionary<string, object>;
                return dictionary.Unwrap();
            }
            else if (destinationType == typeof(IEnumerable))
            {
                var enumerable = converter.ConvertTo(value, destinationType) as IEnumerable;
                return enumerable.Unwrap();
            }
            else if (destinationType == typeof(string))
            {
                var stringValue = converter.ConvertTo(value, destinationType) as string;                
                DateTime dateValue = DateTime.MinValue;
                Guid guidValue = Guid.Empty;

                if (TryParseDateTime(stringValue, ref dateValue))
                    return dateValue;
                else if (TryParseGuid(stringValue, ref guidValue))
                    return guidValue;
                else
                    return stringValue;
            }

            return converter.ConvertTo(value, destinationType);
        }        

        private static Type GetDestinationType(this TypeConverter converter, object value)
        {
            if (converter.CanConvertTo(typeof(bool)))
                return typeof(bool);
            if (converter.CanConvertTo(typeof(int)))
                return typeof(int);
            if (converter.CanConvertTo(typeof(long)))
                return typeof(long);
            if (converter.CanConvertTo(typeof(double)))
                return typeof(double);
            if (converter.CanConvertTo(typeof(DateTime)))
                return typeof(DateTime);
            if (converter.CanConvertTo(typeof(IDictionary<string, object>)))
                return typeof(IDictionary<string, object>);         
            if (converter.CanConvertTo(typeof(IEnumerable)))
                return typeof(IEnumerable);
            if (converter.CanConvertTo(typeof(string)))
                return typeof(string);

            throw new SerializationException("Unsupported data type: " + value.GetType().Name);
        }
        

        public static bool TryParseDateTime(string value, ref DateTime dateValue)
        {
            if (!IsDateTimeString(value))
                return false;

            var length = value.Length;            

            if (length == DATE_TIME_FORMAT_DATE_ONLY.Length &&
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_ONLY, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;

            if (length == DATE_TIME_FORMAT_DATE_HHMM.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMM, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMM_UTC.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMM_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMM_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMM_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DATE_TIME_FORMAT_DATE_HHMMSS.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSS, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSS_UTC.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSS_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSS_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSS_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DATE_TIME_FORMAT_DATE_HHMMSSF.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSF_UTC.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFF.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFF_UTC.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFF.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFF_UTC.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFF.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFF_UTC.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFF.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFF_UTC.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFFF.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFFFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFFF_UTC.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFFFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFFFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFFFF.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFFFFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFFFF_UTC.Length && 
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFFFFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFFFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSSFFFFFFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            return false;
        }

        public static bool TryParseGuid(string value, ref Guid guid)
        {
            return value.Length == GUID_STRING_LENGTH &&
                   value.Count(c => c == '-') == 4 &&
                   Guid.TryParse(value, out guid);
        }        

        private static bool IsDateTimeString(string value)
        {
            var length = value.Length;

            if (!(length >= DATE_TIME_FORMAT_DATE_ONLY.Length &&
                  length <= DATE_TIME_FORMAT_DATE_HHMMSSFFFFFFF_UTC.Length + 5
               ))
                return false;

            if (!(Char.IsNumber(value[0]) && Char.IsNumber(value[1]) && Char.IsNumber(value[2]) && Char.IsNumber(value[3]) &&
                  value[4] == '-' &&
                  Char.IsNumber(value[5]) && Char.IsNumber(value[6]) &&
                  value[7] == '-' &&
                  Char.IsNumber(value[8]) && Char.IsNumber(value[9])
               ))
                return false;

            if (length > DATE_TIME_FORMAT_DATE_ONLY.Length &&
                  value[10] != 'T'
               )
                return false;

            if (length == DATE_TIME_FORMAT_DATE_HHMM_UTC.Length ||
                length == DATE_TIME_FORMAT_DATE_HHMMSS_UTC.Length ||
                length == DATE_TIME_FORMAT_DATE_HHMMSSF_UTC.Length ||
                length == DATE_TIME_FORMAT_DATE_HHMMSSFF_UTC.Length ||
                length == DATE_TIME_FORMAT_DATE_HHMMSSFFF_UTC.Length ||
                length == DATE_TIME_FORMAT_DATE_HHMMSSFFFF_UTC.Length ||
                length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFF_UTC.Length ||
                length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFFF_UTC.Length ||
                length == DATE_TIME_FORMAT_DATE_HHMMSSFFFFFFF_UTC.Length
               )
            {
                if (!value.EndsWith("Z", StringComparison.InvariantCulture))
                    return false;
            }

            return true;
        }    
        
    }
}
