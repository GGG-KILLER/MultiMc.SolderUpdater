using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiMc.SolderUpdater
{
    public static class Hash
    {
        [SuppressMessage ( "Security", "CA5351:Do Not Use Broken Cryptographic Algorithms", Justification = "No way around it." )]
        public static async Task<String> Md5Async ( Stream stream, Int32 bufferSize = 4 * 1024, CancellationToken cancellationToken = default )
        {
            if ( stream is null )
                throw new ArgumentNullException ( nameof ( stream ) );

            var buffer = ArrayPool<Byte>.Shared.Rent ( bufferSize );
            try
            {
                var length = 0;
                using var md5 = MD5.Create ( );
                while ( ( length = await stream.ReadAsync ( buffer.AsMemory ( 0, bufferSize ), cancellationToken )
                                               .ConfigureAwait ( false ) ) > 0 )
                {
                    md5.TransformBlock ( buffer, 0, length, null, 0 );
                }
                md5.TransformFinalBlock ( buffer, 0, 0 );

                return ByteArrayToHex ( md5.Hash );
            }
            finally
            {
                ArrayPool<Byte>.Shared.Return ( buffer );
            }
        }

        private static String ByteArrayToHex ( Byte[] array ) =>
            String.Concat ( Array.ConvertAll ( array, b => $"{b:x2}" ) );

        public static Boolean AreEqual ( String digest1, String digest2 ) =>
            digest1.Equals ( digest2, StringComparison.OrdinalIgnoreCase );
    }
}