using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Components.Movements;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace D3DLab.Std.Engine.Core.Systems {
    public interface IStickOnHeightMapComponent : IGraphicComponent {
        Vector3 AxisUpLocal { get; }
        Vector3 AttachPointLocal { get; }
    }
    public interface IHeightMapSourceComponent : IGraphicComponent {
        Matrix4x4 GetTransfromToMap(ref Ray ray);
    }

    public class StickOnHeightMapSystem : BaseEntitySystem, IGraphicSystem, IGraphicSystemContextDependent {
        public IContextState ContextState { get; set; }

        protected override void Executing(ISceneSnapshot ss) {
            var snapshot = (SceneSnapshot)ss;
            var emanager = ContextState.GetEntityManager();
            var toProcess = new List<GraphicEntity>();
            GraphicEntity source = null;
            foreach (var entity in emanager.GetEntities()) {
                if (entity.Has<IStickOnHeightMapComponent>()) {
                    toProcess.Add(entity);
                }
                var s = entity.GetComponents<IHeightMapSourceComponent>();
                if (s.Any()) {
                    source = entity;
                }
            }

            if (source.IsNull()) {
                return;
            }

            foreach(var en in toProcess) {
                var com = en.GetComponent<IStickOnHeightMapComponent>();
                var tr = en.GetComponent<TransformComponent>();

                var rayLocal = new Ray(com.AttachPointLocal, com.AxisUpLocal);
                var rayEnWorld = rayLocal.Transformed(tr.MatrixWorld);

                //

                var sourceTr = source.GetComponent<TransformComponent>();
                var map = source.GetComponent<IHeightMapSourceComponent>();

                var rayMapLocal = rayEnWorld.Transformed(sourceTr.MatrixWorld.Inverted());
                var matrix = map.GetTransfromToMap(ref rayMapLocal);

                if (!matrix.IsIdentity) {
                    en.UpdateComponent(TransformComponent.Create(tr.MatrixWorld * matrix));

                    snapshot.Notifier.NotifyChange(tr);
                }
            }

        }
    }
}
