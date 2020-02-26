using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Ext;

namespace D3DLab.Std.Engine.Core.Systems {
    public class XZPlaneMovementComponent : GraphicComponent {
        public TimeSpan TimeMovement = TimeSpan.Zero;
        public TimeSpan PrevTime = TimeSpan.Zero;
        public float SpeedValue = 0.01f;
        
    }

    public class ObjectMovementSystem : BaseEntitySystem, IGraphicSystem, IGraphicSystemContextDependent {
        public IContextState ContextState { get; set; }

        protected override void Executing(ISceneSnapshot ss) {
            var snapshot = (SceneSnapshot)ss;
            var emanager = ContextState.GetEntityManager();

            var time = snapshot.FrameRateTime;

            foreach (var entity in emanager.GetEntities()) {

                var has = entity.GetComponents<XZPlaneMovementComponent>();
                if (has.Any()) {
                    var com = has.Single();

                    var first = com.TimeMovement == TimeSpan.Zero;
                    
                    var delta = time - com.PrevTime;

                    com.PrevTime = time;
                    com.TimeMovement += time;
                    if (first) {
                        continue;
                    }

                    var spent = com.TimeMovement;

                    var tr = entity.GetComponent<TransformComponent>();
                    var geoLocal = entity.GetComponent<IGeometryComponent>();
                    var orientation = entity.GetComponent<OrientationComponent>();

                    var directionWorld = Vector3.TransformNormal(orientation.FrontDirectionLocal, tr.MatrixWorld);

                    var movement = Matrix4x4.Identity;
                    if (spent > TimeSpan.FromSeconds(2)) {
                        com.TimeMovement = TimeSpan.Zero;
                        var centerWorld = Vector3.Transform(geoLocal.Box.GetCenter(), tr.MatrixWorld);

                        movement = Matrix4x4.CreateRotationY(10f.ToRad(), centerWorld) ;
                        directionWorld = Vector3.TransformNormal(orientation.FrontDirectionLocal, movement);
                    }

                    //S = V • t 
                    var s = com.SpeedValue * ((float)delta.TotalMilliseconds/ 1000.0f);
                    movement *= Matrix4x4.CreateTranslation(directionWorld * s);

                    entity.UpdateComponent(TransformComponent.Create(tr.MatrixWorld * movement));

                    snapshot.Notifier.NotifyChange(tr);

                }
            }
        }

    }
}
