using D3DLab.ECS;
using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.Std.Engine.Core.Systems {
    public interface IAnimationComponent : IGraphicComponent {
        void Animate(GraphicEntity owner, TimeSpan frameRateTime);
    }
    public class EmptyAnimationSystem : BaseEntitySystem, IGraphicSystem, IGraphicSystemContextDependent {
        public IContextState ContextState { get; set; }

        protected override void Executing(ISceneSnapshot ss) {
            var snapshot = (SceneSnapshot)ss;
            var emanager = ContextState.GetEntityManager();

            foreach (var entity in emanager.GetEntities()) {
                foreach(var com in entity.GetComponents<IAnimationComponent>()) {
                    com.Animate(entity, snapshot.FrameRateTime);
                }
            }
        }
    }
}
