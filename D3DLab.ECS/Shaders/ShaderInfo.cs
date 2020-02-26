using System;
using System.Collections.Generic;
using System.IO;

namespace D3DLab.ECS.Shaders {
    public interface IShaderInfo {
        string Stage { get; }
        string EntryPoint { get; }
        string Name { get; }

        byte[] ReadCompiledBytes();
        void WriteCompiledBytes(byte[] bytes);

        string ReadText();
        byte[] ReadBytes();
        void WriteText(string txt);
    }
}
