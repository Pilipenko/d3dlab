using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS.Common {
    public class IncorrectBufferSizeException : Exception{
        public IncorrectBufferSizeException() :base("Buffer must have a size, that is a multiple of 16 bytes.") {

        }
    }
}
