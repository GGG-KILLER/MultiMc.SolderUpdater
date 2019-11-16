using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MultiMc.SolderUpdater
{
    public static class Hash
    {
        public static Byte[] Md5(Stream stream)
        {
            using var md5 = MD5.Create();
            return md5.ComputeHash(stream);
        }

        public static String ToHexString(Byte[] digest)
        {
            var builder = new StringBuilder(digest.Length * 2);
            for (var i = 0; i < digest.Length; i++)
                builder.AppendFormat("x2", digest[i]);
            return builder.ToString();
        }

        public static Boolean AreEqual(Byte[] digest1, Byte[] digest2)
        {
            if (digest1.Length != digest2.Length)
                return false;

            var areEqual = true;
            for (var i = 0; i < digest1.Length; i++)
            {
                areEqual &= (digest1[i] ^ digest2[i]) == 0;
            }
            return areEqual;
        }

        public static Boolean AreEqual(String digest1, String digest2)
        {
            if (digest1.Length != digest2.Length)
                return false;

            digest1 = digest1.ToUpperInvariant();
            digest2 = digest2.ToUpperInvariant();

            var areEqual = true;
            for (var i = 0; i < digest1.Length; i++)
            {
                areEqual &= (digest1[i] ^ digest2[i]) == 0;
            }
            return areEqual;
        }
    }
}