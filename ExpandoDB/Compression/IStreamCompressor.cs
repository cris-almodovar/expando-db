using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Compression
{
    public interface IStreamCompressor
    {
        Stream GetCompressionStream(Stream inputStream);
        Stream GetDecompressionStream(Stream inputStream);
    }
}
