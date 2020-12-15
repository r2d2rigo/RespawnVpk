using System;

namespace RespawnVpk.Utilities
{
    internal unsafe class DecompressState
    {
        internal DecompressState(void* decompress_state_ptr)
        {
            State = new IntPtr(decompress_state_ptr);
        }

        internal IntPtr State
        {
            get;
            private set;
        }
    }
}
