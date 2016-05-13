using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Compression
{
    public class DeflateCompressor : IStreamCompressor
    {        
        public Stream GetCompressionStream(Stream inputStream)
        {
            return new DeflateStream(inputStream, CompressionMode.Compress, true);
        }

        public Stream GetDecompressionStream(Stream inputStream)
        {
            return new DeflateStream(inputStream, CompressionMode.Decompress, true);
        }
    }
}
