using ExpandoDB.Compression;
using ExpandoDB.Serialization;
using Jil;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Storage
{
    /// <summary>
    /// Implements utility methods used by the <see cref="LightningStorageEngine"/> class.
    /// </summary>
    public static class LightningStorageUtils
    {
        private static readonly ByteArraySerializer _serializer;
        internal static readonly CompressionOption _compressionOption;

        /// <summary>
        /// Initializes the <see cref="LightningStorageUtils"/> class.
        /// </summary>
        static LightningStorageUtils()
        {
            _compressionOption = (CompressionOption) Enum.Parse(typeof(CompressionOption), (ConfigurationManager.AppSettings["StorageEngine.Compression"] ?? "LZ4"), true);
            _serializer = new ByteArraySerializer(_compressionOption);
        }

        /// <summary>
        /// Converts the specified Document object to a LightningKeyValuePair object.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static LightningKeyValuePair ToKeyValuePair(this Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var key = document._id.Value.ToByteArray();
            var value = document.ToByteArray();

            return new LightningKeyValuePair { Key = key, Value = value };
        }

        /// <summary>
        /// Converts the specified LightningKeyValuePair object to a Document object.
        /// </summary>
        /// <param name="kv">The kv.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static Document ToDocument(this LightningKeyValuePair kv)
        {
            if (kv == null)
                throw new ArgumentNullException(nameof(kv));

            return kv.Value.ToDocument();
        }        

        /// <summary>
        /// Converts the specified Document object to a byte array.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns></returns>
        public static byte[] ToByteArray(this Document document)
        {
            return _serializer.Serialize(document);
        }

        /// <summary>
        /// Converts the specified byte array to a Document object.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static Document ToDocument(this byte[] value)
        {
            return _serializer.Deserialize(value);
        }             
    }

    /// <summary>
    /// Represents the LightningStorageEngine data compression option
    /// </summary>
    public enum CompressionOption
    {
        /// <summary>
        /// Data is not compressed.
        /// </summary>
        None,
        /// <summary>
        /// Data is compressed using the LZ4 compression algorithm.
        /// </summary>
        LZ4,

        /// <summary>
        /// Data is compressed using the Deflate/zlib compression algorithm.
        /// </summary>
        Deflate
    }
}
