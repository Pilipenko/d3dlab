using System.Collections.Generic;
using System.Text;
using System.Numerics;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace D3DLab.SDX.Engine.Animation {
    using System.Collections.Immutable;
    using System.IO;
    using D3DLab.ECS;
    using D3DLab.ECS.Common;
    using D3DLab.ECS.Components;
    using D3DLab.ECS.Filter;
    using D3DLab.ECS.Shaders;
    using D3DLab.SDX.Engine.Components;
    using D3DLab.SDX.Engine.D2;
    using D3DLab.SDX.Engine.Rendering;
    using D3DLab.SDX.Engine.Shader;
    using D3DLab.Std.Engine.Core;
    using D3DLab.Std.Engine.Core.Animation;
    using D3DLab.Std.Engine.Core.Animation.Formats;
    using D3DLab.Std.Engine.Core.Common;
    using D3DLab.Std.Engine.Core.Components;
    using D3DLab.Std.Engine.Core.Ext;
    using D3DLab.Std.Engine.Core.Shaders;
    using D3DLab.Std.Engine.Core.Systems;
    using D3DLab.Wpf.Engine.App.D3D;
    using SharpDX.D3DCompiler;
    using SharpDX.Direct3D11;

    public class D3DAnimRenderComponent : GraphicComponent, IRenderableComponent,
        ID3DTransformWorldRenderComponent {

        readonly EnumerableDisposableSetter<List<Buffer>> vertexBuffers;
        readonly EnumerableDisposableSetter<List<Buffer>> indexBuffers;
        public EnumerableDisposableSetter<List<ShaderResourceView>> TextureViews { get; }

        public DisposableSetter<SamplerState> SamplerState { get; }
        [IgnoreDebuging]
        // The per material buffer to use so that the mesh parameters can be used
        public DisposableSetter<Buffer> PerMaterialBuffer { get; set; }
        [IgnoreDebuging]
        // The per armature constant buffer to use
        public DisposableSetter<Buffer> PerArmatureBuffer { get; set; }
        // Create the constant buffer that will
        // store our worldViewProjection matrix
        public DisposableSetter<Buffer> PerObjectBuffer { get; set; }
        [IgnoreDebuging]
        public DisposableSetter<DepthStencilState> DepthStencilState { get; private set; }
        public D3DRasterizerState RasterizerState { get; set; }
        public SharpDX.Direct3D.PrimitiveTopology PrimitiveTopology { get; set; }
        [IgnoreDebuging]
        public DisposableSetter<VertexShader> VertexShader { get; private set; }
        [IgnoreDebuging]
        public DisposableSetter<PixelShader> PixelShader { get; private set; }
        [IgnoreDebuging]
        public DisposableSetter<InputLayout> Layout { get; private set; }
        [IgnoreDebuging]
        public DisposableSetter<BlendState> BlendingState { get; private set; }

        [IgnoreDebuging]
        public DisposableSetter<Buffer> TransformWorldBuffer { get; set; }

        public bool CanRender { get; set; }

        readonly DisposeWatcher disposer;

        public D3DAnimRenderComponent() {
            disposer = new DisposeWatcher();
            vertexBuffers = new EnumerableDisposableSetter<List<Buffer>>(disposer);
            indexBuffers = new EnumerableDisposableSetter<List<Buffer>>(disposer);
            TextureViews = new EnumerableDisposableSetter<List<ShaderResourceView>>(disposer);

            PerObjectBuffer = new DisposableSetter<Buffer>(disposer);
            PerMaterialBuffer = new DisposableSetter<Buffer>(disposer);
            PerArmatureBuffer = new DisposableSetter<Buffer>(disposer);
            SamplerState = new DisposableSetter<SamplerState>(disposer);
            DepthStencilState = new DisposableSetter<DepthStencilState>();
            TransformWorldBuffer = new DisposableSetter<Buffer>();

            RasterizerState = new D3DRasterizerState(new RasterizerStateDescription() {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
            });

            VertexShader = new DisposableSetter<VertexShader>(disposer);
            PixelShader = new DisposableSetter<PixelShader>(disposer);
            Layout = new DisposableSetter<InputLayout>();
            PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
            BlendingState = new DisposableSetter<BlendState>();

            CanRender = true;
            IsModified = true;
        }

        public void ClearVertexBuffer() {
            vertexBuffers.Set(new List<Buffer>());
        }
        public void ClearIndexBuffer() {
            indexBuffers.Set(new List<Buffer>());
        }
        public Buffer AddVertexBuffer(Buffer b) {
            vertexBuffers.Get().Add(b);
            return b;
        }
        public Buffer AddIndexBuffer(Buffer b) {
            indexBuffers.Get().Add(b);
            return b;
        }
        internal Buffer GetVertexBuffers(int index) {
            return vertexBuffers.Get()[index];
        }
        internal Buffer GetIndexBuffers(int index) {
            return indexBuffers.Get()[index];
        }

        public override void Dispose() {
            disposer.Dispose();
            base.Dispose();
            CanRender = false;
        }
    }


    //BaseEntitySystem, IGraphicSystem 
    public class AminRenderTechniqueSystem  : NestedRenderTechniqueSystem, IRenderTechnique<CustomRenderProperties> {
        const string path = @"D3DLab.Wpf.Engine.App.D3D.Animation.Shaders.Animation.hlsl";

        static readonly D3DShaderTechniquePass pass;
        static readonly VertexLayoutConstructor layconst;

        static AminRenderTechniqueSystem () {
            layconst = new VertexLayoutConstructor()
               .AddSVPositionElementAsVector4()
               .AddNormalElementAsVector3()
               .AddColorElementAsVector4()
               .AddTexCoorElementAsVector2()
               .AddBlendIndicesElementAsUInt4()
               .AddBlendWeightElementAsVector4();

            var d = new CombinedShadersLoader(typeof(AminRenderTechniqueSystem ));
            pass = new D3DShaderTechniquePass(d.Load(path, "ANIM_"));
        }

        //RasterizerStateDescription rasterizerStateDescription;
        //DepthStencilStateDescription depthStencilStateDescription;
        //BlendStateDescription blendStateDescription;

        public AminRenderTechniqueSystem ()
            : base(new EntityHasSet(
               typeof(D3DAnimRenderComponent),
               typeof(CMOAnimateMeshComponent),
               typeof(MeshAnimationComponent),
               typeof(TransformComponent),
               typeof(D3DTexturedMaterialSamplerComponent))) {

            rasterizerStateDescription = new RasterizerStateDescription() {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
            };
            depthStencilStateDescription = new DepthStencilStateDescription() {
                IsDepthEnabled = true, // enable depth?
                DepthComparison = Comparison.Less,
                DepthWriteMask = SharpDX.Direct3D11.DepthWriteMask.All,
                IsStencilEnabled = false,// enable stencil?
                StencilReadMask = 0xff, // 0xff (no mask)
                StencilWriteMask = 0xff,// 0xff (no mask)
                                        // Configure FrontFace depth/stencil operations
                FrontFace = new DepthStencilOperationDescription() {
                    Comparison = Comparison.Always,
                    PassOperation = StencilOperation.Keep,
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Increment
                },
                // Configure BackFace depth/stencil operations
                BackFace = new DepthStencilOperationDescription() {
                    Comparison = Comparison.Always,
                    PassOperation = StencilOperation.Keep,
                    FailOperation = StencilOperation.Keep,
                    DepthFailOperation = StencilOperation.Decrement
                },
            };
            blendStateDescription = D3DBlendStateDescriptions.BlendStateDisabled;
        }

        public IRenderTechniquePass GetPass() => pass;
        

        static readonly ConstantBuffers.PerMaterial DefaultMaterial = new ConstantBuffers.PerMaterial {
            Ambient = new Vector4(0.2f),
            Diffuse = V4Colors.White,
            Emissive = new Vector4(0),
            Specular = V4Colors.White,
            SpecularPower = 20f,
            HasTexture = 0,
            UVTransform = Matrix4x4.Identity,
        };


        // A buffer that will be used to update the lights
        Buffer perFrameBuffer;
        //protected void Executing1(SceneSnapshot snapshot) {
        //    var emanager = snapshot.ContextState.GetEntityManager();
        //    var ticks = (float)snapshot.FrameRateTime.TotalMilliseconds;
        //    try {
        //        using (var frame = graphics.Device.FrameBegin()) {
        //            var camera = snapshot.Camera;
        //            var lights = snapshot.Lights.Select(x => x.GetStructLayoutResource()).ToArray();
        //            var gamebuff = GameStructBuffer.FromCameraState(camera);

        //            foreach (var entity in emanager.GetEntities()) {
        //                var renders = entity.GetComponents<D3DAnimRenderComponent>();
        //                if (renders.Any() && renders.All(x => x.CanRender)) {
        //                   // Rendering(graphics.Device, entity, camera, new DefaultGameBuffers(null, null));
        //                }
        //            }
        //        }
        //    } catch (Exception ex) {
        //        ex.ToString();
        //    }
        //}


        //protected override void Rendering(GraphicsDevice graphics, DefaultGameBuffers game) {
        //    throw new NotImplementedException();
        //}
        protected override void Rendering(GraphicsDevice graphics, CustomRenderProperties game) {
            var device = graphics.D3DDevice;
            var context = graphics.ImmediateContext;

            foreach (var en in entities) {
                var mesh = en.GetComponent<CMOAnimateMeshComponent>();
                var render = en.GetComponent<D3DAnimRenderComponent>();
                var animator = en.GetComponent<MeshAnimationComponent>();
                var transform = en.GetComponent<TransformComponent>();
                var texture = en.GetComponent<D3DTexturedMaterialSamplerComponent>();

                if (render.IsModified || mesh.IsModified) {
                    if (!pass.IsCompiled) {
                        pass.Compile(graphics.Compilator);
                    }

                    UpdateRenderComponent(device, render, mesh);

                    var vertexShaderByteCode = pass.VertexShader.ReadCompiledBytes();
                    try {
                        var inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                        render.Layout.Set(new InputLayout(device, inputSignature, layconst.ConstuctElements()));
                        render.VertexShader.Set(new VertexShader(device, vertexShaderByteCode));
                        render.PixelShader.Set(new PixelShader(device, pass.PixelShader.ReadCompiledBytes()));

                        render.RasterizerState = new D3DRasterizerState(rasterizerStateDescription);
                        render.BlendingState.Set(new BlendState(graphics.D3DDevice, blendStateDescription));
                        render.DepthStencilState.Set(new DepthStencilState(graphics.D3DDevice, depthStencilStateDescription));

                        perFrameBuffer?.Dispose();
                        // Create the per frame constant buffer
                        // lighting / camera position
                        perFrameBuffer = new Buffer(device, Unsafe.SizeOf<ConstantBuffers.PerFrame>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);
                    } catch (Exception ex) {
                        ex.ToString();
                    }
                    render.IsModified = false;
                    mesh.IsModified = false;
                }

                if (texture.IsModified) {
                    // Create our sampler state
                    render.SamplerState.Set(new SamplerState(device, texture.SampleDescription));
                    texture.IsModified = false;
                }

                UpdateTransformWorld(graphics, render, transform);

                //default buffers data
                //var perFrame = new ConstantBuffers.PerFrame();
                //perFrame.Light.Color = new Vector4(0.8f, 0.8f, 0.8f, 1.0f);
                //perFrame.Light.Direction = camera.LookDirection;
                //perFrame.CameraPosition = camera.Position;

                //var viewProjection = Matrix4x4.Multiply(camera.ViewMatrix, camera.ProjectionMatrix);

                //var perObject = new ConstantBuffers.PerObject();

                //perObject.World = transform.MatrixWorld;// * worldMatrix;
                //perObject.WorldInverseTranspose = Matrix4x4.Transpose(perObject.World.Inverted());
                //perObject.WorldViewProjection = perObject.World * viewProjection;
                //perObject.Transpose();
                var mat = DefaultMaterial;

                graphics.SetVertexShader(render.VertexShader);
                graphics.SetPixelShader(render.PixelShader);

                graphics.RegisterConstantBuffer(context.VertexShader, GameStructBuffer.RegisterResourceSlot, game.Game);
                graphics.RegisterConstantBuffer(context.VertexShader, TransforStructBuffer.RegisterResourceSlot, render.TransformWorldBuffer);

                // graphics.RegisterConstantBuffer(context.VertexShader, ConstantBuffers.PerObject.Slot, render.PerObjectBuffer.Get());
                // graphics.RegisterConstantBuffer(context.VertexShader, ConstantBuffers.PerFrame.Slot, perFrameBuffer);
                graphics.RegisterConstantBuffer(context.VertexShader, ConstantBuffers.PerMaterial.Slot, render.PerMaterialBuffer.Get());
                graphics.RegisterConstantBuffer(context.VertexShader, MeshAnimationComponent.Slot, render.PerArmatureBuffer.Get());


                graphics.RegisterConstantBuffer(context.PixelShader, GameStructBuffer.RegisterResourceSlot, game.Game);
                graphics.RegisterConstantBuffer(context.PixelShader, LightStructBuffer.RegisterResourceSlot, game.Lights);

                //graphics.RegisterConstantBuffer(context.PixelShader, ConstantBuffers.PerFrame.Slot, perFrameBuffer);
                graphics.RegisterConstantBuffer(context.PixelShader, ConstantBuffers.PerMaterial.Slot, render.PerMaterialBuffer.Get());

                //graphics.UpdateSubresource(ref perObject, render.PerObjectBuffer.Get());
                //graphics.UpdateSubresource(ref perFrame, perFrameBuffer);
                graphics.UpdateSubresource(ref mat, render.PerMaterialBuffer.Get());
                graphics.UpdateArraySubresource(animator.Bones, render.PerArmatureBuffer.Get());

                context.InputAssembler.InputLayout = render.Layout.Get();
                graphics.UpdateRasterizerState(render.RasterizerState.GetDescription());

                context.OutputMerger.SetDepthStencilState(render.DepthStencilState.Get());
                //var blendFactor = new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 0);
                // context.OutputMerger.SetBlendState(render.BlendingState.Get(), blendFactor, -1);

                RenderMaterial(context, render, mesh);
            }
        }

        void UpdateRenderComponent(Device device, D3DAnimRenderComponent render, CMOAnimateMeshComponent mesh) {

            render.ClearIndexBuffer();
            render.ClearVertexBuffer();
            render.TextureViews.Set(new List<ShaderResourceView>());

            try {
                // Initialize vertex buffers
                for (int indx = 0; indx < mesh.VertexBuffers.Count; indx++) {
                    var vb = mesh.VertexBuffers[indx];
                    Vertex[] vertices = new Vertex[vb.Length];
                    for (var i = 0; i < vb.Length; i++) {
                        // Retrieve skinning information for vertex
                        var skin = new SkinningVertex();
                        if (mesh.SkinningVertexBuffers.Count > 0) {
                            skin = mesh.SkinningVertexBuffers[indx][i];
                        }

                        // Create vertex
                        vertices[i] = new Vertex(vb[i].Position, vb[i].Normal, (Vector4)vb[i].Color, vb[i].UV, skin);
                    }
                    render
                        .AddVertexBuffer(Buffer.Create(device, BindFlags.VertexBuffer, vertices))
                        .DebugName = "VertexBuffer_" + indx.ToString();
                }

                // Initialize index buffers
                for (var i = 0; i < mesh.IndexBuffers.Count; i++) {
                    var ib = mesh.IndexBuffers[i];
                    render
                        .AddIndexBuffer(Buffer.Create(device, BindFlags.IndexBuffer, ib))
                        .DebugName = "IndexBuffer_" + i.ToString();
                }

                var tloader = new TextureLoader(device);
                //Load textures if a material has any.
                foreach (var mat in mesh.Materials) {
                    for (var i = 0; i < mat.Textures.Length; i++) {
                        if (System.IO.File.Exists(mat.Textures[i])) {
                            render.TextureViews.Get().Add(tloader.LoadShaderResource(new FileInfo(mat.Textures[i])));
                        } else {
                            render.TextureViews.Get().Add(null);
                        }
                    }
                }
            } catch (Exception ex) {
                ex.ToString();
            }

            render.PerMaterialBuffer.Set(new Buffer(device, Unsafe.SizeOf<ConstantBuffers.PerMaterial>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));
            render.PerObjectBuffer.Set(new Buffer(device, Unsafe.SizeOf<ConstantBuffers.PerObject>(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));
            render.PerArmatureBuffer.Set(new Buffer(device, ConstantBuffers.PerArmature.Size(), ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));
        }

        void RenderMaterial(DeviceContext context, D3DAnimRenderComponent render, CMOAnimateMeshComponent mesh) {
            var materialCount = mesh.Materials.Count;
            // If there are no materials
            if (materialCount == 0) {
                RenderMeshes(context, render, mesh.SubMeshes);
                return;
            }
            // Draw sub-meshes grouped by material
            for (var mIndx = 0; mIndx < materialCount; mIndx++) {
                // Retrieve sub meshes for this material
                var subMeshesForMaterial = mesh.SubMeshes.Where(x => x.MaterialIndex == mIndx).ToList();
                try {
                    // If the material buffer is available and there are submeshes
                    // using the material update the PerMaterialBuffer
                    if (subMeshesForMaterial.Count > 0) {
                        // update the PerMaterialBuffer constant buffer
                        var material = new ConstantBuffers.PerMaterial() {
                            Ambient = mesh.Materials[mIndx].Ambient,
                            Diffuse = mesh.Materials[mIndx].Diffuse,
                            Emissive = mesh.Materials[mIndx].Emissive,
                            Specular = mesh.Materials[mIndx].Specular,
                            SpecularPower = mesh.Materials[mIndx].SpecularPower,
                            UVTransform = mesh.Materials[mIndx].UVTransform,
                        };

                        // Bind textures to the pixel shader
                        int texIndxOffset = mIndx * CMOAnimateMeshComponent.MaxTextures;
                        material.HasTexture = (uint)(render.TextureViews.Get()[texIndxOffset] != null ? 1 : 0); // 0=false

                        context.PixelShader.SetShaderResources(0, render.TextureViews.Get().GetRange(texIndxOffset, CMOAnimateMeshComponent.MaxTextures).ToArray());
                        context.PixelShader.SetSampler(0, render.SamplerState.Get());
                        context.UpdateSubresource(ref material, render.PerMaterialBuffer.Get());
                    }
                } catch (Exception ex) {
                    ex.ToString();
                }
                // For each sub-mesh
                RenderMeshes(context, render, subMeshesForMaterial);
            }
        }

        void RenderMeshes(DeviceContext context, D3DAnimRenderComponent render, List<CMOAnimateMeshComponent.SubMesh> meshes) {
            foreach (var subMesh in meshes) {
                try {
                    // Ensure the vertex buffer and index buffers are in range
                    // if (subMesh.VertexBufferIndex < vertexBuffers.Count && subMesh.IndexBufferIndex < indexBuffers.Count) {
                    // Retrieve and set the vertex and index buffers
                    var vertexBuffer = render.GetVertexBuffers((int)subMesh.VertexBufferIndex);
                    context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Unsafe.SizeOf<Vertex>(), 0));
                    context.InputAssembler.SetIndexBuffer(render.GetIndexBuffers((int)subMesh.IndexBufferIndex),
                        SharpDX.DXGI.Format.R16_UInt, 0);
                    // Set topology
                    context.InputAssembler.PrimitiveTopology = render.PrimitiveTopology;
                    // }

                    // Draw the sub-mesh (includes Primitive count which we multiply by 3)
                    // The submesh also includes a start index into the vertex buffer
                    context.DrawIndexed((int)subMesh.PrimCount * 3, (int)subMesh.StartIndex, 0);
                } catch (Exception ex) {
                    ex.ToString();
                }
            }

        }


    }
}
