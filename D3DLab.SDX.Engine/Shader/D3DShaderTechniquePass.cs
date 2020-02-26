using D3DLab.ECS.Shaders;
using System;
using System.Collections.Generic;
using System.IO;

namespace D3DLab.SDX.Engine.Shader {
    /// <summary>
    /// Class just describe shader technique structure, no spetific actions or behaviours just for readability
    /// </summary>
    public class D3DShaderTechniquePass : IRenderTechniquePass {
        public IShaderInfo VertexShader { get => Get(ShaderStages.Vertex); }
        public IShaderInfo GeometryShader { get => Get(ShaderStages.Geometry); }
        public IShaderInfo PixelShader { get => Get(ShaderStages.Fragment); }

        public IShaderInfo[] ShaderInfos { get; }

        public bool IsCompiled { get; private set; }

        readonly Dictionary<ShaderStages, IShaderInfo> shaders;

        public D3DShaderTechniquePass(IShaderInfo[] shaderInfos) {
            this.ShaderInfos = shaderInfos;
            shaders = new Dictionary<ShaderStages, IShaderInfo>();
            foreach (var info in shaderInfos) {
                var stage = (ShaderStages)Enum.Parse(typeof(ShaderStages), info.Stage, true);
                if (shaders.ContainsKey(stage)) {
                    throw new Exception($"One pass can contain only one {stage} shader");
                }
                shaders.Add(stage, info);
            }
        }

        IShaderInfo Get(ShaderStages stage) {
            return shaders.ContainsKey(stage) ? shaders[stage] : null;
        }

        public void ClearCache() {
            IsCompiled = false;
        }

        public void Compile(IShaderCompilator compilator) {
            foreach(var sh in shaders) {
                compilator.CompileWithPreprocessing(sh.Value);
            }
            IsCompiled = true;
        }

        public void ActivateDebugMode(DirectoryInfo dir) {
            if (!dir.Exists) {
                dir.Create();
            }
            var watcher = new FileSystemWatcher(dir.FullName, "*.hlsl");
            watcher.Changed += OmDirectory_Changed;
            foreach(var info in ShaderInfos) {
                File.WriteAllText(Path.Combine(dir.FullName, $"{info.Name}_{info.Stage}_shader.hlsl"), info.ReadText());
            }
        }

        private void OmDirectory_Changed(object sender, FileSystemEventArgs e) {
            foreach(var info in ShaderInfos) {
                var key = $"{info.Name}_{info.Stage}";
                if (e.Name.Contains(key)) {
                    info.WriteText(File.ReadAllText(e.FullPath));
                    ClearCache();
                }
            }
        }   
    }
}
