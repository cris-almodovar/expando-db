using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Extensions;
using System.Text.RegularExpressions;

namespace ExpandoDB.Rest.DTO
{
    public class DynamicModelBinder : IModelBinder
    {
        public object Bind(NancyContext context, Type modelType, object instance, BindingConfig configuration, params string[] blackListedProperties)
        {
            var data =
                GetDataFields(context, blackListedProperties);

            var model =
                DynamicDictionary.Create(data);

            return model;
        }

        private static IDictionary<string, object> GetDataFields(NancyContext context, params string[] blackListedProperties)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return Merge
            (
                new IDictionary<string, object>[]
                {
                    ConvertDynamicDictionary(context.Request.Form), 
                    ConvertDynamicDictionary(context.Request.Query), 
                    ConvertDynamicDictionary(context.Parameters),
                    ParseRequestBody(context.Request)
                },
                blackListedProperties
            );
        }        

        private static IDictionary<string, object> Merge(IEnumerable<IDictionary<string, object>> dictionaries, params string[] blackListedProperties)
        {
            if (dictionaries == null)
                throw new ArgumentNullException("dictionaries");

            var output =
                new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var dictionary in dictionaries.Where(d => d != null))
            {
                foreach (var kvp in dictionary)
                {
                    if (!output.ContainsKey(kvp.Key) && !blackListedProperties.Contains(kvp.Key))
                    {
                        output.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            return output;
        }

        private static IDictionary<string, object> ConvertDynamicDictionary(DynamicDictionary dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            return dictionary.GetDynamicMemberNames().ToDictionary(
                    memberName => memberName,
                    memberName => dictionary[memberName]);
        }

        private static IDictionary<string, object> ParseRequestBody(Request request)
        {
            var json = request.Body.AsString();
            if (String.IsNullOrWhiteSpace(json))
                throw new ArgumentException("The JSON string is empty");

            var dictionary = NetJSON.NetJSON.Deserialize<IDictionary<string, object>>(json);
            if (dictionary == null)
                return new Dictionary<string, object>();

            ParseDateTimeStrings(dictionary);
            return dictionary;
        }

        private static void ParseDateTimeStrings(IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            const string dateTimePattern = @"^\d{4}-\d{2}-\d{2}";
            var keysToProcess =
                            (
                                from key in dictionary.Keys
                                let value = dictionary[key]
                                where value != null && value is string && Regex.Match(value as string, dateTimePattern).Success
                                select key
                            ).ToList();

            foreach (var key in keysToProcess)
            {
                var stringValue = dictionary[key] as string;
                DateTime dateValue;
                if (DateTime.TryParse(stringValue, out dateValue))
                    dictionary[key] = dateValue;
            }
        }

        public bool CanBind(Type modelType)
        {
            return modelType == typeof(DynamicDictionary);
        }
    }
}
