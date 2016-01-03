using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jil;
using System.IO;
using System.ComponentModel;
using System.Collections;

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
            return new Content(json.DeserializeDictionary());
        }

        public static Dictionary<string, object> DeserializeDictionary(this string json)
        {
            if (String.IsNullOrWhiteSpace(json))
                throw new ArgumentNullException("json");
            
            var dictionary = JSON.Deserialize<Dictionary<string, object>>(json);                                     

            return dictionary.Unwrap();
        }

        public static T Deserialize<T>(string json)
        {
            return JSON.Deserialize<T>(json);
        }

        private static Dictionary<string, object> Unwrap(this IDictionary<string, object> dictionary)
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

            return converter.ConvertTo(value, destinationType); 
        }

        private static List<object> Unwrap(this IEnumerable enumerable)
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


            return null;
        }
    }
}
