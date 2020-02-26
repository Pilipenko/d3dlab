using D3DLab.SDX.Engine.Rendering;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace D3DLab.SDX.Engine.Components {
    public class D3DTriangleColoredVertexRenderComponent : D3DRenderComponent {

        public static D3DTriangleColoredVertexRenderComponent AsStrip() {
            return new D3DTriangleColoredVertexRenderComponent() {
                PrimitiveTopology = PrimitiveTopology.TriangleStrip
            };
        }

        public static D3DTriangleColoredVertexRenderComponent AsTriangleListCullNone() {
            return new D3DTriangleColoredVertexRenderComponent(CullMode.None) {
                PrimitiveTopology = PrimitiveTopology.TriangleList
            };
        }
        public D3DTriangleColoredVertexRenderComponent() : this(CullMode.Front) {

        }
        public D3DTriangleColoredVertexRenderComponent(CullMode cull) : base() {
            RasterizerState = new D3DRasterizerState(new RasterizerStateDescription() {
                CullMode = cull,
                FillMode = FillMode.Solid,
                IsMultisampleEnabled = false,

                IsFrontCounterClockwise = false,
                IsScissorEnabled = false,
                IsAntialiasedLineEnabled = false,
                DepthBias = 0,
                DepthBiasClamp = .0f,
                SlopeScaledDepthBias = .0f
            });
            PrimitiveTopology = PrimitiveTopology.TriangleList;
            // LayoutConstructor = StategyStaticShaders.ColoredVertexes.GetLayoutConstructor();
        }

    }
}
