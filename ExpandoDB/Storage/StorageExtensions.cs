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
        const string DATE_TIME_FORMAT_ROUNDTRIP_UTC = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        const string DATE_TIME_FORMAT_ROUNDTRIP_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
        const string DATE_TIME_FORMAT_DATE_ONLY = "yyyy-MM-dd";
        const string DATE_TIME_FORMAT_DATE_HHMM = "yyyy-MM-ddTHH:mm";
        const string DATE_TIME_FORMAT_DATE_HHMM_UTC = "yyyy-MM-ddTHH:mmZ";
        const string DATE_TIME_FORMAT_DATE_HHMM_TIMEZONE = "yyyy-MM-ddTHH:mmzzz";
        const string DATE_TIME_FORMAT_DATE_HHMMSS = "yyyy-MM-ddTHH:mm:ss";
        const string DATE_TIME_FORMAT_DATE_HHMMSS_UTC = "yyyy-MM-ddTHH:mm:ssZ";
        const string DATE_TIME_FORMAT_DATE_HHMMSS_TIMEZONE = "yyyy-MM-ddTHH:mm:sszzz";     

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
                    var itemList = ConvertList(kvp.Value as IList);
                    expandoDictionary.Add(kvp.Key, itemList);
                }                
                else
                {
                    expandoDictionary.Add(kvp);
                }
            }

            return expando;
        }

        public static bool TryParseDateTime(string value, ref DateTime dateValue)
        {
            if (!IsDateTimeString(value))
                return false;

            var length = value.Length;
            if (length == DATE_TIME_FORMAT_ROUNDTRIP_UTC.Length)
                return DateTime.TryParseExact(value, DATE_TIME_FORMAT_ROUNDTRIP_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue);
            else if (length == DATE_TIME_FORMAT_ROUNDTRIP_UTC.Length + 5)
                return DateTime.TryParseExact(value, DATE_TIME_FORMAT_ROUNDTRIP_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue);
            else if (length == DATE_TIME_FORMAT_DATE_ONLY.Length)
                return DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_ONLY, null, DateTimeStyles.AssumeLocal, out dateValue);
            else if (length == DATE_TIME_FORMAT_DATE_HHMM.Length)
                return DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMM, null, DateTimeStyles.AssumeLocal, out dateValue);
            else if (length == DATE_TIME_FORMAT_DATE_HHMM_UTC.Length)
                return DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMM_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue);
            else if (length == DATE_TIME_FORMAT_DATE_HHMM_UTC.Length + 5)
                return DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMM_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue);
            else if (length == DATE_TIME_FORMAT_DATE_HHMMSS.Length)
                return DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSS, null, DateTimeStyles.AssumeLocal, out dateValue);
            else if (length == DATE_TIME_FORMAT_DATE_HHMMSS_UTC.Length)
                return DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSS_UTC, null, DateTimeStyles.AdjustToUniversal, out dateValue);
            else if (length == DATE_TIME_FORMAT_DATE_HHMMSS_UTC.Length + 5)
                return DateTime.TryParseExact(value, DATE_TIME_FORMAT_DATE_HHMMSS_TIMEZONE, null, DateTimeStyles.AdjustToUniversal, out dateValue);

            return false;
        }

        public static bool TryParseGuid(string value, ref Guid guid)
        {
            return value.Length == GUID_STRING_LENGTH &&
                   value.Count(c => c == '-') == 4 &&
                   Guid.TryParse(value, out guid);
        }

        private static List<object> ConvertList(IList list)
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
                    var list2 = ConvertList(item as IList);
                    parsedItems.Add(list2);
                }                
                else
                {
                    parsedItems.Add(item);
                }
            }

            return parsedItems;
        }

        private static bool IsDateTimeString(string value)
        {
            var length = value.Length;

            if (!(length == DATE_TIME_FORMAT_ROUNDTRIP_UTC.Length ||
                  length == DATE_TIME_FORMAT_ROUNDTRIP_UTC.Length + 5 ||
                  length == DATE_TIME_FORMAT_DATE_ONLY.Length ||
                  length == DATE_TIME_FORMAT_DATE_HHMM.Length ||
                  length == DATE_TIME_FORMAT_DATE_HHMM_UTC.Length ||
                  length == DATE_TIME_FORMAT_DATE_HHMM_UTC.Length + 5 ||
                  length == DATE_TIME_FORMAT_DATE_HHMMSS.Length ||
                  length == DATE_TIME_FORMAT_DATE_HHMMSS_UTC.Length ||
                  length == DATE_TIME_FORMAT_DATE_HHMMSS_UTC.Length + 5                  
               ))
               return false;

            if (!(Char.IsNumber(value[0]) && Char.IsNumber(value[1]) && Char.IsNumber(value[2]) && Char.IsNumber(value[3]) &&
                  value[4] == '-' &&
                  Char.IsNumber(value[5]) && Char.IsNumber(value[6]) &&
                  value[7] == '-' &&
                  Char.IsNumber(value[8]) && Char.IsNumber(value[9])
               ))
               return false;

            if (!(length > DATE_TIME_FORMAT_DATE_ONLY.Length &&
                  value[10] == 'T'
               ))
               return false;

            if (length == DATE_TIME_FORMAT_ROUNDTRIP_UTC.Length ||
                length == DATE_TIME_FORMAT_DATE_HHMM_UTC.Length ||
                length == DATE_TIME_FORMAT_DATE_HHMMSS_UTC.Length)
            {
                if (!value.EndsWith("Z", StringComparison.InvariantCulture))
                    return false;
            }

            return true;
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
            return DynamicSerializer.Deserialize<IDictionary<string, object>>(json);
        }

        /// <summary>
        /// Deserializes the StorageRow.json string into an ExpandoObject.
        /// </summary>
        /// <param name="row">The StorageRow.</param>
        /// <returns></returns>
        public static IDictionary<string, object> ToDictionary(this StorageRow row)
        {
            IDictionary<string, object> dictionary = new Dictionary<string, object>();
            try
            {
                dictionary = row.json.ToDictionary();
            }
            catch (Exception ex)
            {
                var guid = Guid.Empty;
                Guid.TryParse(row.id, out guid);

                dictionary[Content.ID_FIELD_NAME] = guid;
                dictionary[Content.PARSE_ERROR_MESSAGE_FIELD_NAME] = ex.Message;
                dictionary[Content.PARSE_ERROR_JSON_FIELD_NAME] = row.json;
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
            var json = DynamicSerializer.Serialize(content);
            return json;
        }
    }
}
