using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace D3DLab.ECS.Camera {
    public struct MovementData {
        public Vector2 Begin;
        public Vector2 End;
    }



    public struct CameraMovementComponent : IGraphicComponent {
        public enum MovementTypes {
            Undefined,
            Zoom,
            Rotate,

        }

        public static CameraMovementComponent CreateZoom(CameraState state, MovementData movementData, int delta, float speedValue = 1) {
            return new CameraMovementComponent {
                Tag = new ElementTag(Guid.NewGuid().ToString()),
                State = state,
                MovementData = movementData,
                Delta = delta,
                MovementType = MovementTypes.Zoom,
                IsValid = true,
                SpeedValue = speedValue
            };
        }

        public static CameraMovementComponent CreateRotate(CameraState state, MovementData movementData, float speedValue = 1) {
            return new CameraMovementComponent {
                Tag = new ElementTag(Guid.NewGuid().ToString()),
                State = state,
                MovementData = movementData,
                MovementType = MovementTypes.Rotate,
                IsValid = true,
                SpeedValue = speedValue
            };
        }





        public ElementTag Tag { get; private set; }
        public ElementTag EntityTag { get; set; }
        public bool IsModified { get; set; }
        public bool IsValid { get; private set; }
        public bool IsDisposed { get; private set; }

        //genneral
        public CameraState State;
        public MovementData MovementData;
        public MovementTypes MovementType;

        //zooming
        public int Delta;

        public float SpeedValue;

        

        public void Dispose() {
            IsDisposed = true;
        }
    }
}
