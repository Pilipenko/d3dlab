using System;

namespace Veldrid.MetalBindings
{
    public struct MTLComputePipelineState
    {
        public readonly IntPtr NativePtr;
        public MTLComputePipelineState(IntPtr ptr) => NativePtr = ptr;
    }
}