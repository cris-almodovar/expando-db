using ExpandoDB.Compression;
using ExpandoDB.Storage;
using LZ4;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Serialization
{
    /// <summary>
    /// Serializes/deserialize Documents to and from byte arrays, with an option to compress/decompress the data.
    /// </summary>
    public class ByteArraySerializer
    {        
        private static readonly NetSerializer.Serializer _serializer;
        private static readonly RecyclableMemoryStreamManager _memoryManager;        
        private readonly CompressionOption _compressionOption;
        private readonly IStreamCompressor _streamCompressor;

        /// <summary>
        /// Initializes the <see cref="ByteArraySerializer"/> class.
        /// </summary>
        static ByteArraySerializer()
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

                typeof(Schema),
                typeof(Schema.Field),
                typeof(List<Schema.Field>),
                typeof(IList<Schema.Field>),
                typeof(Schema.DataType)
            };

            _serializer = new NetSerializer.Serializer(supportedTypes);
            _memoryManager = new RecyclableMemoryStreamManager();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArraySerializer"/> class.
        /// </summary>
        /// <param name="compressionOption">The compression option.</param>
        public ByteArraySerializer(CompressionOption compressionOption)
        {
            _compressionOption = compressionOption;
            _streamCompressor = GetCompressor(compressionOption);
        }

        private IStreamCompressor GetCompressor(CompressionOption compressionOption)
        {
            switch(compressionOption)
            {
                case CompressionOption.LZ4:
                    return new LZ4Compressor();
                case CompressionOption.Deflate:
                    return new DeflateCompressor();
                default:
                    throw new ArgumentException($"{compressionOption} is not a valid CompressionOption");
            }
        }

        /// <summary>
        /// Serializes the specified Document to a byte array.
        /// </summary>
        /// <param name="document">The Document.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public byte[] Serialize(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            byte[] value = null;
            using (var memoryStream = _memoryManager.GetStream())
            {
                var dictionary = document.ToDictionary();

                if (_compressionOption == CompressionOption.None)
                    _serializer.Serialize(memoryStream, dictionary);
                else
                    using (var compressedStream = _streamCompressor.Compress(memoryStream)) 
                        _serializer.Serialize(compressedStream, dictionary);
                
                value = memoryStream.ToArray();
            }
            return value;
        }

        /// <summary>
        /// Deserializes the specified byte array to a Document.
        /// </summary>
        /// <param name="value">The byte array.</param>
        /// <returns></returns>
        public Document Deserialize(byte[] value)
        {
            if (value == null)
                return null;

            Document document = null;
            using (var memoryStream = _memoryManager.GetStream(null, value, 0, value.Length))
            {
                IDictionary<string, object> dictionary = null;
                if (_compressionOption == CompressionOption.None)
                    dictionary = _serializer.Deserialize(memoryStream) as IDictionary<string, object>;
                else
                    using (var decompressedStream = _streamCompressor.Decompress(memoryStream))                                        
                        dictionary = _serializer.Deserialize(decompressedStream) as IDictionary<string, object>;                   
                
                document = new Document(dictionary);
            }

            return document;
        }        
    }
}
