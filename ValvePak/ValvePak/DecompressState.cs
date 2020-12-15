using System;
using System.Collections.Generic;
using System.Text;

namespace ValvePak
{
    public unsafe class DecompressState
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
