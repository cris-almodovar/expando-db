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
                typeof(Dictionary<string, object>),
                typeof(Dictionary<string, string>),
                typeof(Dictionary<string, double>),
                typeof(Dictionary<string, float>),
                typeof(Dictionary<string, int>),
                typeof(Dictionary<string, long>),
                typeof(Dictionary<string, decimal>),
                typeof(Dictionary<string, Guid>),
                typeof(Dictionary<string, DateTime>),
                typeof(Dictionary<string, bool>),
                typeof(IDictionary<string, object>),
                typeof(IDictionary<string, string>),
                typeof(IDictionary<string, double>),
                typeof(IDictionary<string, float>),
                typeof(IDictionary<string, int>),
                typeof(IDictionary<string, long>),
                typeof(IDictionary<string, decimal>),
                typeof(IDictionary<string, Guid>),
                typeof(IDictionary<string, DateTime>),
                typeof(IDictionary<string, bool>)
            };
            
            _serializer = new NetSerializer.Serializer(supportedTypes);
        }

        public static LightningKeyValue ToKeyValue(this Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));           

            var key = Encoding.UTF8.GetBytes(document._id.ToString());
            var value = document.ToCompressedBytes();

            return new LightningKeyValue { Key = key, Value = value };
        }  
        
        public static Document ToDocument(this LightningKeyValue kv)
        {
            if (kv == null)
                throw new ArgumentNullException(nameof(kv));

            return kv.Value.ToDocument();
        }      

        public static byte[] ToCompressedBytes(this Document document)
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
