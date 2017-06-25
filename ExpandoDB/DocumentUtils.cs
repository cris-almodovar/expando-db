using ExpandoDB.Search;
using ExpandoDB.Serialization;
using ExpandoDB.Storage;
using FlexLucene.Facet;
using Mapster;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using LuceneDocument = FlexLucene.Document.Document;

namespace ExpandoDB
{
    /// <summary>
    /// Provides utility methods for Document objects.
    /// </summary>
    public static class DocumentUtils
    {
        /// <summary>
        /// Converts all date values inside the Document object to UTC format.
        /// </summary>
        /// <param name="document">The document to process.</param>
        public static void ConvertDatesToUtc(this Document document)
        {
            document.AsDictionary().ConvertDatesToUtc();
        }

        /// <summary>
        /// Converts all date values inside the dictionary to UTC format.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        internal static void ConvertDatesToUtc(this IDictionary<string, object> dictionary)
        {
            var keysToProcess =
                    dictionary.Where(kvp => kvp.Value is DateTime || kvp.Value is IDictionary<string, object> || kvp.Value is IList)
                              .Select(kvp => kvp.Key)
                              .ToArray();

            foreach (var key in keysToProcess)
            {
                var value = dictionary[key];

                if (value is IDictionary<string, object>)
                {
                    (value as IDictionary<string, object>).ConvertDatesToUtc();
                }
                else if (value is IList)
                {
                    (value as IList).ConvertDatesToUtc();
                }
                else if (value is DateTime)
                {
                    var dateValue = (DateTime)value;
                    if (dateValue.Kind != DateTimeKind.Utc)
                    {
                        switch (dateValue.Kind)
                        {
                            case DateTimeKind.Unspecified:
                                dateValue = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc);
                                break;
                            case DateTimeKind.Local:
                                dateValue = dateValue.ToUniversalTime();
                                break;
                        }

                        dictionary[key] = dateValue;
                    }
                }
            }
        }

