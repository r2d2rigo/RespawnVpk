using System;
using System.Runtime.InteropServices;

namespace ValvePak
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct DecompressParameters
    {
        public UInt32 StructSize;
        public UInt32 DictSizeLog2;
        public DecompressFlags DecompressFlags;
        public UInt32 NumSeedBytes;
        public byte* SeedBytes;
    }
}
