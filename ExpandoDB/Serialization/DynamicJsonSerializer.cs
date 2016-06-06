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
    /// Provides methods for serializing/deserializing dynamic Document object to/from JSON.
    /// </summary>
    public static class DynamicJsonSerializer
    {
        private const int GUID_STRING_LENGTH = 36;

        /// <summary>
        /// Serializes the specified Document object to JSON.
        /// </summary>
        /// <param name="document">The Document object.</param>
        /// <returns></returns>
        public static string Serialize(Document document)
        {
            return JSON.SerializeDynamic(document.AsExpando(), Options.ISO8601IncludeInherited);
        }

        /// <summary>
        /// Serializes the specified Document object to JSON and writes to the provided TextWriter.
        /// </summary>
        /// <param name="document">The Document object.</param>
        /// <param name="writer">The TextWriter object.</param>
        public static void Serialize(Document document, TextWriter writer)
        {
            JSON.SerializeDynamic(document.AsExpando(), writer, Options.ISO8601IncludeInherited);
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

            if (length == DateTimeFormat.DATE_ONLY.Length &&
                DateTime.TryParseExact(value, DateTimeFormat.DATE_ONLY, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;

            if (length == DateTimeFormat.DATE_HHMM.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMM, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMM_UTC.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMM_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMM_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMM_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DateTimeFormat.DATE_HHMMSS.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSS, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSS_UTC.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSS_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSS_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSS_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DateTimeFormat.DATE_HHMMSSF.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSF_UTC.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DateTimeFormat.DATE_HHMMSSFF.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFF_UTC.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DateTimeFormat.DATE_HHMMSSFFF.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFFF_UTC.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DateTimeFormat.DATE_HHMMSSFFFF.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFFFF_UTC.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFFFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DateTimeFormat.DATE_HHMMSSFFFFF.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFFFFF_UTC.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFFFFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DateTimeFormat.DATE_HHMMSSFFFFFF.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFFFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFFFFFF_UTC.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFFFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFFFFFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFFFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

            if (length == DateTimeFormat.DATE_HHMMSSFFFFFFF.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFFFFF, null, DateTimeStyles.AssumeLocal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFFFFFFF_UTC.Length && 
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFFFFF_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;
            if (length == DateTimeFormat.DATE_HHMMSSFFFFFFF_UTC.Length + 5 &&
                DateTime.TryParseExact(value, DateTimeFormat.DATE_HHMMSSFFFFFFF_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue)) return true;

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

            if (!(length >= DateTimeFormat.DATE_ONLY.Length &&
                  length <= DateTimeFormat.DATE_HHMMSSFFFFFFF_UTC.Length + 5
               ))
                return false;

            if (!(Char.IsNumber(value[0]) && Char.IsNumber(value[1]) && Char.IsNumber(value[2]) && Char.IsNumber(value[3]) &&
                  value[4] == '-' &&
                  Char.IsNumber(value[5]) && Char.IsNumber(value[6]) &&
                  value[7] == '-' &&
                  Char.IsNumber(value[8]) && Char.IsNumber(value[9])
               ))
                return false;

            if (length > DateTimeFormat.DATE_ONLY.Length &&
                  value[10] != 'T'
               )
                return false;

            if (length == DateTimeFormat.DATE_HHMM_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSS_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFFF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFFFF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFFFFF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFFFFFF_UTC.Length ||
                length == DateTimeFormat.DATE_HHMMSSFFFFFFF_UTC.Length
               )
            {
                if (!value.EndsWith("Z", StringComparison.InvariantCulture))
                    return false;
            }

            return true;
        }    
        
    }
}
