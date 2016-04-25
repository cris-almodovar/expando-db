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
        private static readonly NetSerializer.Serializer _serializer;

        /// <summary>
        /// Initializes the <see cref="LightningExtensions"/> class.
        /// </summary>
        static LightningExtensions()
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
                typeof(IDictionary<string, object>[])
            };
            
            _serializer = new NetSerializer.Serializer(supportedTypes);
        }

        public static LightningKeyValue ToKeyValue(this Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var key = document._id.Value.ToByteArray();
            var value = document.ToCompressedByteArray();

            return new LightningKeyValue { Key = key, Value = value };
        }  
        
        public static Document ToDocument(this LightningKeyValue kv)
        {
            if (kv == null)
                throw new ArgumentNullException(nameof(kv));

            return kv.Value.ToDocument();
        }      

        public static byte[] ToCompressedByteArray(this Document document)
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

        public static Document ToDocument(this byte[] value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

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
    }
}
