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

        static LightningStorageUtils()
        {
            _compressionOption = (CompressionOption) Enum.Parse(typeof(CompressionOption), (ConfigurationManager.AppSettings["StorageEngine.Compression"] ?? "LZ4"), true);
            _serializer = new ByteArraySerializer(_compressionOption);
        }

        public static LightningKeyValuePair ToKeyValuePair(this Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var key = document._id.Value.ToByteArray();
            var value = document.ToCompressedByteArray();

            return new LightningKeyValuePair { Key = key, Value = value };
        }        

        public static Document ToDocument(this LightningKeyValuePair kv)
        {
            if (kv == null)
                throw new ArgumentNullException(nameof(kv));

            return kv.Value.ToDocument();
        }        

        public static byte[] ToByteArray(this string stringValue)
        {
            if (stringValue == null)
                throw new ArgumentNullException(nameof(stringValue));

            return Encoding.UTF8.GetBytes(stringValue);
        }

        public static byte[] ToCompressedByteArray(this Document document)
        {
            return _serializer.Serialize(document);
        }        

        public static Document ToDocument(this byte[] value)
        {
            return _serializer.Deserialize(value);
        }             
    }

    /// <summary>
    /// 
    /// </summary>
    public enum CompressionOption
    {
        None,
        LZ4,
        Deflate
    }
}
