using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using D3DLab.ECS.Components;
using D3DLab.ECS.Filter;
using D3DLab.ECS.Shaders;
using D3DLab.SDX.Engine;
using D3DLab.SDX.Engine.Components;
using D3DLab.SDX.Engine.Rendering;
using D3DLab.SDX.Engine.Shader;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Components.Materials;
using D3DLab.Std.Engine.Core.Shaders;
using D3DLab.Wpf.Engine.App.D3D.Components;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace D3DLab.Wpf.Engine.App.D3D.Techniques {
    class SkyGradientColoringRenderTechnique : NestedRenderTechniqueSystem, IRenderTechnique<CustomRenderProperties> {
        const string path = @"D3DLab.Wpf.Engine.App.D3D.Shaders.sky.hlsl";

        static readonly D3DShaderTechniquePass pass;
        static readonly VertexLayoutConstructor layconst;

        [StructLayout(LayoutKind.Sequential)]
        public struct Vertex {
            public readonly Vector3 Position;
            public Vertex(Vector3 position) {
                Position = position;
            }
            public static readonly int Size = Unsafe.SizeOf<Vertex>();
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct GradientBuffer {
            public Vector4 apexColor;
            public Vector4 centerColor;
        }


        static SkyGradientColoringRenderTechnique() {
            layconst = new VertexLayoutConstructor()
               .AddPositionElementAsVector3();

            var d = new CombinedShadersLoader(typeof(SkyGradientColoringRenderTechnique));
            pass = new D3DShaderTechniquePass(d.Load(path, "SKY_"));
        }

        public SkyGradientColoringRenderTechnique()
            : base(new EntityHasSet(typeof(D3DSkyRenderComponent), typeof(TransformComponent))) {
            rasterizerStateDescription = new RasterizerStateDescription() {
                IsAntialiasedLineEnabled = false,
                CullMode = CullMode.None,
                DepthBias = 0,
                DepthBiasClamp = .0f,
                IsDepthClipEnabled = true,
                FillMode = FillMode.Solid,
                IsFrontCounterClockwise = false,
                IsMultisampleEnabled = false,
                IsScissorEnabled = false,
                SlopeScaledDepthBias = .0f
            };
            // disable depth because SKY dom is close to camera and with correct depth overlap all objects
            depthStencilStateDescription = D3DDepthStencilStateDescriptions.DepthDisabled;
            blendStateDescription = D3DBlendStateDescriptions.BlendStateDisabled;
        }

        public IRenderTechniquePass GetPass() => pass;

        protected override void Rendering(GraphicsDevice graphics, CustomRenderProperties game) {
            var device = graphics.D3DDevice;
            var context = graphics.ImmediateContext;

            foreach (var en in entities) {
                var render = en.GetComponent<D3DSkyRenderComponent>();
                var gradient = en.GetComponent<GradientMaterialComponent>();
                var geo = en.GetComponent<IGeometryComponent>();
                var transform = en.GetComponent<TransformComponent>();
                var components = en.GetComponents<ID3DRenderable>();

                foreach (var com in components) {
                    if (com.IsModified) {
                        com.Update(graphics);
                    }
                }

                if (render.IsModified) {//update
                    if (!pass.IsCompiled) {
                        pass.Compile(graphics.Compilator);
                    }

                    UpdateShaders(graphics, render, pass, layconst);
                    render.PrimitiveTopology = PrimitiveTopology.TriangleList;

                    render.IsModified = false;
                }

                UpdateTransformWorld(graphics, render,transform);

                SetShaders(context, render);

                if (gradient.IsModified) {
                    var str = new GradientBuffer {
                        apexColor = gradient.Apex,
                        centerColor = gradient.Center
                    };
                    render.GradientBuffer = graphics.CreateBuffer(BindFlags.ConstantBuffer, ref str);
                    gradient.IsModified = false;
                }

                context.PixelShader.SetConstantBuffer(0, render.GradientBuffer);
                context.VertexShader.SetConstantBuffer(GameStructBuffer.RegisterResourceSlot, game.Game);

                if (geo.IsModified) {
                    var vertex = new Vertex[geo.Positions.Length];
                    for (var i = 0; i < geo.Positions.Length; i++) {
                        vertex[i] = new Vertex(geo.Positions[i]);
                    }

                    render.VertexBuffer.Set(graphics.CreateBuffer(BindFlags.VertexBuffer, vertex));
                    render.IndexBuffer.Set(graphics.CreateBuffer(BindFlags.IndexBuffer, geo.Indices.ToArray()));

                    geo.MarkAsRendered();
                }

                Render(graphics, context, render, Vertex.Size);

                foreach (var com in components) {
                    com.Render(graphics);
                }

                graphics.ImmediateContext.DrawIndexed(geo.Indices.Length, 0, 0);
            }
        }
    }
}
