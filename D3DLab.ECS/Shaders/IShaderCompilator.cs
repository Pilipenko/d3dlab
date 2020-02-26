using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS.Shaders {
    public interface IShaderCompilator {
        void CompileWithPreprocessing(IShaderInfo info);
        void CompileWithPreprocessing(IShaderInfo info, string text);

        void Compile(IShaderInfo info);
        void Compile(IShaderInfo info, string text);
    }
}
