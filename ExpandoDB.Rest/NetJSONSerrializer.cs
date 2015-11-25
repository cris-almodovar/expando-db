// Based on: https://github.com/jbattermann/Nancy.Serialization.NetJSON/blob/master/Nancy.Serialization.NetJSON/NetJSONSerializer.cs

using System.Collections.Generic;
using System.IO;
using Nancy.IO;
using System;
using ExpandoDB.Service.DTO;

namespace Nancy.Serialization.NetJSON
{
    /// <summary>
    /// A <see cref="ISerializer"/> implementation based on <see cref="NetJSON"/>.
    /// </summary>
    public class NetJSONSerializer : ISerializer
    {
        private readonly Json.JavaScriptSerializer _defaultSerializer = new Json.JavaScriptSerializer { RetainCasing = true, ISO8601DateFormat = true };
        private readonly Type _typeOfIResponseDto = typeof(IResponseDto);

        #region Implementation of ISerializer

        /// <summary>
        /// Whether the serializer can serialize the content type
        /// </summary>
        /// <param name="contentType">Content type to serialize</param>
        /// <returns>
        /// True if supported, false otherwise
        /// </returns>
        public bool CanSerialize(string contentType)
        {
            return Helpers.IsJsonType(contentType);
        }

        /// <summary>
        /// Serializes the given model instance with the given contentType
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="contentType">Content type to serialize into</param>
        /// <param name="model">Model instance to serialize.</param>
        /// <param name="outputStream">Output stream to serialize to.</param>
        public void Serialize<TModel>(string contentType, TModel model, Stream outputStream)
        {
            using (var output = new StreamWriter(new UnclosableStreamWrapper(outputStream)))
            {
                var modelType = model.GetType();
                var json = String.Empty;

                if (_typeOfIResponseDto.IsAssignableFrom(modelType))
                    json = global::NetJSON.NetJSON.Serialize(modelType, model);
                else
                    json = _defaultSerializer.Serialize(model); 

                output.Write(json);
            }
        }

        /// <summary>
        /// Gets the list of extensions that the serializer can handle.
        /// </summary>
        /// <value>
        /// An <see cref="T:System.Collections.Generic.IEnumerable`1"/> of extensions if any are available, otherwise an empty enumerable.
        /// </value>
        public IEnumerable<string> Extensions
        {
            get { yield return "json"; }
        }

        #endregion
    }

    internal static class Helpers
    {
        /// <summary>
        /// Attempts to detect if the content type is JSON.
        /// Supports:
        ///   application/json
        ///   text/json
        ///   application/vnd[something]+json
        /// Matches are case insentitive to try and be as "accepting" as possible.
        /// </summary>
        /// <param name="contentType">Request content type</param>
        /// <returns>True if content type is JSON, false otherwise</returns>
        public static bool IsJsonType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            var contentMimeType = contentType.Split(';')[0];

            return contentMimeType.Equals("application/json", StringComparison.InvariantCultureIgnoreCase) ||
                   contentMimeType.Equals("text/json", StringComparison.InvariantCultureIgnoreCase) ||
                   (contentMimeType.StartsWith("application/vnd", StringComparison.InvariantCultureIgnoreCase) &&
                    contentMimeType.EndsWith("+json", StringComparison.InvariantCultureIgnoreCase));
        }
    }
}