using LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Compression
{
    public class Lz4Compressor : IStreamCompressor
    {
        private const int LZ4_DEFAULT_BLOCK_SIZE = 4096;

        public Stream GetCompressionStream(Stream inputStream)
        {
            return new LZ4Stream(inputStream, CompressionMode.Compress, LZ4StreamFlags.HighCompression | LZ4StreamFlags.IsolateInnerStream, LZ4_DEFAULT_BLOCK_SIZE);
        }

        public Stream GetDecompressionStream(Stream inputStream)
        {
            return new LZ4Stream(inputStream, CompressionMode.Decompress, LZ4StreamFlags.IsolateInnerStream, LZ4_DEFAULT_BLOCK_SIZE);
        }
    }
}
