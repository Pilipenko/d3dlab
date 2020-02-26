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
using D3DLab.Std.Engine.Core.Shaders;
using D3DLab.Wpf.Engine.App.D3D.Components;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace D3DLab.Wpf.Engine.App.D3D.Techniques {
    class SkyPlaneWithParallaxRenderTechnique : NestedRenderTechniqueSystem, IRenderTechnique<CustomRenderProperties> {

        const string path = @"D3DLab.Wpf.Engine.App.D3D.Shaders.skyplane.hlsl";

        static readonly D3DShaderTechniquePass pass;
        static readonly VertexLayoutConstructor layconst;

        [StructLayout(LayoutKind.Sequential)]
        public struct Vertex {
            public readonly Vector3 Position;
            public readonly Vector2 Tex;
            public Vertex(Vector3 position, Vector2 tex) {
                Position = position;
                Tex = tex;
            }
            public static readonly int Size = Unsafe.SizeOf<Vertex>();
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ParallaxAnimationParams {
            internal Vector2 firstTranslation;
            internal Vector2 secondTranslation;
            internal float brightness;
            internal Vector3 padding;
            public static readonly int Size = Unsafe.SizeOf<ParallaxAnimationParams>();
        }

        static SkyPlaneWithParallaxRenderTechnique() {
            layconst = new VertexLayoutConstructor()
               .AddPositionElementAsVector3()
               .AddTexCoorElementAsVector2();

            var d = new CombinedShadersLoader(typeof(SkyPlaneWithParallaxRenderTechnique));
            pass = new D3DShaderTechniquePass(d.Load(path, "SKYPL_"));
        }

        public SkyPlaneWithParallaxRenderTechnique() 
            : base(new EntityHasSet(
                typeof(D3DSkyPlaneRenderComponent),
                typeof(SkyPlaneParallaxAnimationComponent),
                typeof(D3DTexturedMaterialSamplerComponent),
                typeof(TransformComponent))){

            rasterizerStateDescription = new RasterizerStateDescription() {
                IsAntialiasedLineEnabled = false,
                CullMode = CullMode.None, //Back
                DepthBias = 0,
                DepthBiasClamp = .0f,
                IsDepthClipEnabled = true,
                FillMode = FillMode.Solid,
                IsFrontCounterClockwise = false,
                IsMultisampleEnabled = false,
                IsScissorEnabled = false,
                SlopeScaledDepthBias = .0f
            };
            // Enable additive blending so the clouds blend with the sky dome color.
            blendStateDescription = new BlendStateDescription();
            blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
            blendStateDescription.RenderTarget[0].SourceBlend = BlendOption.One;
            blendStateDescription.RenderTarget[0].DestinationBlend = BlendOption.One;
            blendStateDescription.RenderTarget[0].BlendOperation = BlendOperation.Add;
            blendStateDescription.RenderTarget[0].SourceAlphaBlend = BlendOption.One;
            blendStateDescription.RenderTarget[0].DestinationAlphaBlend = BlendOption.Zero;
            blendStateDescription.RenderTarget[0].AlphaBlendOperation = BlendOperation.Add;
            blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;

            depthStencilStateDescription = D3DDepthStencilStateDescriptions.DepthDisabled;
        }

        public IRenderTechniquePass GetPass() => pass;

        protected override void Rendering(GraphicsDevice graphics, CustomRenderProperties game) {
            var device = graphics.D3DDevice;
            var context = graphics.ImmediateContext;

            foreach (var en in entities) {
                var render = en.GetComponent<D3DSkyPlaneRenderComponent>();
                var geo = en.GetComponent<IGeometryComponent>();
                var animation = en.GetComponent<SkyPlaneParallaxAnimationComponent>();
                var material = en.GetComponent<D3DTexturedMaterialSamplerComponent>();
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

                UpdateTransformWorld(graphics, render, transform);

                SetShaders(context, render);

                context.VertexShader.SetConstantBuffer(GameStructBuffer.RegisterResourceSlot, game.Game);

                //update animate buff
                if (animation.IsModified) {
                    var ani = new ParallaxAnimationParams {
                        firstTranslation = animation.translate1,
                        secondTranslation = animation.translate2,
                        brightness = 0.5f,
                        padding = Vector3.One
                    };
                    if (render.ParallaxAnimation == null) {
                        render.ParallaxAnimation = graphics.CreateDynamicBuffer(ref ani, ParallaxAnimationParams.Size);
                    } else {
                        graphics.UpdateDynamicBuffer(ref ani, render.ParallaxAnimation);
                    }
                    animation.IsModified = false;
                }
                context.PixelShader.SetConstantBuffer(0, render.ParallaxAnimation);

                {// material
                    if (material.IsModified) {
                        render.TextureResources.Set(ConvertToResources(material, graphics.TexturedLoader));
                        render.SampleState.Set(graphics.CreateSampler(material.SampleDescription));
                    }
                    context.PixelShader.SetShaderResources(0, render.TextureResources.Get());
                    context.PixelShader.SetSampler(0, render.SampleState.Get());
                }

                if (geo.IsModified) {
                    var vertex = new Vertex[geo.Positions.Length];
                    for (var i = 0; i < geo.Positions.Length; i++) {
                        vertex[i] = new Vertex(geo.Positions[i], geo.TextureCoordinates[i]);
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
