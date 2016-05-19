using ExpandoDB.Serialization;
using ExpandoDB.Storage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace ExpandoDB
{
    /// <summary>
    /// Provides utility methods for Document objects.
    /// </summary>
    public static class DocumentExtensions
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
        private static void ConvertDatesToUtc(this IDictionary<string, object> dictionary)
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
                        dateValue = dateValue.ToUniversalTime();
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
                        dateValue = dateValue.ToUniversalTime();
                        list[i] = dateValue;
                    }
                }
            }
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
        /// Deserializes the JSON string into a Document object.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns></returns>
        public static Document ToDocument(this string json)
        {
            return new Document(json.ToDictionary().ToExpando());
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
        /// Serializes the Document object to a JSON string.
        /// </summary>
        /// <param name="document">The document object to serialize.</param>
        /// <returns></returns>
        public static string ToJson(this Document document)
        {
            var json = DynamicJsonSerializer.Serialize(document);
            return json;
        }

        /// <summary>
        /// Clones the Document object to a Dictionary object.
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
    }
}
