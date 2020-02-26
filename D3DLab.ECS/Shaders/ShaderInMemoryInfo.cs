using System.Text;

namespace D3DLab.ECS.Shaders {
    public struct ShaderInMemoryInfo : IShaderInfo {
        string shader;
        byte[] compiledBytes;

        public string Stage { get; }
        public string EntryPoint { get; }
        public string Name { get; }

        public ShaderInMemoryInfo(string name, string shader, byte[] compiledBytes, string stage, string entry) {
            Stage = stage;
            EntryPoint = entry;
            Name = name;
            this.shader = shader;
            this.compiledBytes = compiledBytes;
        }

        public byte[] ReadBytes() {
            return Encoding.UTF8.GetBytes(shader);
        }

        public byte[] ReadCompiledBytes() {
            //ShaderCompilator compilator = new ShaderCompilator(null);
            //compilator.Compile(this, shader);
            return compiledBytes;
        }

        public string ReadText() {
            return shader;
        }
        public void WriteText(string txt) {
            shader = txt;
        }

        public void WriteCompiledBytes(byte[] bytes) {
            compiledBytes = bytes;
        }
    }
}
