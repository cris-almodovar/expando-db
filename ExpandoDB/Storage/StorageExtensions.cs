using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// Provides utility methods for persisting Content objects.
    /// </summary>
    public static class StorageExtensions
    {
        const string ISO_DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        const int GUID_STRING_LENGTH = 36;             

        /// <summary>
        /// Converts a IDictionary instance to an ExpandoObject.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns></returns>        
        public static ExpandoObject ToExpando(this IDictionary<string, object> dictionary)
        {
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
                    var itemList = ParseList(kvp.Value as IList);
                    expandoDictionary.Add(kvp.Key, itemList);
                }
                else if (kvp.Value is string)
                {
                    var value = kvp.Value as string;
                    DateTime dateValue = DateTime.MinValue;
                    Guid guidValue = Guid.Empty;

                    if (TryParseIsoDateTime(value, ref dateValue))
                        expandoDictionary.Add(kvp.Key, dateValue);
                    else if (TryParseGuid(value, ref guidValue))
                        expandoDictionary.Add(kvp.Key, guidValue);
                    else
                        expandoDictionary.Add(kvp);
                }
                else
                {
                    expandoDictionary.Add(kvp);
                }
            }
            
            return expando;
        }

        private static bool TryParseIsoDateTime(string value, ref DateTime dateValue)
        {
            return IsIsoDateTime(value) &&
                   DateTime.TryParseExact(value, ISO_DATE_TIME_FORMAT, null, DateTimeStyles.AdjustToUniversal, out dateValue);                  
        }

        private static bool TryParseGuid(string value, ref Guid guid)
        {
            return IsGuidString(value) && Guid.TryParse(value, out guid);
        }

        private static bool IsGuidString(string value)
        {
            return value.Length == GUID_STRING_LENGTH &&
                   value.Count(c => c == '-') == 4;
        }

        private static List<object> ParseList(IList list)
        {
            var itemList = new List<object>();
            foreach (var item in list)
            {
                if (item is IDictionary<string, object>)
                {
                    var expandoItem = ((IDictionary<string, object>)item).ToExpando();
                    itemList.Add(expandoItem);
                }
                else if (item is IList)
                {
                    var itemList2 = ParseList(item as IList);
                    itemList.Add(itemList2);
                }
                else if (item is string)
                {
                    var value = item as string;
                    DateTime dateValue = DateTime.MinValue;

                    if (TryParseIsoDateTime(value, ref dateValue))
                        itemList.Add(dateValue);
                    else
                        itemList.Add(value);
                }
                else
                {
                    itemList.Add(item);
                }
            }

            return itemList;
        }

        private static bool IsIsoDateTime(string value)
        {
            return (value.Length == ISO_DATE_TIME_FORMAT.Length &&
                    Char.IsNumber(value[0]) && Char.IsNumber(value[1]) && Char.IsNumber(value[2]) && Char.IsNumber(value[3]) &&
                    value[4] == '-' &&
                    Char.IsNumber(value[5]) && Char.IsNumber(value[6]) &&
                    value[7] == '-' &&
                    Char.IsNumber(value[8]) && Char.IsNumber(value[9]) &&
                    value[10] == 'T' &&
                    value.EndsWith("Z", StringComparison.InvariantCulture));
        }

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
                        dateValue = dateValue.ToUniversalTime();
                        dictionary[key] = dateValue;
                    }                  
                }                
            }            
        }               

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
        /// Deserializes the JSON string into a Content object.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns></returns>
        public static Content ToContent(this string json)
        {         
            return new Content(json.ToExpando());
        }

        /// <summary>
        /// Deserializes the JSON string into an ExpandoObject.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns></returns>
        public static ExpandoObject ToExpando(this string json)
        {
            if (String.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON string is null or empty");

            var dictionary = NetJSON.NetJSON.Deserialize<IDictionary<string, object>>(json);
            var expando = dictionary.ToExpando();
            return expando;
        }

        /// <summary>
        /// Deserializes the JSON results .
        /// </summary>
        /// <param name="jsonResults">The json results.</param>
        /// <returns></returns>
        public static EnumerableContents ToEnumerableContents(this IEnumerable<string> jsonResults)
        {
            return new EnumerableContents(jsonResults);
        }

        /// <summary>
        /// Converts all dates inside the Content to UTC format.
        /// </summary>
        /// <param name="content">The content to process.</param>
        public static void ConvertDatesToUtc(this Content content)
        {
            content.AsDictionary().ConvertDatesToUtc();
        }

        /// <summary>
        /// Serializes the Content object to a JSON string.
        /// </summary>
        /// <param name="content">The content object to serialize.</param>
        /// <returns></returns>
        public static string ToJson(this Content content)
        {
            var json = NetJSON.NetJSON.Serialize(content.AsExpando());
            return json;
        }
    }
}
