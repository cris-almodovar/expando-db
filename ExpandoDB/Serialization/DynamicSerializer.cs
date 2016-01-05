using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jil;
using System.IO;
using System.ComponentModel;
using System.Collections;
using ExpandoDB.Storage;

namespace ExpandoDB.Serialization
{
    public static class DynamicSerializer
    {
        public static string Serialize(Content content)
        {
            return JSON.SerializeDynamic(content.AsExpando(), Options.ISO8601IncludeInherited);
        }

        public static void Serialize(Content content, TextWriter output)
        {
            JSON.SerializeDynamic(content.AsExpando(), output, Options.ISO8601IncludeInherited);
        }

        public static string Serialize(object data)
        {
            return JSON.SerializeDynamic(data, Options.ISO8601IncludeInherited);
        }

        public static void Serialize(object data, TextWriter output)
        {
            JSON.SerializeDynamic(data, output, Options.ISO8601IncludeInherited);
        }

        public static Content Deserialize(string json)
        {
            var dictionary = Deserialize<IDictionary<string, object>>(json);
            return new Content(dictionary);
        }

        public static T Deserialize<T>(string json)
        {
            if (typeof(T) == typeof(IDictionary<string, object>))
            {
                var dictionary = JSON.Deserialize<T>(json) as IDictionary<string, object>;
                return (T)dictionary.Unwrap();
            }

            return JSON.Deserialize<T>(json);
        }

        public static T Deserialize<T>(TextReader reader)
        {
            if (typeof(T) == typeof(IDictionary<string, object>))
            {
                var dictionary = JSON.Deserialize<T>(reader) as IDictionary<string, object>;
                return (T)dictionary.Unwrap();
            }

            return JSON.Deserialize<T>(reader);
        }       

        private static IDictionary<string, object> Unwrap(this IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            var unwrappedDictionary = new Dictionary<string, object>();
            foreach (var kvp in dictionary)
            {
                var key = kvp.Key;
                var value = kvp.Value.Unwrap();

                unwrappedDictionary.Add(key, value);
            }

            return unwrappedDictionary;
        }

        private static object Unwrap(this object value)
        {
            if (value == null)
                return null;

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

                if (StorageExtensions.TryParseDateTime(stringValue, ref dateValue))
                    return dateValue;
                else if (StorageExtensions.TryParseGuid(stringValue, ref guidValue))
                    return guidValue;
                else
                    return stringValue;
            }

            return converter.ConvertTo(value, destinationType);
        }

        private static IList<object> Unwrap(this IEnumerable enumerable)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                list.Add(item.Unwrap());
            }
            return list;
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
    }
}
