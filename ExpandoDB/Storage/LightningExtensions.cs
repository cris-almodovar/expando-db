﻿using ExpandoDB.Serialization;
using Jil;
using System;
using System.Collections.Generic;
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
        private static readonly DeflateSerializer _serializer = new DeflateSerializer();       

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
            return _serializer.ToCompressedByteArray(document);
        }

        public static byte[] ToCompressedByteArray(this DocumentCollectionSchema collectionSchema)
        {
            return _serializer.ToCompressedByteArray(collectionSchema);
        }

        public static Document ToDocument(this byte[] value)
        {
            return _serializer.ToDocument(value);
        }

        public static DocumentCollectionSchema ToDocumentCollectionSchema(this byte[] value)
        {
            return _serializer.ToDocumentCollectionSchema(value);
        }        
    }
}
