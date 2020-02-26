using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.ECS.Ext;
using D3DLab.ECS.Input;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Components.Movements;
using D3DLab.Std.Engine.Core.Ext;
using System.Linq;
using System.Numerics;

namespace D3DLab.Std.Engine.Core.Input.Commands {


    public class CameraIdleCommand : IInputCommand {
        public InputStateData InputState { get; }
        public CameraIdleCommand() {
            //InputState = state;
        }
        public bool Execute(GraphicEntity entity) {
            var find = entity.GetComponents<GeneralCameraComponent>();
            if (!find.Any()) {
                return false;
            }

            entity.RemoveComponents<CameraMovementComponent>();

            return true;
        }
    }

    #region common

    public class ForceRenderCommand : IInputCommand {
        public InputStateData InputState { get; }
        public bool Execute(GraphicEntity entity) {
            return true;
        }
    }

    public class ToCenterWorldCommand : IInputCommand {
        public InputStateData InputState { get; }
        public ToCenterWorldCommand() {
            //InputState = state;
        }
        public bool Execute(GraphicEntity entity) {
            var find = entity.GetComponents<GeneralCameraComponent>();
            if (!find.Any()) {
                return false;
            }
            var ccom = find.First();

            ccom.ResetToDefault();
            entity.RemoveComponents<CameraMovementComponent>();

            return true;
        }
    }

    #endregion

    #region keywords

    public class KeywordsMovingCommand : IInputCommand {
        public InputStateData InputState { get; }
        
        public KeywordsMovingCommand(InputStateData state) {
            this.InputState = state;
        }
        public bool Execute(GraphicEntity entity) {
            var find = entity.GetComponents<GeneralCameraComponent>();
            if (!find.Any()) {
                return false;
            }
            var ccom = find.First();

            var type = KeywordMovingComponent.MovingDirection.Undefined;

            switch (InputState.Keyword) {
                case GeneralKeywords.W:
                    type = KeywordMovingComponent.MovingDirection.MoveForward;
                    break;
                case GeneralKeywords.S:
                    type = KeywordMovingComponent.MovingDirection.MoveBackward;
                    break;
                case GeneralKeywords.A:
                    type = KeywordMovingComponent.MovingDirection.TurnLeft;
                    break;
                case GeneralKeywords.D:
                    type = KeywordMovingComponent.MovingDirection.TurnRight;
                    break;
            }

            entity
                .GetOrCreateComponent(new KeywordMovingComponent())
                .Do(x => {
                    x.Direction = type;
                    x.IsKeywordDown = InputState.IsKeywordDown;
                });

            return true;
        }
    }

    #endregion

    #region mouse events 

    public class CameraZoomCommand : IInputCommand {
        const float scrollSpeed = 0.5f;
        public InputStateData InputState { get; }

        public CameraZoomCommand(InputStateData state) {
            this.InputState = state;
        }

        public bool Execute(GraphicEntity entity) {
            var find = entity.GetComponents<GeneralCameraComponent>();
            if (!find.Any()) {
                return false;
            }
            var ccom = find.First();
            var delta = InputState.Delta;

            var p2 = InputState.CurrentPosition;
            var data = new MovementData { End = p2 };

            entity
                .GetOrCreateComponent(new CameraZoomingComponent { State = ccom.GetState() })
                .Do(x => {
                    x.MovementData = data;
                    x.Delta = delta;
                });

            return true;
        }
    }

    public class CameraRotateCommand : IInputCommand {
        public InputStateData InputState { get; }

        public CameraRotateCommand(InputStateData state) {
            this.InputState = state;
        }

        public bool Execute(GraphicEntity entity) {
            var find = entity.GetComponents<GeneralCameraComponent>();
            if (!find.Any()) {
                return false;
            }

            var p11 = InputState.ButtonsStates[GeneralMouseButtons.Right].PointV2;
            var p2 = InputState.CurrentPosition;
            var data = new MovementData { Begin = p11, End = p2 };

            var ccom = find.First();

            entity.GetOrCreateComponent(new CameraRotatingComponent { State = ccom.GetState() })
                .MovementData = data;

            return true;
        }
    }

    public class FocusToObjectCommand : CameraCommand {
        public FocusToObjectCommand(InputStateData state) : base(state) { }
        protected override bool Executing(GraphicEntity entity, GeneralCameraComponent comp) {
            var mcomp = new Systems.CollidingWithScreenRayComponent();
            mcomp.ScreenPosition = InputState.CurrentPosition;
            entity.AddComponent(mcomp);

            return true;
        }
    }

    #endregion
    
    public class CameraPanCommand : CameraCommand {
        public CameraPanCommand(InputStateData state) : base(state) { }
        protected override bool Executing(GeneralCameraComponent comp) {
            var p1 = InputState.PrevPosition;
            var p2 = InputState.CurrentPosition;
            var move = new Vector2(p2.X - p1.X, p2.Y - p1.Y);

           // comp.Pan(move);

            return true;
        }
    }

    public abstract class CameraCommand : IInputCommand {
        public InputStateData InputState { get; protected set; }

        public CameraCommand(InputStateData state) {
            this.InputState = state;
        }
        public bool Execute(GraphicEntity entity) {
            var find = entity.GetComponents<GeneralCameraComponent>();
            if (!find.Any()) {
                return false;
            }
            var ccom = find.First();
            return Executing(entity, ccom) && Executing(ccom);

        }
        protected virtual bool Executing(GeneralCameraComponent comp) { return true; }
        protected virtual bool Executing(GraphicEntity entity, GeneralCameraComponent comp) { return true; }
    }

    public class CaptureTargetUnderMouseCameraCommand : CameraCommand {
        public CaptureTargetUnderMouseCameraCommand(InputStateData state) : base(state) {
            ScreenPosition = state.CurrentPosition;
        }

        public Vector2 ScreenPosition { get; }

        protected override bool Executing(GeneralCameraComponent comp) {
            return false;
        }
    }

    /*
    public class ZoomToObjectCommand : IInputCommand {
        readonly BoundingBox box;

        public ZoomToObjectCommand(BoundingBox box) {
            this.box = box;
        }

        public bool Execute(GraphicEntity entity) {
            var find = entity.GetComponents<CameraComponent>();
            if (!find.Any()) {
                return false;
            }
            var com = find.First();

            var x = box.SizeX();
            var y = box.SizeY();
            var ratio = x / y;
            var max = Math.Max(box.SizeX(), box.SizeY()) * 1.2f;

            com.Position = box.GetCenter() + Vector3.UnitZ * max;
            com.RotatePoint = box.GetCenter();
            com.Width = max;
            com.LookDirection = new Vector3(0, 0, -3);
            com.UpDirection = new Vector3(0, 1, 0);
            com.Scale = 1;
            // com.LookDirection = -Vector3.UnitZ * max;

            //ccom.Width *= box.SizeX() / viewport.ActualWidth;
            //var oldTarget = pcam.Position + pcam.LookDirection;
            //var distance = pcam.LookDirection.Length;
            //var newTarget = centerRay.PlaneIntersection(oldTarget, w);
            //if (newTarget != null) {
            //    orthographicCamera.LookDirection = w * distance;
            //    orthographicCamera.Position = newTarget.Value - orthographicCamera.LookDirection;
            //}

            return true;
        }
    }*/
}
