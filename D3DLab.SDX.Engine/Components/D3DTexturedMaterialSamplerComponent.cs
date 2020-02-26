using D3DLab.ECS.Components;
using SharpDX.Direct3D11;
using System.IO;

namespace D3DLab.SDX.Engine.Components {
    public class D3DTexturedMaterialSamplerComponent : TexturedMaterialComponent {        

        /// <summary>
        /// TODO: make wrapper as for D3DRasterizerState to allow online debugging
        /// </summary>
        public SamplerStateDescription SampleDescription { get; }

        public D3DTexturedMaterialSamplerComponent(SamplerStateDescription description, params FileInfo[] image) : base(image) {
            SampleDescription = description;
            IsModified = true;
        }
        public D3DTexturedMaterialSamplerComponent(params FileInfo[] image) : this(SamplerStateDescriptions.Default, image) { }
    }
}
