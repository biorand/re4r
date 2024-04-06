using System;
using System.IO;
using System.IO.Compression;
using Zstandard.Net;

namespace REE
{
    class ZSTD
    {
        public static Byte[] iCompress(Byte[] lpBuffer)
        {
            var TInputMemoryStream = new MemoryStream(lpBuffer);
            using (var TOutputMemoryStream = new MemoryStream())
            using (var TZstandardStream = new ZstandardStream(TOutputMemoryStream, CompressionMode.Compress))
            {
                TInputMemoryStream.CopyTo(TZstandardStream);
                TZstandardStream.Close();
                return TOutputMemoryStream.ToArray();
            }
        }

        public static Byte[] iDecompress(Byte[] lpSrcBuffer)
        {
            Byte[] lpDstBuffer;
            using (MemoryStream TSrcStream = new MemoryStream(lpSrcBuffer))
            {
                using (var TZstandardStream = new ZstandardStream(TSrcStream, CompressionMode.Decompress))
                using (var TDstStream = new MemoryStream())
                {
                    TZstandardStream.CopyTo(TDstStream);
                    lpDstBuffer = TDstStream.ToArray();
                }
            }
            return lpDstBuffer;
        }
    }
}
