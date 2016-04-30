using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Serialization
{
    public class DeflateSerializer
    {
        private static readonly NetSerializer.Serializer _serializer;

        /// <summary>
        /// Initializes the <see cref="DeflateSerializer"/> class.
        /// </summary>
        static DeflateSerializer()
        {
            var supportedTypes = new[]
            {
                typeof(object),
                typeof(string),
                typeof(double),
                typeof(float),
                typeof(int),
                typeof(long),
                typeof(decimal),
                typeof(Guid),
                typeof(DateTime),
                typeof(bool),
                typeof(Dictionary<string, object>),
                typeof(IDictionary<string, object>),

                typeof(List<object>),
                typeof(List<string>),
                typeof(List<double>),
                typeof(List<float>),
                typeof(List<int>),
                typeof(List<long>),
                typeof(List<decimal>),
                typeof(List<Guid>),
                typeof(List<DateTime>),
                typeof(List<bool>),
                typeof(List<Dictionary<string, object>>),
                typeof(List<IDictionary<string, object>>),

                typeof(IList<object>),
                typeof(IList<string>),
                typeof(IList<double>),
                typeof(IList<float>),
                typeof(IList<int>),
                typeof(IList<long>),
                typeof(IList<decimal>),
                typeof(IList<Guid>),
                typeof(IList<DateTime>),
                typeof(IList<bool>),
                typeof(IList<Dictionary<string, object>>),
                typeof(IList<IDictionary<string, object>>),

                typeof(object[]),
                typeof(string[]),
                typeof(double[]),
                typeof(float[]),
                typeof(int[]),
                typeof(long[]),
                typeof(decimal[]),
                typeof(Guid[]),
                typeof(DateTime[]),
                typeof(bool[]),
                typeof(Dictionary<string, object>[]),
                typeof(IDictionary<string, object>[]),

                typeof(DocumentCollectionSchema),
                typeof(DocumentCollectionSchemaField),
                typeof(List<DocumentCollectionSchemaField>),
                typeof(FieldDataType)
            };

            _serializer = new NetSerializer.Serializer(supportedTypes);
        }

        public byte[] ToCompressedByteArray(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            byte[] value = null;
            using (var memoryStream = new MemoryStream())
            {
                using (var compressionStream = new DeflateStream(memoryStream, CompressionMode.Compress))
                {
                    var dictionary = document.ToDictionary();
                    _serializer.Serialize(compressionStream, dictionary);
                }
               
                value = memoryStream.ToArray();
            }
            return value;
        }

        public byte[] ToCompressedByteArray(DocumentCollectionSchema collectionSchema)
        {
            if (collectionSchema == null)
                throw new ArgumentNullException(nameof(collectionSchema));

            byte[] value = null;
            using (var memoryStream = new MemoryStream())
            {
                using (var compressionStream = new DeflateStream(memoryStream, CompressionMode.Compress))
                {
                    _serializer.Serialize(compressionStream, collectionSchema);
                }                
                value = memoryStream.ToArray();
            }
            return value;
        }

        public Document ToDocument(byte[] value)
        {
            if (value == null)
                return null;

            Document document = null;
            using (var memoryStream = new MemoryStream(value))
            {
                using (var decompressionStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
                {
                    var dictionary = _serializer.Deserialize(decompressionStream) as IDictionary<string, object>;
                    document = new Document(dictionary);
                }               
            }

            return document;
        }

        public DocumentCollectionSchema ToDocumentCollectionSchema(byte[] value)
        {
            if (value == null)
                return null;

            DocumentCollectionSchema documentCollectionSchema = null;
            using (var memoryStream = new MemoryStream(value))
            {
                using (var decompressionStream = new DeflateStream(memoryStream, CompressionMode.Decompress))
                {
                    documentCollectionSchema = _serializer.Deserialize(decompressionStream) as DocumentCollectionSchema;
                }                
            }

            return documentCollectionSchema;
        }
    }
}
