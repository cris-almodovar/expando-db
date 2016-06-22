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
using Wire;

namespace ExpandoDB.Serialization
{
    /// <summary>
    /// Serializes/deserialize Documents to and from byte arrays, with an option to compress/decompress the data.
    /// </summary>
    public class ByteArraySerializer
    {
        private readonly Serializer _serializer;
        private readonly RecyclableMemoryStreamManager _memoryManager;
        private readonly CompressionOption _compressionOption;
        private readonly IStreamCompressor _streamCompressor;       

        /// <summary>
        /// Initializes a new instance of the <see cref="ByteArraySerializer"/> class.
        /// </summary>
        /// <param name="compressionOption">The compression option.</param>
        public ByteArraySerializer(CompressionOption compressionOption)
        {            
            _serializer = new Serializer();
            _memoryManager = new RecyclableMemoryStreamManager();

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
                    _serializer.Serialize(dictionary, memoryStream);                    
                else
                    using (var compressedStream = _streamCompressor.Compress(memoryStream))
                        _serializer.Serialize(dictionary, compressedStream);                        

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
                    dictionary = _serializer.Deserialize<Dictionary<string, object>>(memoryStream);
                else
                    using (var decompressedStream = _streamCompressor.Decompress(memoryStream))
                        dictionary = _serializer.Deserialize<Dictionary<string, object>>(decompressedStream);

                dictionary.ConvertDatesToUtc();
                document = new Document(dictionary);                
            }

            return document;
        }        
    }
}
