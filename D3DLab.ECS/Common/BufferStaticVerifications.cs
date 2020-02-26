using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace D3DLab.ECS.Common {
    public static class BufferStaticVerifications {
        public static void CheckSizeInBytes(int sizeInBytes) {
            if ((sizeInBytes % 16) != 0) {
                throw new IncorrectBufferSizeException();
            }
        }

        public unsafe static void CheckSizeInBytes<T>() {
            CheckSizeInBytes(Unsafe.SizeOf<T>());
        }
    }
}
