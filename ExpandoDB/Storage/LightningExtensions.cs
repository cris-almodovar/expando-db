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
    public static class LightningExtensions
    {
        private static readonly ByteArraySerializer _serializer; 
        private static readonly CompressionOption _compressionOption;

        static LightningExtensions()
        {
            _compressionOption = (CompressionOption) Enum.Parse(typeof(CompressionOption), (ConfigurationManager.AppSettings["LightningStorageEngine.Compression"] ?? "LZ4"), true);
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

        public static LightningKeyValuePair ToKeyValuePair(this DocumentCollectionSchema collectionSchema)
        {
            if (collectionSchema == null)
                throw new ArgumentNullException(nameof(collectionSchema));

            var key = collectionSchema.Name.ToByteArray();
            var value = collectionSchema.ToCompressedByteArray();

            return new LightningKeyValuePair { Key = key, Value = value };
        }

        public static Document ToDocument(this LightningKeyValuePair kv)
        {
            if (kv == null)
                throw new ArgumentNullException(nameof(kv));

            return kv.Value.ToDocument();
        }

        public static DocumentCollectionSchema ToDocumentCollectionSchema(this LightningKeyValuePair kv)
        {
            if (kv == null)
                throw new ArgumentNullException(nameof(kv));

            return kv.Value.ToDocumentCollectionSchema();
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

        public static byte[] ToCompressedByteArray(this DocumentCollectionSchema collectionSchema)
        {
            return _serializer.Serialize(collectionSchema);
        }

        public static Document ToDocument(this byte[] value)
        {
            return _serializer.DeserializeToDocument(value);
        }

        public static DocumentCollectionSchema ToDocumentCollectionSchema(this byte[] value)
        {
            return _serializer.DeserializeToDocumentCollectionSchema(value);
        }        
    }

    public enum CompressionOption
    {
        None,
        LZ4,
        Deflate
    }
}
