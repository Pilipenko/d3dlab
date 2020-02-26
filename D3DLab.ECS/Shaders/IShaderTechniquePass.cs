namespace D3DLab.ECS.Shaders {
    public interface IRenderTechniquePass {
        bool IsCompiled { get; }

        IShaderInfo VertexShader { get; }
        IShaderInfo GeometryShader { get; }
        IShaderInfo PixelShader { get; }

        IShaderInfo[] ShaderInfos { get; }
        void ClearCache();
        void Compile(IShaderCompilator compilator);
    }
    public interface IRenderTechnique {
        IRenderTechniquePass[] Passes { get; }
    }

}
