using System;
using System.Runtime.InteropServices;

namespace ValvePak
{
    public static class Lzham
    {
        [DllImport("lzham_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe void* lzham_decompress_init(DecompressParameters pParams);

        [DllImport("lzham_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe UInt32 lzham_decompress_deinit(void* state);

        [DllImport("lzham_x64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe UInt32 lzham_decompress_memory(DecompressParameters pParams, byte* pDst_buf, ref UIntPtr pDst_len, byte* pSrc_buf, UIntPtr src_len, ref UInt32 pAdler32);

        public static unsafe DecompressState DecompressInit(DecompressParameters parameters)
        {
            return new DecompressState(lzham_decompress_init(parameters));
        }

        public static unsafe DecompressStatus DecompressDeinit(DecompressState state)
        {
            return (DecompressStatus)lzham_decompress_deinit(state.State.ToPointer());
        }

        public static unsafe DecompressStatus DecompressMemory(DecompressParameters parameters, Span<byte> source, ref Span<byte> destination)
        {
            parameters.StructSize = (UInt32)sizeof(DecompressParameters);

            fixed (byte* pSource = source,
                pDestination = destination)
            {
                var sourceLength = new UIntPtr((UInt32)source.Length);
                var destinationLength = new UIntPtr((UInt32)destination.Length);
                UInt32 adler32 = 0;

                return (DecompressStatus)lzham_decompress_memory(parameters, pDestination, ref destinationLength, pSource, sourceLength, ref adler32);
            }
        }
    }
}
