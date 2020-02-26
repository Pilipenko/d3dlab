using System;
using System.Buffers;
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
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Shaders;
using D3DLab.Wpf.Engine.App.D3D.Components;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace D3DLab.Wpf.Engine.App.D3D.Techniques {
    [Obsolete("Not implemented", true)]
    class TerrainClipmapsRenderTechnique : NestedRenderTechniqueSystem, IRenderTechnique<CustomRenderProperties> {
        const string path = @"D3DLab.Wpf.Engine.App.D3D.Shaders.terrain_clipmaps.hlsl";
        
        static readonly D3DShaderTechniquePass pass;
        static readonly VertexLayoutConstructor layconst;

        static TerrainClipmapsRenderTechnique() {
            layconst = new VertexLayoutConstructor()
               .AddTexCoorElementAsVector2();

            var d = new CombinedShadersLoader(typeof(TerrainClipmapsRenderTechnique));

            pass = new D3DShaderTechniquePass(d.Load(path, "TRR_"));
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct TerrainClipmapsData { // Size must be in 16 bytes parts 
            public float ZScaleFactor;
            public Vector3 LightDirection;// + 16

            public Vector4 ScaleFactor;// + 16

            public Vector2 AlphaOffset;
            public Vector2 ViewerPos;// + 16

            public Vector4 FineTextureBlockOrigin;// + 16

            public Matrix4x4 WorldViewProjMatrix;// * 16

            public float OneOverWidth;
            public Vector3 __offset;// + 16

            public static readonly int Size = Unsafe.SizeOf<TerrainClipmapsData>();
            public const int SlotId = 3;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct TerrainClipmapsVertex {
            internal Vector2 texcoor;

            public static readonly int Size = Unsafe.SizeOf<TerrainClipmapsVertex>();
        }

        public TerrainClipmapsRenderTechnique()
           : base(new EntityHasSet(
               typeof(D3DTerrainRenderComponent),
               typeof(ClipmapsTerrainComponent),
               typeof(D3DTexturedMaterialSamplerComponent))
                 ) {
            rasterizerStateDescription = new RasterizerStateDescription() {
                CullMode = CullMode.Front,
                FillMode = FillMode.Solid,
                IsMultisampleEnabled = false,
                IsAntialiasedLineEnabled = false
            };
            depthStencilStateDescription = D3DDepthStencilStateDescriptions.DepthEnabled;
            blendStateDescription = D3DBlendStateDescriptions.BlendStateDisabled;
        }

        public IRenderTechniquePass GetPass() => throw new NotImplementedException();
        protected override void Rendering(GraphicsDevice graphics, CustomRenderProperties game) {
            var device = graphics.D3DDevice;
            var context = graphics.ImmediateContext;
            foreach (var en in entities) {
                var render = en.GetComponent<D3DTerrainRenderComponent>();
                var geo = en.GetComponent<TerrainGeometryCellsComponent>();
                var material = en.GetComponent<D3DTexturedMaterialSamplerComponent>();
                var transform = en.GetComponent<TransformComponent>();
                var components = en.GetComponents<ID3DRenderable>();
                var data = en.GetComponent<ClipmapsTerrainComponent>();
                var conf = en.GetComponent<Systems.TerrainConfigurationComponent>();

                foreach (var com in components) {
                    if (com.IsModified) {
                        com.Update(graphics);
                    }
                }

                if (render.IsModified || !pass.IsCompiled) {//update
                    if (!pass.IsCompiled) {
                        pass.Compile(graphics.Compilator);
                    }

                    UpdateShaders(graphics, render, pass, layconst);
                    render.PrimitiveTopology = PrimitiveTopology.TriangleList;

                    render.IsModified = false;
                }

                UpdateTransformWorld(graphics, render, transform);
                //SetShaders(context, render);

                graphics.SetVertexShader(render.VertexShader);
                graphics.SetPixelShader(render.PixelShader);

                if (material.IsModified) {
                    render.TextureResources.Set(ConvertToResources(material, graphics.TexturedLoader));
                    render.SampleState.Set(graphics.CreateSampler(material.SampleDescription));
                    material.IsModified = false;
                }


                var rd = new TerrainClipmapsData {
                    OneOverWidth = 256,//data.OneOverWidth,
                    ZScaleFactor = 1,//data.ZScaleFactor,
                    AlphaOffset = Vector2.One, //data.AlphaOffset,
                    FineTextureBlockOrigin = Vector4.One, //data.FineTextureBlockOrigin,
                    ScaleFactor = Vector4.One,//data.ScaleFactor,
                    LightDirection = -Vector3.UnitY,
                    ViewerPos = Vector2.Zero,
                    WorldViewProjMatrix = game.CameraState.ViewMatrix * game.CameraState.ProjectionMatrix
                };

                //var tr = new TransforStructBuffer();
                //var test = graphics.CreateDynamicBuffer(ref tr, Unsafe.SizeOf<TransforStructBuffer>());

                var buffData = graphics.CreateDynamicBuffer(ref rd, TerrainClipmapsData.Size);//132

                

                var resources = render.TextureResources.Get();

                //graphics.RegisterConstantBuffer(context.VertexShader,
                //    GameStructBuffer.RegisterResourceSlot, game.Game);
                //graphics.RegisterConstantBuffer(context.VertexShader,
                //    LightStructBuffer.RegisterResourceSlot, game.Lights);

               

                graphics.RegisterConstantBuffer(context.VertexShader,
                    TerrainClipmapsData.SlotId, buffData);

                var res = graphics.TexturedLoader.LoadBitmapShaderResource(conf.Texture);

                context.VertexShader.SetShaderResource(0, res);

                //

                //graphics.RegisterConstantBuffer(context.PixelShader,
                //    GameStructBuffer.RegisterResourceSlot, game.Game);
                //graphics.RegisterConstantBuffer(context.PixelShader,
                //    LightStructBuffer.RegisterResourceSlot, game.Lights);
                graphics.RegisterConstantBuffer(context.PixelShader,
                   TerrainClipmapsData.SlotId, buffData);

                context.PixelShader.SetShaderResources(0, resources);
                // context.PixelShader.SetSampler(0, render.SampleState.Get());

                if (geo.IsModified) {
                    UpdateCellBuffers(graphics, geo, render);
                    geo.MarkAsRendered();
                }

                TerrainRenderTechnique.RenderCells(graphics, CameraStateHelper.GetFrustum(game.CameraState), geo, render, transform);

            }          

        }

        void UpdateCellBuffers(GraphicsDevice graphics,
           TerrainGeometryCellsComponent geo, D3DTerrainRenderComponent render) {

            var vbuff = new SharpDX.Direct3D11.Buffer[geo.Cells.Length];
            var ibuff = new SharpDX.Direct3D11.Buffer[geo.Cells.Length];

            for (var cellIndex = 0; cellIndex < geo.Cells.Length; cellIndex++) {
                var cell = geo.Cells[cellIndex];
                var pool = ArrayPool<TerrainClipmapsVertex>.Shared;
                TerrainClipmapsVertex[] vertex = null;
                try {
                    //vertex = new TerrainVertex[pos.Length];
                    vertex = pool.Rent(cell.VertexCount);//
                    for (var i = 0; i < cell.VertexCount; i++) {
                        vertex[i] = new TerrainClipmapsVertex {
                            texcoor = cell.TextureCoordinates[i],
                        };
                    }

                    vbuff[cellIndex] = graphics.CreateBuffer(BindFlags.VertexBuffer, vertex);
                    ibuff[cellIndex] = graphics.CreateBuffer(BindFlags.IndexBuffer, cell.Indices);

                } finally {
                    pool.Return(vertex);
                }
            }
            render.VertexBuffers.Set(vbuff);
            render.IndexBuffers.Set(ibuff);
        }

    }
    class TerrainRenderTechnique : NestedRenderTechniqueSystem, IRenderTechnique<CustomRenderProperties> {
        const string path = @"D3DLab.Wpf.Engine.App.D3D.Shaders.terrain.hlsl";

        static readonly D3DShaderTechniquePass pass;
        static readonly VertexLayoutConstructor layconst;

        static TerrainRenderTechnique() {
            layconst = new VertexLayoutConstructor()
               .AddPositionElementAsVector3()
               .AddNormalElementAsVector3()
               .AddTexCoorElementAsVector2()
               .AddTexCoorElementAsVector2()
               .AddTangentElementAsVector3()
               .AddBinormalElementAsVector3()
               .AddColorElementAsVector4()
               .AddTexCoorElementAsVector4();

            var d = new CombinedShadersLoader(typeof(TerrainRenderTechnique));

            pass = new D3DShaderTechniquePass(d.Load(path, "TRR_"));
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TerrainVertex {
            internal Vector3 position;
            internal Vector3 normal;
            internal Vector2 texcoor;
            internal Vector2 normapMapTexCoor;
            internal Vector3 tangent;
            internal Vector3 binormal;
            internal Vector4 color;
            /* Vector4
             * shore
             * grass
             * rock
             * snow
             */
            internal Vector4 texWeights;


            public static readonly int Size = Unsafe.SizeOf<TerrainVertex>();
        }

        public TerrainRenderTechnique()
            : base(new EntityHasSet(
                typeof(D3DTerrainRenderComponent),
                typeof(D3DTexturedMaterialSamplerComponent))
                  ) {
            rasterizerStateDescription = new RasterizerStateDescription() {
                CullMode = CullMode.Front,
                FillMode = FillMode.Solid,
                IsMultisampleEnabled = false,
                IsAntialiasedLineEnabled = false
            };
            // disable depth because SKY dom is close to camera and with correct depth overlap all objects
            depthStencilStateDescription = D3DDepthStencilStateDescriptions.DepthEnabled;
            blendStateDescription = D3DBlendStateDescriptions.BlendStateDisabled;
        }

        public IRenderTechniquePass GetPass() => pass;

        protected override void Rendering(GraphicsDevice graphics, CustomRenderProperties game) {
            var device = graphics.D3DDevice;
            var context = graphics.ImmediateContext;

            foreach (var en in entities) {
                var render = en.GetComponent<D3DTerrainRenderComponent>();
                var geo = en.GetComponent<TerrainGeometryCellsComponent>();
                var material = en.GetComponent<D3DTexturedMaterialSamplerComponent>();
                var transform = en.GetComponent<TransformComponent>();
                var components = en.GetComponents<ID3DRenderable>();

                foreach (var com in components) {
                    if (com.IsModified) {
                        com.Update(graphics);
                    }
                }

                if (render.IsModified || !pass.IsCompiled) {//update
                    if (!pass.IsCompiled) {
                        pass.Compile(graphics.Compilator);
                    }

                    UpdateShaders(graphics, render, pass, layconst);
                    render.PrimitiveTopology = PrimitiveTopology.TriangleList;

                    render.IsModified = false;
                }

                UpdateTransformWorld(graphics, render, transform);
                SetShaders(context, render);

                if (material.IsModified) {
                    render.TextureResources.Set(ConvertToResources(material, graphics.TexturedLoader));
                    render.SampleState.Set(graphics.CreateSampler(material.SampleDescription));
                    material.IsModified = false;
                }
                //

                context.VertexShader.SetConstantBuffer(GameStructBuffer.RegisterResourceSlot, game.Game);
                context.VertexShader.SetConstantBuffer(LightStructBuffer.RegisterResourceSlot, game.Lights);

                //context.PixelShader.SetConstantBuffer(GameStructBuffer.RegisterResourceSlot, game.Game);
                //context.PixelShader.SetConstantBuffer(LightStructBuffer.RegisterResourceSlot, game.Lights);

                context.PixelShader.SetConstantBuffer(GameStructBuffer.RegisterResourceSlot, game.Game);
                context.PixelShader.SetConstantBuffer(LightStructBuffer.RegisterResourceSlot, game.Lights);
                context.PixelShader.SetShaderResources(0, render.TextureResources.Get());
                context.PixelShader.SetSampler(0, render.SampleState.Get());

                if (geo.IsModified) {
                    UpdateCellBuffers(graphics, geo, render);

                    /*
                     * <remark>https://docs.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1?view=netcore-2.2<remark>
                     */
                    /*
                   var pool = ArrayPool<TerrainVertex>.Shared;
                   TerrainVertex[] vertex = null;
                   try {
                       var pos = geo.Positions;
                       var normals = geo.Normals;
                       //vertex = new TerrainVertex[pos.Length];
                       vertex = pool.Rent(pos.Length);//
                       for (var i = 0; i < pos.Length; i++) {
                           vertex[i] = new TerrainVertex {
                               position = pos[i],
                               normal = normals[i],
                               tangent = geo.Tangents[i],
                               binormal = geo.Binormal[i],
                               color = geo.Colors[i],
                               texcoor = geo.TextureCoordinates[i],
                               normapMapTexCoor = geo.NormalMapTexCoordinates[i],
                           };
                       }

                       render.VertexBuffer.Set(graphics.CreateBuffer(BindFlags.VertexBuffer, vertex));
                       render.IndexBuffer.Set(graphics.CreateBuffer(BindFlags.IndexBuffer, geo.Indices.ToArray()));

                       geo.MarkAsRendered();
                   } finally {
                      pool.Return(vertex);
                   }
                   */
                    geo.MarkAsRendered();
                }

                RenderCells(graphics, CameraStateHelper.GetFrustum(game.CameraState), geo, render, transform);

                //Frustum frustum = null;
                //for (var i = 0; i < render.CellCount; ++i) {
                //    var cell = render.GetCell(0, frustum);

                //    Render(graphics, context, render, TerrainVertex.Size);

                //    foreach (var com in components) {
                //        com.Render(graphics);
                //    }

                //    graphics.ImmediateContext.DrawIndexed(geo.Indices.Length, 0, 0);
                ////}
            }



        }

        class ArrayPoolAdapter<T> : IDisposable {
            readonly ArrayPool<T> pool;
            readonly int count;
            T[] array;

            public T[] Array {
                get {
                    if (array == null) {
                        array = pool.Rent(count);
                    }
                    return array;
                }
            }

            public ArrayPoolAdapter(int count) {
                pool = ArrayPool<T>.Shared;
                this.count = count;
            }

            public void Dispose() {
                pool.Return(array);
            }
        }

        public static void UpdateCellBuffers(GraphicsDevice graphics,
            TerrainGeometryCellsComponent geo, D3DTerrainRenderComponent render) {

            var vbuff = new SharpDX.Direct3D11.Buffer[geo.Cells.Length];
            var ibuff = new SharpDX.Direct3D11.Buffer[geo.Cells.Length];

            for (var cellIndex = 0; cellIndex < geo.Cells.Length; cellIndex++) {
                var cell = geo.Cells[cellIndex];
                var pool = ArrayPool<TerrainVertex>.Shared;
                TerrainVertex[] vertex = null;
                try {
                    //vertex = new TerrainVertex[pos.Length];
                    vertex = pool.Rent(cell.VertexCount);//
                    for (var i = 0; i < cell.VertexCount; i++) {

                        var weight = new Vector4(
                               SharpDX.MathUtil.Clamp(1.0f -(float)Math.Abs(cell.Positions[i].Y - 0f) / 8.0f, 0f, 1f),
                               SharpDX.MathUtil.Clamp(1.0f -(float)Math.Abs(cell.Positions[i].Y - 10) / 6.0f, 0f, 1f),
                               SharpDX.MathUtil.Clamp(1.0f -(float)Math.Abs(cell.Positions[i].Y - 15) / 6.0f, 0f, 1f),
                               SharpDX.MathUtil.Clamp(1.0f -(float)Math.Abs(cell.Positions[i].Y - geo.MaxHeight) / 4.0f, 0f, 1f)
                            );
                        var total = weight.X + weight.Y + weight.Z + weight.W;
                        weight.X /= total; 
                        weight.Y /= total; 
                        weight.Z /= total;
                        weight.W /= total;

                        vertex[i] = new TerrainVertex {
                            position = cell.Positions[i],
                            normal = cell.Normals[i],
                            tangent = cell.Tangents[i],
                            binormal = cell.Binormal[i],
                            color = cell.Colors[i],
                            texcoor = cell.TextureCoordinates[i],
                            normapMapTexCoor = cell.NormalMapTexCoordinates[i],
                            texWeights = weight,
                        };
                    }

                    vbuff[cellIndex] = graphics.CreateBuffer(BindFlags.VertexBuffer, vertex);
                    ibuff[cellIndex] = graphics.CreateBuffer(BindFlags.IndexBuffer, cell.Indices);

                } finally {
                    pool.Return(vertex);
                }
            }
            render.VertexBuffers.Set(vbuff);
            render.IndexBuffers.Set(ibuff);
        }

        public static void RenderCells(GraphicsDevice graphics, Frustum frustum,
            TerrainGeometryCellsComponent geo, D3DTerrainRenderComponent render, TransformComponent transform) {
            var context = graphics.ImmediateContext;
            if (render.TransformWorldBuffer.HasValue) {
                context.VertexShader.SetConstantBuffer(TransforStructBuffer.RegisterResourceSlot, render.TransformWorldBuffer.Get());
            }

            context.InputAssembler.InputLayout = render.Layout.Get();
            context.InputAssembler.PrimitiveTopology = render.PrimitiveTopology;
            graphics.UpdateRasterizerState(render.RasterizerState.GetDescription());

            context.OutputMerger.SetDepthStencilState(render.DepthStencilState.Get(), 0);
            var blendFactor = new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 0);
            context.OutputMerger.SetBlendState(render.BlendingState.Get(), blendFactor, -1);


            for (var index = 0; index < geo.Cells.Length; ++index) {

                var cell = geo.Cells[index];
                var box = cell.Tree.GetBounds().Transform(transform.MatrixWorld);

                if (cell.Tree.IsBuilt && frustum.Contains(ref box) != Frustum.ContainmentType.Disjoint) {
                    context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(render.VertexBuffers.Get()[index], TerrainVertex.Size, 0));
                    context.InputAssembler.SetIndexBuffer(render.IndexBuffers.Get()[index], SharpDX.DXGI.Format.R32_UInt, 0);

                    graphics.ImmediateContext.DrawIndexed(cell.IndexCount, 0, 0);
                }

            }
        }
    }


}
