using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS.Shaders {
    public interface IShadersContainer {
        IRenderTechniquePass[] Pass { get; }
        IShaderCompilator GetCompilator();
    }
}
