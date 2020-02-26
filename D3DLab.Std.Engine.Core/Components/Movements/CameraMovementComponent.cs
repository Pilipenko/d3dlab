using D3DLab.ECS;
using D3DLab.ECS.Camera;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace D3DLab.Std.Engine.Core.Components.Movements {
    public interface ICameraMovementComponentHandler {
        void Handle(CameraRotatingComponent component);
        void Handle(CameraZoomingComponent component);
        void Handle(KeywordMovingComponent component);
        void Handle(CameraMoveToPositionComponent component);
    }

    public class CameraMoveToPositionComponent : CameraMovementComponent {
        public Vector3 TargetPosition { get; set; }

        public override void Execute(ICameraMovementComponentHandler handler) {
            handler.Handle(this);
        }
    }

    public class CameraRotatingComponent : CameraMovementComponent {
        public override void Execute(ICameraMovementComponentHandler handler) {
            handler.Handle(this);
        }
    }
    public class CameraZoomingComponent : CameraMovementComponent {
        public int Delta { get; set; }

        public override void Execute(ICameraMovementComponentHandler handler) {
            handler.Handle(this);
        }
    }

    public abstract class CameraMovementComponent : GraphicComponent {
        public CameraState State { get; set; }
        public MovementData MovementData { get; set; }

        public abstract void Execute(ICameraMovementComponentHandler handler);
    }
}
