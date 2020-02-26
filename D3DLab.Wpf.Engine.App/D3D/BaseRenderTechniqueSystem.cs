using D3DLab.ECS.Components;
using D3DLab.ECS.Filter;
using D3DLab.SDX.Engine;
using D3DLab.SDX.Engine.Components;
using D3DLab.SDX.Engine.Rendering;
using D3DLab.Std.Engine.Core.Shaders;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace D3DLab.Wpf.Engine.App.D3D {
    public abstract class NestedRenderTechniqueSystem : D3DAbstractRenderTechnique<CustomRenderProperties> {
        protected NestedRenderTechniqueSystem(EntityHasSet entityHasSet) : base(entityHasSet) {
        }

        protected static void Render(GraphicsDevice graphics, DeviceContext context, D3DRenderComponent render, int vertexSize) {

            if (render.TransformWorldBuffer.HasValue) {
                context.VertexShader.SetConstantBuffer(TransforStructBuffer.RegisterResourceSlot, render.TransformWorldBuffer.Get());
            }

            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(render.VertexBuffer.Get(), vertexSize, 0));
            context.InputAssembler.SetIndexBuffer(render.IndexBuffer.Get(), SharpDX.DXGI.Format.R32_UInt, 0);

            context.InputAssembler.InputLayout = render.Layout.Get();
            context.InputAssembler.PrimitiveTopology = render.PrimitiveTopology;
            graphics.UpdateRasterizerState(render.RasterizerState.GetDescription());

            context.OutputMerger.SetDepthStencilState(render.DepthStencilState.Get(), 0);
            var blendFactor = new SharpDX.Mathematics.Interop.RawColor4(0, 0, 0, 0);
            context.OutputMerger.SetBlendState(render.BlendingState.Get(), blendFactor, -1);
        }

        protected void UpdateTransformWorld(GraphicsDevice graphics, ID3DTransformWorldRenderComponent render, TransformComponent transform) {
            if (transform.IsModified) {
                var tr = TransforStructBuffer.ToTranspose(transform.MatrixWorld);

                if (render.TransformWorldBuffer.HasValue) {
                    var buff = render.TransformWorldBuffer.Get();
                    graphics.UpdateDynamicBuffer(ref tr, buff);
                } else {
                    var buff = graphics.CreateDynamicBuffer(ref tr, Unsafe.SizeOf<TransforStructBuffer>());
                    render.TransformWorldBuffer.Set(buff);
                }

                transform.IsModified = false;
            }
        }
    }
}
