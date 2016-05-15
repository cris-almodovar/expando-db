using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Compression
{
    /// <summary>
    /// Compresses and decompresses streams using the the deflate (zlib) algorithm.
    /// </summary>
    /// <seealso cref="ExpandoDB.Compression.IStreamCompressor" />
    public class DeflateCompressor : IStreamCompressor
    {

        /// <summary>
        /// Compresses the specified input stream; the stream will not be closed or disposed.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns></returns>
        public Stream Compress(Stream inputStream)
        {
            return new DeflateStream(inputStream, CompressionMode.Compress, true);
        }


        /// <summary>
        /// Decompresses the specified input stream; the stream will not be closed or disposed.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns></returns>
        public Stream Decompress(Stream inputStream)
        {
            return new DeflateStream(inputStream, CompressionMode.Decompress, true);
        }
    }
}
