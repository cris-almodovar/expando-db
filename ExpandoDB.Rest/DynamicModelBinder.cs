using ExpandoDB.Rest.DTO;
using ExpandoDB.Serialization;
using Nancy;
using Nancy.Extensions;
using Nancy.ModelBinding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ExpandoDB.Rest
{
    /// <summary>
    /// Binds request data to a DynamicDictionary object. 
    /// </summary>
    /// <seealso cref="Nancy.ModelBinding.IModelBinder" />
    public class DynamicModelBinder : IModelBinder
    {
        public object Bind(NancyContext context, Type modelType, object instance, BindingConfig configuration, params string[] blackListedProperties)
        {
            object model = null;
            if (modelType == typeof(DynamicDictionary))
            {
                var data = GetDataFields(context, blackListedProperties);
                model = DynamicDictionary.Create(data);
                
            }
            else if (modelType == typeof(IList<PatchOperationDto>))
            {
                model = ParsePatchOperationsList(context.Request);
            }

            return model;
        }

        private static IDictionary<string, object> GetDataFields(NancyContext context, params string[] blackListedProperties)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            return Merge
            (
                new IDictionary<string, object>[]
                {
                    ConvertDynamicDictionary(context.Request.Form), 
                    ConvertDynamicDictionary(context.Request.Query), 
                    ConvertDynamicDictionary(context.Parameters),
                    ParseDictionary(context.Request)
                },
                blackListedProperties
            );
        }        

        private static IDictionary<string, object> Merge(IEnumerable<IDictionary<string, object>> dictionaries, params string[] blackListedProperties)
        {
            if (dictionaries == null)
                throw new ArgumentNullException(nameof(dictionaries));

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
                throw new ArgumentNullException(nameof(dictionary));

            return dictionary.GetDynamicMemberNames().ToDictionary(
                    memberName => memberName,
                    memberName => dictionary[memberName]);
        }

        private static IDictionary<string, object> ParseDictionary(Request request)
        {
            var json = request.Body.AsString();
            if (String.IsNullOrWhiteSpace(json))
                throw new ArgumentException("The JSON string is empty");

            var model = DynamicJsonSerializer.Deserialize<IDictionary<string, object>>(json);
            if (model == null)
                return new Dictionary<string, object>();
            
            return model;            
        }

        private static IList<PatchOperationDto> ParsePatchOperationsList(Request request)
        {
            var json = request.Body.AsString();
            if (String.IsNullOrWhiteSpace(json))
                throw new ArgumentException("The JSON string is empty");

            var operations = DynamicJsonSerializer.Deserialize<IList<PatchOperationDto>>(json) ?? new List<PatchOperationDto>();
            foreach (var op in operations)
                op.value = op.value.Unwrap();
            
            return operations;
        }

        public bool CanBind(Type modelType)
        {
            return (modelType == typeof(DynamicDictionary) || modelType == typeof(IList<PatchOperationDto>));
        }
    }
}
