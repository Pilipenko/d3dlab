using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.SDX.Engine {
    public static class SamplerStateDescriptions {

        public readonly static SamplerStateDescription Default = new SamplerStateDescription() {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Wrap,
            AddressV = TextureAddressMode.Wrap,
            AddressW = TextureAddressMode.Wrap,
            MipLodBias = 0.0f,
            MaximumAnisotropy = 1,
            ComparisonFunction = Comparison.Always,
            BorderColor = new SharpDX.Color4(0, 0, 0, 0),
            MinimumLod = 0,
            MaximumLod = float.MaxValue
        };
    }
}
