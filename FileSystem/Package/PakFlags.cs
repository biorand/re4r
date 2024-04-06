using System;

namespace REE
{
    [Flags]
    public enum PakFlags : Int64
    {
        NONE = 0,
        DEFLATE = 1,
        INFLATE = 1,
        ZSTD = 2,
    }
}
