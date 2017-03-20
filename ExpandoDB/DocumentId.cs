using System;
using System.Diagnostics;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ExpandoDB
{
    /// <summary>
    /// Generates sequential GUIDs based on the MongoDB ObjectId specification.    
    /// </summary>    
    public static class DocumentId
    {
        private static readonly byte[] _machineIdBytes;
        private static readonly byte[] _processIdBytes;
        private static int _counter;

        /// <summary>
        /// Initializes the <see cref="DocumentId"/> class.
        /// </summary>
        static DocumentId()
        {
            var machineIdHash = Environment.MachineName.ComputeMd5Hash();
            _machineIdBytes = machineIdHash.Take(3).ToArray();            

            // We want the 2 least significant bytes only
            var processId = Process.GetCurrentProcess().Id;
            _processIdBytes = new byte[]
            {
                (byte)(processId >> 8),
                (byte)processId
            };

            _counter = new Random().Next();
        }

        /// <summary>
        /// Generates a new GUID based on the MongoDB ObjectID specification.
        /// </summary>
        public static Guid NewGuid()
        {
            var ticks = DateTimeOffset.UtcNow.UtcTicks;
            var counter = Interlocked.Increment(ref _counter) & 0x00ffffff;  // Increment the counter, but take only the 3 least significant bytes            
            var counterBytes = new byte[]
            {
                (byte)(counter >> 16),
                (byte)(counter >> 8),
                (byte)counter
            };            
           
            return new Guid(
                (int)(ticks >> 32),
                (short)(ticks >> 16),
                (short)ticks,
                _machineIdBytes[0],
                _machineIdBytes[1],
                _machineIdBytes[2],
                _processIdBytes[0],
                _processIdBytes[1],
                counterBytes[0],
                counterBytes[1],
                counterBytes[2]
            );            
        }        
    }
}