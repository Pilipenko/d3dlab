using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.ECS.Input;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Components.Movements;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Input.Commands;
using D3DLab.Std.Engine.Core.Utilities;
using g3;

namespace D3DLab.Std.Engine.Core.Systems {
    public class ManipulatableComponent : GraphicComponent {

    }
    public class CapturedToManipulateComponent : GraphicComponent {
        public Vector3 CapturePointWorld { get; set; }
    }

    public struct TemporaryManipulateTransformKepperComponent : IGraphicComponent {

        public static TemporaryManipulateTransformKepperComponent Create(TransformComponent original) {
            return new TemporaryManipulateTransformKepperComponent {
                Tag = new ElementTag(Guid.NewGuid().ToString()),
                Original = original,
                temporary = original.MatrixWorld,

                IsModified = true,
                IsValid = true,
            };
        }

        public static TemporaryManipulateTransformKepperComponent Create(Matrix4x4 temporary, TransformComponent original) {
            return new TemporaryManipulateTransformKepperComponent {
                Tag = new ElementTag(Guid.NewGuid().ToString()),
                Original = original,
                temporary = temporary,

                IsModified = true,
                IsValid = true,
            };
        }

        public TransformComponent Original { get; private set; }
        public Matrix4x4 MatrixWorld {
            set {
                temporary = value;
            }
            get => temporary;
        }

        Matrix4x4 temporary;
       

        public ElementTag Tag { get; private set; }
        public ElementTag EntityTag { get; set; }
        public bool IsModified { get; set; }
        public bool IsValid { get; private set; }
        public bool IsDisposed { get; private set; }

        public TransformComponent Apply() {
            //original.MatrixWorld *= temporary;
            return Original;
        }

        public void Dispose() {
            IsDisposed = true;
        }
    }


    public class MovementSystem : BaseEntitySystem, IGraphicSystem, IGraphicSystemContextDependent {
        public IContextState ContextState { get; set; }

        protected override void Executing(ISceneSnapshot ss) {
            var snapshot = (SceneSnapshot)ss;
            var emanager = ContextState.GetEntityManager();

            //foreach (var ev in snapshot.Snapshot.Events) {
            //    switch (ev) {
            //        case UnTargetUnderMouseCameraCommand ut:
            //            untarget = true;
            //            snapshot.Snapshot.RemoveEvent(ev);
            //            break;
            //    }
            //}

            foreach (var entity in emanager.GetEntities()) {
                foreach (var com in entity.GetComponents()) {
                    switch (com) {
                        case MovementComponent move:
                            move.Execute(new Handlers(entity, snapshot, ContextState));
                            break;

                        case CapturedToManipulateComponent capture:
                            var istate = snapshot.InputSnapshot.CurrentInputState;
                            var isManiputating = entity.GetComponents<TemporaryManipulateTransformKepperComponent>();
                            var left = istate.ButtonsStates[GeneralMouseButtons.Left];
                            
                            if (left.Condition == ButtonStates.Released) {
                                entity.RemoveComponent(capture);
                                //apply transformation
                                var origin = isManiputating.Single().Apply();
                                entity.RemoveComponents<TemporaryManipulateTransformKepperComponent>();
                                entity.AddComponent(origin);
                                origin.IsModified = true;
                                continue;
                            }

                            if (!isManiputating.Any()) {//start manipulate
                                var orig = entity.GetComponent<TransformComponent>();
                                var temp = TemporaryManipulateTransformKepperComponent.Create(orig);
                                entity.RemoveComponent(orig);
                                entity.AddComponent(temp);
                                isManiputating = new[] { temp };
                            }

                            var transform = isManiputating.Single();
                            var movable = entity.GetComponent<ManipulatableComponent>();

                            var cstate = snapshot.Camera;

                            var begin = left.PointV2;
                            var end = istate.CurrentPosition;
                            var delta = (begin - end).Length();
                            var captutedPointW = capture.CapturePointWorld;

                            var rayW = snapshot.Viewport.UnProject(end, snapshot.Camera, snapshot.Window);
                            var vectorToMovePoint = rayW.Origin - captutedPointW;
                            
                            var endPoint = rayW.IntersecWithPlane(captutedPointW, -cstate.LookDirection);

                            var moveVector = endPoint - captutedPointW;
                           // moveVector.Normalize();

                            transform.MatrixWorld = Matrix4x4.CreateTranslation(moveVector);

                            entity.UpdateComponent(transform);

                            //System.Diagnostics.Trace.WriteLine($" {captutedPointW} | {endPoint} | {moveVector}");

                            snapshot.Notifier.NotifyChange(transform);

                            break;
                    }
                }
            }
        }

        class Handlers : IMovementComponentHandler {
            readonly IEntityManager emanager;
            readonly GraphicEntity entity;
            readonly SceneSnapshot snapshot;

            public Handlers(GraphicEntity entity, SceneSnapshot snapshot, IContextState context) {
                this.emanager = context.GetEntityManager();
                this.entity = entity;
                this.snapshot = snapshot;
            }

            public void Execute(MoveCameraToTargetComponent component) {
                //detect that camera should be moved to new position

                //created camera movement component
                var movecom = new CameraMoveToPositionComponent { TargetPosition = component.TargetPosition };

                //just put com to camera entity
                //camera moving is job for Camera system
                emanager
                    .GetEntity(snapshot.CurrentCameraTag)
                    .AddComponent(movecom);

                entity.RemoveComponent(component);
            }

            public void Execute(FollowUpTargetComponent follower) {
                var target = emanager.GetEntity(follower.IsTarget).First();
                follower.Follow(entity, target);
            }

            public void Execute(TranslateMovementComponent translate) {

            }

        }
    }
}
