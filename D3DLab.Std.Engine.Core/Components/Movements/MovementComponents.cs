using D3DLab.ECS;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace D3DLab.Std.Engine.Core.Components.Movements {
    public interface IMovementComponentHandler {
        void Execute(MoveCameraToTargetComponent component);
        void Execute(FollowUpTargetComponent component);
        void Execute(TranslateMovementComponent translate);
    }

    public class KeywordMovingComponent : CameraMovementComponent {
        public MovingDirection Direction { get; set; }
        public bool IsKeywordDown { get; internal set; }

        public enum MovingDirection {
            Undefined,
            MoveForward,
            MoveBackward,
            TurnLeft,
            TurnRight,
        }

        public KeywordMovingComponent() {
        }

        public override void Execute(ICameraMovementComponentHandler handler) {
            handler.Handle(this);
        }
    }

    public struct MovementData {
        public Vector2 Begin;
        public Vector2 End;
    }

    public abstract class MovementComponent : GraphicComponent {
        public abstract void Execute(IMovementComponentHandler handler);
    }
    public class TranslateMovementComponent : MovementComponent {
        public override void Execute(IMovementComponentHandler handler) {
            handler.Execute(this);
        }
    }
    public class MoveCameraToTargetComponent : MovementComponent {
        public ElementTag Target { get; set; }
        public Vector3 TargetPosition { get; set; }

        public override void Execute(IMovementComponentHandler handler) {
            handler.Execute(this);
        }
    }
}
