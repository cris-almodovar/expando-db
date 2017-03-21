using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
            var machineNameHash = 0x00ffffff & Environment.MachineName.GetHashCode();            
            var machineId = (machineNameHash + AppDomain.CurrentDomain.Id) & 0x00ffffff;

            _machineIdBytes = new byte[]
                {
                    (byte)(machineId >> 16),
                    (byte)(machineId >> 8),
                    (byte)machineId,
                };

            
            var processId = 0;
            try
            {
                processId = GetCurrentProcessId();
            }
            catch (SecurityException) { }

            // We want the 2 least significant bytes only
            _processIdBytes = new byte[]
            {
                (byte)(processId >> 8),
                (byte)processId
            };

            _counter = new Random().Next();
        }
       

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int GetCurrentProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }

        /// <summary>
        /// Generates a new GUID based on the MongoDB ObjectID specification.
        /// </summary>
        public static Guid NewGuid()
        {
            var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 0x0db0000000000000;
            var counter = Interlocked.Increment(ref _counter) & 0x00ffffff;  // Increment the counter, but take only the 3 least significant bytes            
            var counterBytes = new byte[]
            {
                (byte)(counter >> 16),
                (byte)(counter >> 8),
                (byte)counter
            };    
           
            return new Guid(
                (int)(unixTime >> 32),
                (short)(unixTime >> 16),
                (short)unixTime,
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