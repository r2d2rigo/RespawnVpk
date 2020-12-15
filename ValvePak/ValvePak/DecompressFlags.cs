using System;

namespace ValvePak
{
    public enum DecompressFlags : UInt32
    {
        OutputUnbuffered = 1,
        ComputeAdler32 = 2,
        ReadZlibStream = 4,
    }
}
