using System;

namespace RespawnVpk.Utilities
{
    internal enum DecompressFlags : UInt32
    {
        OutputUnbuffered = 1,
        ComputeAdler32 = 2,
        ReadZlibStream = 4,
    }
}
