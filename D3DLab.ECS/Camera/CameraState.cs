using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace D3DLab.ECS.Camera {
    public struct CameraState {
        public static CameraState OrthographicState() {
            return new CameraState(CameraTypes.Orthographic);
        }
        public static CameraState PerspectiveState() {
            return new CameraState(CameraTypes.Perspective);
        }

        public enum CameraTypes {
            Perspective,
            Orthographic
        }

        public Vector3 UpDirection;
        public Vector3 LookDirection;
        public Vector3 Position;
        public Vector3 Target;

        public Matrix4x4 ProjectionMatrix;
        public Matrix4x4 ViewMatrix;

        public float NearPlaneDistance;
        public float FarPlaneDistance;

        public readonly CameraTypes Type;

        public CameraState(CameraTypes type) : this() {
            Type = type;
        }
    }
}
