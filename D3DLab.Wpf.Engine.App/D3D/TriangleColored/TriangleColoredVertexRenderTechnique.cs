using D3DLab.ECS.Components;
using D3DLab.ECS.Filter;
using D3DLab.ECS.Shaders;
using D3DLab.SDX.Engine.Components;
using D3DLab.SDX.Engine.Shader;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Shaders;
using D3DLab.Wpf.Engine.App.D3D;
using SharpDX.Direct3D11;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace D3DLab.SDX.Engine.Rendering {
    public class TriangleColoredVertexRenderTechnique : NestedRenderTechniqueSystem, IRenderTechnique<CustomRenderProperties> {
        const string path = @"D3DLab.Wpf.Engine.App.D3D.TriangleColored.colored_vertex.hlsl";

        static readonly D3DShaderTechniquePass pass;
        static readonly VertexLayoutConstructor layconst;

        [StructLayout(LayoutKind.Sequential)]
        public struct Vertex {
            public readonly Vector3 Position;
            public readonly Vector3 Normal;
            public readonly Vector4 Color;
            public Vertex(Vector3 position, Vector3 normal, Vector4 color) {
                Position = position;
                Normal = normal;
                Color = color;
            }
            public static readonly int Size = Unsafe.SizeOf<Vertex>();
        }

        static TriangleColoredVertexRenderTechnique() {
            layconst = new VertexLayoutConstructor()
               .AddPositionElementAsVector3()
               .AddNormalElementAsVector3()
               .AddColorElementAsVector4();

            var d = new CombinedShadersLoader(typeof(TriangleColoredVertexRenderTechnique));
            pass = new D3DShaderTechniquePass(d.Load(path, "CV_"));
        }

        public IRenderTechniquePass GetPass() => pass;

        public TriangleColoredVertexRenderTechnique()
            : base(new EntityHasSet(
                typeof(D3DTriangleColoredVertexRenderComponent),
                typeof(TransformComponent))) {
            rasterizerStateDescription = new RasterizerStateDescription() {
                CullMode = CullMode.Front,
                FillMode = FillMode.Solid,
                IsMultisampleEnabled = false,
                IsAntialiasedLineEnabled = false
            };
            depthStencilStateDescription = D3DDepthStencilStateDescriptions.DepthEnabled;
            blendStateDescription = D3DBlendStateDescriptions.BlendStateDisabled;
        }

        protected override void Rendering(GraphicsDevice graphics, CustomRenderProperties game) {
            var device = graphics.D3DDevice;
            var context = graphics.ImmediateContext;

            foreach (var en in entities) {
                var render = en.GetComponent<D3DTriangleColoredVertexRenderComponent>();
                var geo = en.GetComponent<IGeometryComponent>();
                var components = en.GetComponents<ID3DRenderable>();
                var color = en.GetComponents<ColorComponent>();
                var transform = en.GetComponent<TransformComponent>();

                foreach (var com in components) {
                    if (com.IsModified) {
                        com.Update(graphics);
                    }
                }

                if (render.IsModified || !pass.IsCompiled) {
                    if (!pass.IsCompiled) {
                        pass.Compile(graphics.Compilator);
                    }

                    UpdateShaders(graphics, render, pass, layconst);
                    render.IsModified = false;
                }

                UpdateTransformWorld(graphics, render, transform);

                SetShaders(context, render);

                context.VertexShader.SetConstantBuffer(GameStructBuffer.RegisterResourceSlot, game.Game);
                context.VertexShader.SetConstantBuffer(LightStructBuffer.RegisterResourceSlot, game.Lights);

                if (geo.IsModified || color.Any(x=>x.IsModified)) {


                    //Tests.BoundingBoxTest(geo.Positions.ToArray());

                    var vertex = new Vertex[geo.Positions.Length];
                    if (color.Any()) {
                        var c = color.Single();
                        for (var index = 0; index < vertex.Length; index++) {
                            vertex[index] = new Vertex(
                                geo.Positions[index], geo.Normals[index], c.Color);
                        }
                    } else {
                        for (var index = 0; index < vertex.Length; index++) {
                            vertex[index] = new Vertex(
                                geo.Positions[index], geo.Normals[index], geo.Colors[index]);
                        }
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
