using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Compression
{
    /// <summary>
    /// Defines the operations implemented by a Stream Compressor.
    /// </summary>
    public interface IStreamCompressor
    {        
        /// <summary>
        /// Compresses the specified input stream; the input stream will not be closed or disposed.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns></returns>
        Stream Compress(Stream inputStream);
        /// <summary>
        /// Decompresses the specified input stream; the input stream will not be closed or disposed.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns></returns>
        Stream Decompress(Stream inputStream);
    }
}
