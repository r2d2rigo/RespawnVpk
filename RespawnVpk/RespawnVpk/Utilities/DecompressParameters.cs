using System;
using System.Runtime.InteropServices;

namespace RespawnVpk.Utilities
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DecompressParameters
    {
        public UInt32 StructSize;
        public UInt32 DictSizeLog2;
        public DecompressFlags DecompressFlags;
        public UInt32 NumSeedBytes;
        public byte* SeedBytes;
    }
}
