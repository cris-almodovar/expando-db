using ExpandoDB.Serialization;
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
        const string ISO_UTC_DATE_TIME_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        const string ISO_UTC_DATE_TIME_SHORT_FORMAT = "yyyy-MM-ddTHH:mm:ssZ";
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

                    if (TryParseIsoUtcDateTime(value, ref dateValue))
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

        static bool TryParseIsoUtcDateTime(string value, ref DateTime dateValue)
        {
            if (!IsIsoUtcDateTime(value))
                return false;

            if (value.Length == ISO_UTC_DATE_TIME_FORMAT.Length)
                return DateTime.TryParseExact(value, ISO_UTC_DATE_TIME_FORMAT, null, DateTimeStyles.AdjustToUniversal, out dateValue);
            else if (value.Length == ISO_UTC_DATE_TIME_SHORT_FORMAT.Length)
                return DateTime.TryParseExact(value, ISO_UTC_DATE_TIME_SHORT_FORMAT, null, DateTimeStyles.AdjustToUniversal, out dateValue);

            return false;
        }        

        internal static bool TryParseGuid(string value, ref Guid guid)
        {
            return value.Length == GUID_STRING_LENGTH && 
                   value.Count(c => c == '-') == 4 && 
                   Guid.TryParse(value, out guid);
        }

        private static List<object> ParseList(IList list)
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
                    var list2 = ParseList(item as IList);
                    parsedItems.Add(list2);
                }
                else if (item is string)
                {
                    var value = item as string;
                    DateTime dateValue = DateTime.MinValue;

                    if (TryParseIsoUtcDateTime(value, ref dateValue))
                        parsedItems.Add(dateValue);
                    else
                        parsedItems.Add(value);
                }
                else
                {
                    parsedItems.Add(item);
                }
            }

            return parsedItems;
        }

        private static bool IsIsoUtcDateTime(string value)
        {
            return ((value.Length == ISO_UTC_DATE_TIME_FORMAT.Length || value.Length == ISO_UTC_DATE_TIME_SHORT_FORMAT.Length) &&
                    Char.IsNumber(value[0]) && Char.IsNumber(value[1]) && Char.IsNumber(value[2]) && Char.IsNumber(value[3]) &&
                    value[4] == '-' &&
                    Char.IsNumber(value[5]) && Char.IsNumber(value[6]) &&
                    value[7] == '-' &&
                    Char.IsNumber(value[8]) && Char.IsNumber(value[9]) &&
                    value[10] == 'T' &&
                    value.EndsWith("Z", StringComparison.InvariantCulture));
        }

        internal static void ConvertDatesToIsoUtc(this IDictionary<string, object> dictionary)
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
                    (value as IDictionary<string, object>).ConvertDatesToIsoUtc();
                }
                else if (value is IList)
                {                    
                    (value as IList).ConvertDatesToIsoUtc();
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

        private static void ConvertDatesToIsoUtc(this IList list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item is IDictionary<string, object>)
                {
                    (item as IDictionary<string, object>).ConvertDatesToIsoUtc();
                }
                else if (item is IList)
                {                    
                    (item as IList).ConvertDatesToIsoUtc();
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
        /// Deserializes the StorageRow.json string into a Content object.
        /// </summary>
        /// <param name="row">The StorageRow.</param>
        /// <returns></returns>
        public static Content ToContent(this StorageRow row)
        {
            return new Content(row.ToExpando());
        }

        /// <summary>
        /// Deserializes the JSON string into an ExpandoObject.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns></returns>
        public static ExpandoObject ToExpando(this string json)
        {            
            return json.ToDictionary().ToExpando();
        }

        /// <summary>
        /// Deserializes the StorageRow.json string into an ExpandoObject.
        /// </summary>
        /// <param name="row">The StorageRow.</param>
        /// <returns></returns>
        public static ExpandoObject ToExpando(this StorageRow row)
        {
            return row.ToDictionary().ToExpando();
        }

        /// <summary>
        /// Deserializes the JSON string into an IDictionary object.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns></returns>
        public static IDictionary<string, object> ToDictionary(this string json)
        {
            return ToDictionary(null, json);
        }

        /// <summary>
        /// Deserializes the StorageRow.json string into an ExpandoObject.
        /// </summary>
        /// <param name="row">The StorageRow.</param>
        /// <returns></returns>
        public static IDictionary<string, object> ToDictionary(this StorageRow  row)
        {
            return ToDictionary(row.id, row.json);
        }

        /// <summary>
        /// Deserializes the JSON string into an IDictionary object.
        /// </summary>
        /// <param name="json">The JSON string to deserialize.</param>
        /// <returns></returns>
        public static IDictionary<string, object> ToDictionary(string id, string json)
        {
            IDictionary<string, object> dictionary = new Dictionary<string, object>();
            try
            {
                if (String.IsNullOrWhiteSpace(json))
                    throw new ArgumentException("JSON string is null or empty");

                dictionary = json.DeserializeDictionary();
            }
            catch (Exception ex)
            {
                var guid = Guid.Empty;
                Guid.TryParse(id, out guid);

                dictionary[Content.ID_FIELD_NAME] = guid;
                dictionary[Content.PARSE_ERROR_MESSAGE_FIELD_NAME] = ex.Message;
                dictionary[Content.PARSE_ERROR_JSON_FIELD_NAME] = json;
            }

            return dictionary;
        }


        /// <summary>
        /// Deserializes the JSON results .
        /// </summary>
        /// <param name="jsonResults">The json results.</param>
        /// <returns></returns>
        internal static EnumerableContents ToEnumerableContents(this IEnumerable<StorageRow> rows)
        {
            return new EnumerableContents(rows);
        }

        /// <summary>
        /// Converts all dates inside the Content to UTC format.
        /// </summary>
        /// <param name="content">The content to process.</param>
        public static void ConvertDatesToIsoUtc(this Content content)
        {
            content.AsDictionary().ConvertDatesToIsoUtc();
        }

        /// <summary>
        /// Serializes the Content object to a JSON string.
        /// </summary>
        /// <param name="content">The content object to serialize.</param>
        /// <returns></returns>
        public static string ToJson(this Content content)
        {
            var json = DynamicSerializer.Serialize(content); 
            return json;
        }
    }
}
