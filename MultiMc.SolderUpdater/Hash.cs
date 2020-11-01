using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MultiMc.SolderUpdater
{
    public static class Hash
    {
        public static Byte[] Md5 ( Stream stream )
        {
            using var md5 = MD5.Create ( );
            return md5.ComputeHash ( stream );
        }

        public static String ToHexString ( Byte[] digest )
        {
            var builder = new StringBuilder ( digest.Length * 2 );
            for ( var i = 0; i < digest.Length; i++ )
                builder.AppendFormat ( "x2", digest[i] );
            return builder.ToString ( );
        }

        public static Boolean AreEqual ( String digest1, String digest2 ) =>
            digest1.Equals ( digest2, StringComparison.OrdinalIgnoreCase );
    }
}