using D3DLab.ECS;

namespace D3DLab.SDX.Engine.Components {
    public interface ID3DRenderable : IGraphicComponent {
        void Update(GraphicsDevice graphics);
        void Render(GraphicsDevice graphics);
    }
}