        /// <summary>
        /// Converts all date values inside the list to UTC format.
        /// </summary>
        /// <param name="list">The list.</param>
        private static void ConvertDatesToUtc(this IList list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item is IDictionary<string, object>)
                {
                    (item as IDictionary<string, object>).ConvertDatesToUtc();
                }
                else if (item is IList)
                {
                    (item as IList).ConvertDatesToUtc();
                }
                else if (item is DateTime)
                {
                    var dateValue = (DateTime)item;
                    if (dateValue.Kind != DateTimeKind.Utc)
                    {
                        switch (dateValue.Kind)
                        {
                            case DateTimeKind.Unspecified:
                                dateValue = DateTime.SpecifyKind(dateValue, DateTimeKind.Utc);
                                break;
                            case DateTimeKind.Local:
                                dateValue = dateValue.ToUniversalTime();
                                break;
                        }

                        list[i] = dateValue;
                    }
                }
            }
        }

        /// <summary>
        /// Converts the specified object to a Document object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static Document ToDocument<T>(this T value)
        {
            var dictionary = value.ToDictionary();
            var document = new Document(dictionary);
            return document;
        }

        /// <summary>
        /// To the specified document.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        public static T To<T> (this Document document)
        {            
            return To<T>(document.AsDictionary());
        }

        /// <summary>
        /// To the specified document.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns></returns>
        public static T To<T>(this IDictionary<string, object> dictionary)
        {
            var json = DynamicJsonSerializer.Serialize(dictionary);
            var obj = DynamicJsonSerializer.Deserialize<T>(json);
            return obj;
        }

        /// <summary>
        /// Converts a IDictionary instance to an ExpandoObject.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns></returns>        
        public static ExpandoObject ToExpando(this IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException(nameof(dictionary));

            var expando = new ExpandoObject();
            var expandoDictionary = (IDictionary<string, object>)expando;

            foreach (var kvp in dictionary)
            {
                if (kvp.Value is IDictionary<string, object>)
                {
                    var expandoValue = ((IDictionary<string, object>)kvp.Value).ToExpando();
                    expandoDictionary.Add(kvp.Key, expandoValue);
                }
                else if (kvp.Value is IList)
                {
                    var itemList = ConvertItemsToExpando(kvp.Value as IList);
                    expandoDictionary.Add(kvp.Key, itemList);
                }
                else
                {
                    expandoDictionary.Add(kvp);
                }
            }

            return expando;
        }

        private static List<object> ConvertItemsToExpando(IList list)
        {
            var parsedItems = new List<object>();
            foreach (var item in list)
            {
                if (item is IDictionary<string, object>)
                {
                    var expando = ((IDictionary<string, object>)item).ToExpando();
                    parsedItems.Add(expando);
                }
                else if (item is IList)
                {
                    var list2 = ConvertItemsToExpando(item as IList);
                    parsedItems.Add(list2);
                }
                else
                {
                    parsedItems.Add(item);
                }
            }

            return parsedItems;
        }       
        

        /// <summary>
        /// Deserializes the JSON string into an IDictionary object.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns></returns>
        public static IDictionary<string, object> ToDictionary(this string json)
        {
            return DynamicJsonSerializer.Deserialize<IDictionary<string, object>>(json);
        }                

        /// <summary>
        /// Deserializes the key-value pairs to Document objects.
        /// </summary>
        /// <param name="keyValuePairs">The key value pairs.</param>
        /// <returns></returns>
        internal static EnumerableDocuments ToEnumerableDocuments(this IEnumerable<LightningKeyValuePair> keyValuePairs)
        {
            return new EnumerableDocuments(keyValuePairs);
        }        

        /// <summary>
        /// Copies the Document object to a new Dictionary object.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>        
        public static IDictionary<string, object> ToDictionary(this Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var dictionary = new Dictionary<string, object>(document.AsDictionary());
            return dictionary;
        }

        /// <summary>
        /// Computes the MD5 hash of the given string value; 
        /// the hash is returned as a byte array.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns></returns>
        public static byte[] ComputeMd5Hash(this string value)
        {
            if (String.IsNullOrWhiteSpace(value))
                throw new ArgumentException(nameof(value));

            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(value);
                byte[] hash = md5.ComputeHash(data);                                
                
                return hash;
            }
        }

        /// <summary>
        /// Computes the MD5 hash of the given string; 
        /// the hash is returned as a hexadecimal string.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <returns></returns>
        public static string ComputeMd5HashAsString(this string value)
        {
            byte[] hash = value.ComputeMd5Hash();
            var buffer = new System.Text.StringBuilder();
            for (int i = 0; i < hash.Length; i++)
                  buffer.Append(hash[i].ToString("x2"));

            // Return the hexadecimal string.
            return buffer.ToString();            
        }


        /// <summary>
        /// To the dictionary.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">value</exception>
        public static IDictionary<string, object> ToDictionary<T>(this T value)
        {
            if ((value as object) == null)
                throw new ArgumentNullException(nameof(value));

            var dictionary = (value is IDictionary<string, object>) ?
                             new Dictionary<string, object>(value as IDictionary<string, object>) :
                             TypeAdapter.Adapt<Dictionary<string, object>>(value);

            var keyList = dictionary.Keys.ToList();
            foreach (var key in keyList)
            {
                var item = dictionary[key];
                if (item == null)
                    continue;

                var type = item.GetType();
                var typeCode = Type.GetTypeCode(type);

                switch (typeCode)
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
                    case TypeCode.Boolean:
                    case TypeCode.String:
                    case TypeCode.DateTime:
                    case TypeCode.Empty:
                        continue;

                    case TypeCode.Object:
                        if (type == typeof(Guid) || type == typeof(Guid?))
                            continue;
                        else if (item is IDictionary)
                            dictionary[key] = (item as IDictionary).ToDictionary();                        
                        else if (item is IList)
                            dictionary[key] = (item as IList).ToDictionaryList();
                        else if (item is IEnumerable)
                            dictionary[key] = (item as IEnumerable).ToDictionaryList();
                        else
                            dictionary[key] = item.ToDictionary(); 
                        break;
                }
            }

            return dictionary;
        }

        internal static IDictionary<string, object> ToDictionary(this IDictionary dictionary)
        {
            var stringObjectDictionary = new Dictionary<string, object>();
            var keys = dictionary.Keys.Cast<string>().ToList();
            keys.ForEach(key => stringObjectDictionary.Add(key, dictionary[key]));

            return stringObjectDictionary.ToDictionary();
        }

        /// <summary>
        /// To the compatible list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns></returns>
        public static IList ToDictionaryList(this IList list)
        {
            return (list as IEnumerable).ToDictionaryList();
        }

        /// <summary>
        /// To the compatible list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns></returns>
        public static IList ToDictionaryList(this IEnumerable list)
        {
            var newList = new List<object>();
            var enumerator = list.GetEnumerator();

            while (enumerator.MoveNext())
            {
                var item = enumerator.Current;
                if (item == null)
                {
                    newList.Add(null);
                }
                else
                {
                    var type = item.GetType();
                    var typeCode = Type.GetTypeCode(type);

                    switch (typeCode)
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
                        case TypeCode.Boolean:
                        case TypeCode.String:
                        case TypeCode.DateTime:
                        case TypeCode.Empty:
                            newList.Add(item);
                            break;

                        case TypeCode.Object:
                            if (type == typeof(Guid) || type == typeof(Guid?))                            
                                newList.Add(item);
                            else if (item is IDictionary)
                                newList.Add((item as IDictionary).ToDictionary());
                            else if (item is IList)                            
                                continue; // Skip - array of array is NOT supported
                            else if (item is IEnumerable)
                                continue; // Skip - array of array is NOT supported                            
                            else
                                newList.Add(item.ToDictionary());
                            break;
                    }
                }

            }            

            return newList;
        }
    }
}
