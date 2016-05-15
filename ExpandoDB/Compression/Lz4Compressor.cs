using LZ4;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Compression
{
    /// <summary>
    /// Compresses and decompresses streams using the the LZ4 algorithm.
    /// </summary>
    /// <seealso cref="ExpandoDB.Compression.IStreamCompressor" />
    public class Lz4Compressor : IStreamCompressor
    {
        private const int LZ4_DEFAULT_BLOCK_SIZE = 4096;
        private readonly static int _blockSize;

        static Lz4Compressor()
        {
            _blockSize = Int32.Parse(ConfigurationManager.AppSettings["Lz4Compressor.BlockSize"] ?? LZ4_DEFAULT_BLOCK_SIZE.ToString());
        }

        /// <summary>
        /// Compresses the specified input stream; the stream will not be closed or disposed.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns></returns>
        public Stream Compress(Stream inputStream)
        {
            return new LZ4Stream(inputStream, CompressionMode.Compress, LZ4StreamFlags.HighCompression | LZ4StreamFlags.IsolateInnerStream, _blockSize);
        }

        /// <summary>
        /// Decompresses the specified input stream; the stream will not be closed or disposed.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns></returns>
        public Stream Decompress(Stream inputStream)
        {
            return new LZ4Stream(inputStream, CompressionMode.Decompress, LZ4StreamFlags.IsolateInnerStream, _blockSize);
        }
    }
}
