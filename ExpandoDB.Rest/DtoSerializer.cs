using ExpandoDB.Serialization;
using Nancy;
using Nancy.IO;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Rest
{
    /// <summary>
    /// A <see cref="ISerializer"/> implementation based on <see cref="DynamicJsonSerializer"/>.
    /// </summary>
    /// <remarks>
    /// Based on https://github.com/jbattermann/Nancy.Serialization.Jil/blob/master/Nancy.Serialization.Jil/JilSerializer.cs
    /// </remarks>
    public class DtoSerializer : ISerializer
    {
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
                DynamicJsonSerializer.Serialize(model, output);                                            
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
}
