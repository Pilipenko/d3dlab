using D3DLab.ECS.Shaders;
using System;
using System.IO;

namespace D3DLab.SDX.Engine.Shader {
    public struct D3DShaderInfo : IShaderInfo {
        const string extention = ".hlsl";
        const string binary_extention = ".hlsl.bytes";

        public string Name { get { return compiledPath; } }

        /// <summary>
        /// Vertex/Fragment
        /// </summary>
        public string Stage { get; }
        public string EntryPoint { get; }
        readonly string path;
        readonly string compiledPath;
        byte[] compiledBytes;

        public D3DShaderInfo(string directory, string filename, string stage, string entry) {
            path = System.IO.Path.Combine(directory, filename + extention);
            compiledPath = System.IO.Path.ChangeExtension(path, binary_extention);
            Stage = stage;
            EntryPoint = entry;
            compiledBytes = null;
        }

        public byte[] ReadCompiledBytes() {
            if (compiledBytes == null) {
                compiledBytes = File.ReadAllBytes(compiledPath);
            }
            return compiledBytes;
        }

        public FileInfo GetFileInfo() {
            return new FileInfo(path);
        }

        public string ReadText() {
            return File.ReadAllText(path);
        }

        public byte[] ReadBytes() {
            return File.ReadAllBytes(path);
        }

        public void WriteCompiledBytes(byte[] bytes) {
            compiledBytes = bytes;
            File.WriteAllBytes(compiledPath, bytes);
        }

        public void WriteText(string txt) {
            File.Copy(path, path + $".back_{DateTime.Now.Ticks}");
            File.WriteAllText(path, txt);
        }
    }

   
}
