using D3DLab.ECS;
using D3DLab.ECS.Common;
using D3DLab.ECS.Components;
using D3DLab.SDX.Engine.Rendering;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;

namespace D3DLab.SDX.Engine.Components {
    public interface ID3DTransformWorldRenderComponent {
        DisposableSetter<SharpDX.Direct3D11.Buffer> TransformWorldBuffer { get; }
    }
    public abstract class D3DRenderComponent : GraphicComponent, IRenderableComponent,
        ID3DTransformWorldRenderComponent {
        public bool CanRender { get; set; }

        public D3DRasterizerState RasterizerState { get; set; }
        public PrimitiveTopology PrimitiveTopology { get; set; }

        [IgnoreDebuging]
        public DisposableSetter<SharpDX.Direct3D11.Buffer> TransformWorldBuffer { get; set; }
        [IgnoreDebuging]
        public DisposableSetter<SharpDX.Direct3D11.Buffer> VertexBuffer { get; private set; }
        [IgnoreDebuging]
        public DisposableSetter<SharpDX.Direct3D11.Buffer> IndexBuffer { get; private set; }
        [IgnoreDebuging]
        public DisposableSetter<DepthStencilState> DepthStencilState { get; private set; }
        [IgnoreDebuging]
        public DisposableSetter<BlendState> BlendingState { get; private set; }
        [IgnoreDebuging]
        public DisposableSetter<InputLayout> Layout { get; private set; }
        [IgnoreDebuging]
        public DisposableSetter<VertexShader> VertexShader { get; private set; }
        [IgnoreDebuging]
        public DisposableSetter<PixelShader> PixelShader { get; private set; }
        [IgnoreDebuging]
        public DisposableSetter<GeometryShader> GeometryShader { get; private set; }


        protected readonly DisposeWatcher disposer;
        public D3DRenderComponent() {
            CanRender = true;
            IsModified = true;
            disposer = new DisposeWatcher();
            TransformWorldBuffer = new DisposableSetter<SharpDX.Direct3D11.Buffer>(disposer);
            VertexBuffer = new DisposableSetter<SharpDX.Direct3D11.Buffer>(disposer);
            IndexBuffer = new DisposableSetter<SharpDX.Direct3D11.Buffer>(disposer);
            DepthStencilState = new DisposableSetter<DepthStencilState>(disposer);
            BlendingState = new DisposableSetter<BlendState>(disposer);
            Layout = new DisposableSetter<InputLayout>(disposer);
            VertexShader = new DisposableSetter<VertexShader>(disposer);
            PixelShader = new DisposableSetter<PixelShader>(disposer);
            GeometryShader = new DisposableSetter<GeometryShader>(disposer);
        }

        public override void Dispose() {
            Disposer.DisposeAll(
                disposer,
                TransformWorldBuffer);

            base.Dispose();
            CanRender = false;
        }


        public void SetStates(BlendState blend, DepthStencilState depth) {
            DepthStencilState.Set(depth);
            BlendingState.Set(blend);
        }

    }

}
