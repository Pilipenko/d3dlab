using D3DLab.ECS;
using D3DLab.ECS.Camera;
using D3DLab.ECS.Components;
using D3DLab.ECS.Filter;
using D3DLab.ECS.Shaders;
using D3DLab.SDX.Engine.Components;
using D3DLab.SDX.Engine.D2;
using D3DLab.SDX.Engine.Shader;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System.Collections.Generic;

namespace D3DLab.SDX.Engine.Rendering {
    public interface IRenderProperties {
       CameraState CameraState { get; }
    }

    public interface IRenderTechnique<TProperties> where TProperties : IRenderProperties {
        IRenderTechniquePass GetPass();
        /// <summary>
        /// remove all entities from Technique
        /// </summary>
        void Cleanup();
        void Render(GraphicsDevice Graphics, TProperties game);
        void RegisterEntity(GraphicEntity entity);
        bool IsAplicable(GraphicEntity entity);
    }
    

    public abstract class D3DAbstractRenderTechnique<TProperties> where TProperties : IRenderProperties {
        protected readonly LinkedList<GraphicEntity> entities;
        protected RasterizerStateDescription rasterizerStateDescription;
        protected BlendStateDescription blendStateDescription;
        protected DepthStencilStateDescription depthStencilStateDescription;

        readonly EntityHasSet entityHasSet;

        protected D3DAbstractRenderTechnique(EntityHasSet entityHasSet) {
           // pass = new HashSet<IRenderTechniquePass>();
            entities = new LinkedList<GraphicEntity>();
            this.entityHasSet = entityHasSet;
        }

        public void Render(GraphicsDevice graphics, TProperties game) {
            Rendering(graphics, game);
        }
        
        public bool IsAplicable(GraphicEntity entity) {
            return entityHasSet.HasComponents(entity);
        }


        protected abstract void Rendering(GraphicsDevice graphics, TProperties game);
        
        public void RegisterEntity(GraphicEntity entity) {
            entities.AddLast(entity);
        }

        public void Cleanup() {
            entities.Clear();
        }

        protected void UpdateShaders(GraphicsDevice graphics, D3DRenderComponent render,
             D3DShaderTechniquePass pass, VertexLayoutConstructor layconst) {
            var device = graphics.D3DDevice;

            var vertexShaderByteCode = pass.VertexShader.ReadCompiledBytes();

            var inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            render.Layout.Set(new InputLayout(device, inputSignature, layconst.ConstuctElements()));

            render.VertexShader.Set(new VertexShader(device, vertexShaderByteCode));

            if (pass.GeometryShader != null) {
                render.GeometryShader.Set(new GeometryShader(device, pass.GeometryShader.ReadCompiledBytes()));
            }
            if (pass.PixelShader != null) {
                render.PixelShader.Set(new PixelShader(device, pass.PixelShader.ReadCompiledBytes()));
            }

            render.RasterizerState = new D3DRasterizerState(rasterizerStateDescription);
            render.SetStates(
                new BlendState(graphics.D3DDevice, blendStateDescription),
                new DepthStencilState(graphics.D3DDevice, depthStencilStateDescription));
        }

        protected static void SetShaders(DeviceContext context, D3DRenderComponent render) {
            context.VertexShader.Set(render.VertexShader.Get());
            context.GeometryShader.Set(render.GeometryShader.Get());
            context.PixelShader.Set(render.PixelShader.Get());
        }
        

        protected ShaderResourceView[] ConvertToResources(TexturedMaterialComponent material, TextureLoader loader) {
            var resources = new ShaderResourceView[material.Images.Length];
            for (var i = 0; i < material.Images.Length; i++) {
                var file = material.Images[i];
                resources[i] = loader.LoadShaderResource(file);
            }
            return resources;
        }
       
    }
}
