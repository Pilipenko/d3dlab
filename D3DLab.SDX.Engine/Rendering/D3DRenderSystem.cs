using D3DLab.ECS;
using D3DLab.ECS.Camera;
using D3DLab.ECS.Components;
using D3DLab.ECS.Shaders;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace D3DLab.SDX.Engine.Rendering {
    public class RenderTechniqueRegistrator<TProperties> where TProperties : IRenderProperties {
        public IEnumerable<IRenderTechnique<TProperties>> Techniques { get { return dic.Values; } }
        readonly Dictionary<Type, IRenderTechnique<TProperties>> dic;
        readonly List<IRenderTechnique<TProperties>> allTechniques;

        public RenderTechniqueRegistrator(List<IRenderTechnique<TProperties>> techniques) {
            dic = new Dictionary<Type, IRenderTechnique<TProperties>>();
            this.allTechniques = techniques;
        }

        public void Register(GraphicEntity entity) {
            foreach (var tech in allTechniques) {
                if (tech.IsAplicable(entity)) {
                    GetOrCreate(tech).RegisterEntity(entity);
                }
            }
        }
        IRenderTechnique<TProperties> GetOrCreate(IRenderTechnique<TProperties> obj) {
            var type = obj.GetType();
            if (!dic.TryGetValue(type, out var tech)) {
                dic[type] = obj;
                tech = obj;
            }
            return tech;
        }
        public void Cleanup() {
            foreach (var s in Techniques) {
                s.Cleanup();
            }
        }
    }

    public abstract class D3DRenderSystem<TProperties> : ContainerSystem<IRenderTechnique<TProperties>>, 
        IGraphicSystem, IShadersContainer, IGraphicSystemContextDependent where TProperties : IRenderProperties{
        

        protected SynchronizedGraphics graphics;

        
        public D3DRenderSystem<TProperties> Init(SynchronizedGraphics graphics) {
            this.graphics = graphics;
            graphics.Changed += UpdateBuffers;
            UpdateBuffers(graphics.Device);
            return this;
        }

        protected abstract void UpdateBuffers(GraphicsDevice device);


        #region IShaderEditingSystem

        public IRenderTechniquePass[] Pass { get; protected set; }
        public IContextState ContextState { get; set; }

        public IShaderCompilator GetCompilator() {
            return graphics.Device.Compilator;
        }

        #endregion
    }
}
