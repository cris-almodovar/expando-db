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
    /// Serializes/deserializes Document object to/from JSON strings.
    /// </summary>
    /// <remarks>
    /// DynamicJsonSerializer uses the excellent Jil library (https://github.com/kevin-montrose/Jil).
    /// </remarks>
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
                var dictionary = JSON.Deserialize<T>(json, Options.ISO8601IncludeInherited) as IDictionary<string, object>;
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
                var dictionary = JSON.Deserialize<T>(reader, Options.ISO8601IncludeInherited) as IDictionary<string, object>;
                return (T)dictionary.Unwrap();
            }

            return JSON.Deserialize<T>(reader, Options.ISO8601IncludeInherited);
        }       

        internal static IDictionary<string, object> Unwrap(this IDictionary<string, object> dictionary)
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

        internal static IList<object> Unwrap(this IEnumerable enumerable)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                list.Add(item.Unwrap());
            }
            return list;
        }

        internal static object Unwrap(this object value)
        {
            if (value == null)
                return null;

            // NOTE: We only need to unwrap the value if the Type is JsonObject; 
            // this type is a Jil-specific internal wrapper class that holds the actual 
            // JSON values. This class cannot be manipulated directly - we need
            // to 'unwrap' the actual values using a TypeConverter object.

            if (value.GetType().Name != "JsonObject")
                return value;

            var typeConverter = TypeDescriptor.GetConverter(value);
            var type = typeConverter.GetWrappedType(value);            

            if (type == typeof(IDictionary<string, object>))
            {
                var dictionary = typeConverter.ConvertTo(value, type) as IDictionary<string, object>;
                return dictionary.Unwrap();
            }
            else if (type == typeof(IEnumerable))
            {
                var enumerable = typeConverter.ConvertTo(value, type) as IEnumerable;
                return enumerable.Unwrap();
            }
            else if (type == typeof(string))
            {
                var stringValue = typeConverter.ConvertTo(value, type) as string;                
                DateTime dateValue = DateTime.MinValue;
                Guid guidValue = Guid.Empty;

                if (TryParseDateTime(stringValue, ref dateValue))
                    return dateValue;
                else if (TryParseGuid(stringValue, ref guidValue))
                    return guidValue;
                else
                    return stringValue;
            }

            return typeConverter.ConvertTo(value, type);
        }  
        
        private static Type GetWrappedType(this TypeConverter converter, object value)
        {
            if (converter == null)
                throw new ArgumentNullException(nameof(converter));
            if (value == null)
                throw new ArgumentNullException(nameof(value));            

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


        /// <summary>
        /// Tries to convert the given string value to DateTime.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <param name="dateValue">The DateTime value.</param>
        /// <returns>true if successful; othewise false.</returns>
        internal static bool TryParseDateTime(string value, ref DateTime dateValue)
        {
            if (!value.IsMaybeDateTime())
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

        /// <summary>
        /// Performs a quick check to determine if the given string value can be converted to DateTime.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns></returns>
        private static bool IsMaybeDateTime(this string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                return false;

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

        /// <summary>
        /// Tries to convert the given string value to Guid.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <param name="guidValue">The Guid value.</param>
        /// <returns></returns>
        internal static bool TryParseGuid(string value, ref Guid guidValue)
        {
            return value.Length == GUID_STRING_LENGTH &&
                   value.Count(c => c == '-') == 4 &&
                   Guid.TryParse(value, out guidValue);
        }

    }
}
