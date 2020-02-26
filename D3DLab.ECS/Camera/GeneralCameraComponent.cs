using D3DLab.ECS.Camera;
using D3DLab.ECS.Ext;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace D3DLab.ECS.Components {
    [Obsolete("Better to turn it into struct")]
    public abstract class GeneralCameraComponent : GraphicComponent {
        protected bool COORDINATE_SYSTEM_LH = false;

        // A unit Vector3 designating forward in a left-handed coordinate system
        public static readonly Vector3 ForwardLH = new Vector3(0, 0, 1);
        //A unit Vector3 designating forward in a right-handed coordinate system
        public static readonly Vector3 ForwardRH = new Vector3(0, 0, -1);

        public Vector3 RotatePoint { get; set; }


        public Vector3 Position { get; set; }
        public float NearPlaneDistance { get; set; }
        public Vector3 LookDirection { get; set; }
        public Vector3 UpDirection { get; set; }
        public float FarPlaneDistance { get; set; }

        public Matrix4x4 ViewMatrix { get; protected set; }
        public Matrix4x4 ProjectionMatrix { get; protected set; }

        public Vector3 Target => Position + LookDirection;

        protected GeneralCameraComponent() {

        }

        public Matrix4x4 UpdateViewMatrix() {
            ViewMatrix = Matrix4x4.CreateLookAt(Position, Target, UpDirection);
            return ViewMatrix;
        }
        public abstract Matrix4x4 UpdateProjectionMatrix(float width, float height);

        public CameraState GetState() {
            CreateState(out var state);

            state.LookDirection = LookDirection;
            state.UpDirection = UpDirection;
            state.Position = Position;
            state.Target = Target;

            state.ViewMatrix = ViewMatrix;
            state.ProjectionMatrix = ProjectionMatrix;
            state.NearPlaneDistance = NearPlaneDistance;
            state.FarPlaneDistance = FarPlaneDistance;

            return state;
        }

        protected abstract void CreateState(out CameraState state);

        public abstract void ResetToDefault();

    }

    public class OrthographicCameraComponent : GeneralCameraComponent {
        public static OrthographicCameraComponent Clone(OrthographicCameraComponent com) {
            var copied = new OrthographicCameraComponent(com.Width, com.prevScreenHeight);
            copied.Copy(com);
            return copied;
        }

        public float Width { get; set; }
        public float Scale { get; set; }

        protected float prevScreenWidth;
        protected float prevScreenHeight;

        public OrthographicCameraComponent(float width, float height) {
            this.prevScreenWidth = width;
            this.prevScreenHeight = height;
            Scale = 1;
            ResetToDefault();
        }

        public override Matrix4x4 UpdateProjectionMatrix(float width, float height) {
            this.prevScreenWidth = width;
            this.prevScreenHeight = height;
            float aspectRatio = width / height;

            var frameWidth = Width * Scale;
            ProjectionMatrix = Matrix4x4.CreateOrthographic(
                        frameWidth,
                        frameWidth / aspectRatio,
                        NearPlaneDistance,
                        FarPlaneDistance);
            return ProjectionMatrix;
        }

        public override void ResetToDefault() {
            UpDirection = Vector3.UnitY;
            Width = 35f;
            //FieldOfViewRadians = 1.05f;
            NearPlaneDistance = 0.01f;
            LookDirection = ForwardRH;
            Position = Vector3.UnitZ * Width * 10f;

            FarPlaneDistance = Position.Length() * 50;

            Scale = 1;
        }

        public void Pan(Vector2 move) {
            var PanK = (Width * Scale) / prevScreenWidth;
            var p1 = new Vector2(move.X * PanK, move.Y * PanK);

            var left = Vector3.Cross(UpDirection, LookDirection);
            left.Normalized();

            var panVector = left * p1.X + UpDirection * p1.Y;
            Position += panVector;
        }

        static Vector3 Project(Vector3 vector, float x, float y, float width, float height, float minZ, float maxZ, Matrix4x4 worldViewProjection) {
            var v = Vector3.Transform(vector, worldViewProjection);
            return new Vector3(((1.0f + v.X) * 0.5f * width) + x, ((1.0f - v.Y) * 0.5f * height) + y, (v.Z * (maxZ - minZ)) + minZ);
        }

        static Vector3 Unproject(Vector3 vector, float x, float y, float width, float height, float minZ, float maxZ, Matrix4x4 worldViewProjection) {
            Vector3 v = new Vector3();
            Matrix4x4 matrix = new Matrix4x4();
            Matrix4x4.Invert(worldViewProjection, out matrix);

            v.X = (((vector.X - x) / width) * 2.0f) - 1.0f;
            v.Y = -((((vector.Y - y) / height) * 2.0f) - 1.0f);
            v.Z = (vector.Z - minZ) / (maxZ - minZ);

            return Vector3.Transform(v, matrix);
        }
     

        /*
public Ray GetRayFromScreenPoint(Vector2 screen) {
   var screenX = screen.X;
   var screenY = screen.Y;
   var scaleFactor = new Vector2(1, 1);
   // Normalized Device Coordinates Top-Left (-1, 1) to Bottom-Right (1, -1)
   float x = (2.0f * screenX) / (prevScreenWidth / scaleFactor.X) - 1.0f;
   float y = 1.0f - (2.0f * screenY) / (prevScreenHeight / scaleFactor.Y);
   float z = 1.0f;
   Vector3 deviceCoords = new Vector3(x, y, z);

   // Clip Coordinates
   Vector4 clipCoords = new Vector4(deviceCoords.X, deviceCoords.Y, -1.0f, 1.0f);

   // View Coordinates
   Matrix4x4.Invert(ProjectionMatrix, out Matrix4x4 invProj);
   Vector4 viewCoords = Vector4.Transform(clipCoords, invProj);
   viewCoords.Z = -1.0f;
   viewCoords.W = 0.0f;

   Matrix4x4.Invert(ViewMatrix, out Matrix4x4 invView);
   Vector3 worldCoords = Vector4.Transform(viewCoords, invView).XYZ();
   worldCoords = Vector3.Normalize(worldCoords);

   return new Ray(Position, worldCoords);
}*/

        protected override void CreateState(out CameraState state) {
            state = CameraState.OrthographicState();
        }

        public void Copy(OrthographicCameraComponent com) {
            FarPlaneDistance = com.FarPlaneDistance;
            NearPlaneDistance = com.NearPlaneDistance;
            
            RotatePoint = com.RotatePoint;
            Position = com.Position;
            UpDirection = com.UpDirection;
            LookDirection = com.LookDirection;
            
            ProjectionMatrix = com.ProjectionMatrix;
            ViewMatrix = com.ViewMatrix;

            Width = com.Width;
            Scale = com.Scale;
            prevScreenHeight = com.prevScreenHeight;
            prevScreenWidth = com.prevScreenWidth;
        }

        public bool TryChangeCameraDistance(ref float delta, ref Vector3 zoomAround, out OrthographicCameraComponent changed) {
            changed = null;

            // Handle the 'zoomAround' point
            var target = Position + LookDirection;
            var relativeTarget = zoomAround - target;
            var relativePosition = zoomAround - Position;
            if (relativePosition.Length() < 1e-4) {
                if (delta > 0) //If Zoom out from very close distance, increase the initial relativePosition
                {
                    relativePosition.Normalize();
                    relativePosition /= 10;
                } else//If Zoom in too close, stop it.
                  {
                    return false;
                }
            }
            var f = Math.Pow(2.5, delta);
            var newRelativePosition = relativePosition * (float)f;
            var newRelativeTarget = relativeTarget * (float)f;

            var newTarget = zoomAround - newRelativeTarget;
            var newPosition = zoomAround - newRelativePosition;

            var newDistance = (newPosition - zoomAround).Length();
            var oldDistance = (Position - zoomAround).Length();

            if (newDistance > FarPlaneDistance && (oldDistance < FarPlaneDistance || newDistance > oldDistance)) {
                var ratio = (newDistance - FarPlaneDistance) / newDistance;
                f *= 1 - ratio;
                newRelativePosition = relativePosition * (float)f;
                newRelativeTarget = relativeTarget * (float)f;

                newTarget = zoomAround - newRelativeTarget;
                newPosition = zoomAround - newRelativePosition;
                delta = (float)(Math.Log(f) / Math.Log(2.5));
            }

            if (newDistance < NearPlaneDistance && (oldDistance > NearPlaneDistance || newDistance < oldDistance)) {
                var ratio = (NearPlaneDistance - newDistance) / newDistance;
                f *= (1 + ratio);
                newRelativePosition = relativePosition * (float)f;
                newRelativeTarget = relativeTarget * (float)f;

                newTarget = zoomAround - newRelativeTarget;
                newPosition = zoomAround - newRelativePosition;
                delta = (float)(Math.Log(f) / Math.Log(2.5));
            }

            var newLookDirection = newTarget - newPosition;
            
            changed = new OrthographicCameraComponent(Width, prevScreenHeight);
            changed.Copy(this);
            //new data
            changed.LookDirection = newLookDirection.Normalized();
            changed.Position = newPosition;
            
            return true;
        }
    }
}
