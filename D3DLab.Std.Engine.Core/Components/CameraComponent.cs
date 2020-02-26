using D3DLab.ECS;
using D3DLab.ECS.Camera;
using D3DLab.ECS.Components;
using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Utilities;
using System.Numerics;

namespace D3DLab.Std.Engine.Core.Components {
    public static class CameraStateHelper {

        public static Ray Point2DtoPoint3D(CameraState state, Vector2 pointIn) {
            var pointNear = new Vector3();
            var pointFar = new Vector3();

            var pointIn3D = new Vector3(pointIn.X, pointIn.Y, 0);
            var view = state.ViewMatrix;
            var proj = state.ProjectionMatrix;

            Matrix4x4.Invert(view, out view);
            Matrix4x4.Invert(proj, out proj);

            var pointNormalized = Vector3.Transform(pointIn3D, proj);
            pointNormalized.Z = state.NearPlaneDistance;
            pointNear = Vector3.Transform(pointNormalized, view);
            pointNormalized.Z = state.FarPlaneDistance;
            pointFar = Vector3.Transform(pointNormalized, view);

            var r = new Ray(pointNear, (pointFar - pointNear).Normalized());

            return r;
        }

        public static Ray GetRay(CameraState state, IAppWindow window, Vector2 point2d) {
            var w = window.Width;
            var h = window.Height;
            var px = point2d.X;
            var py = point2d.Y;
            
            var viewInverted = state.ViewMatrix.PsudoInverted();
            var projMatrix = state.ProjectionMatrix;
            //Matrix4x4.Invert(projMatrix, out projMatrix);

            var v = new Vector3 {
                X = (2 * px / w - 1) / projMatrix.M11,
                Y = -(2 * py / h - 1) / projMatrix.M22,
                Z = 1 / projMatrix.M33
            };

            var zf = Vector3.Transform(v, viewInverted);

            var zn = Vector3.Zero;
            switch (state.Type) {
                case CameraState.CameraTypes.Orthographic:
                    v.Z = 0;
                    zn = Vector3.Transform(v, viewInverted);
                    break;
                case CameraState.CameraTypes.Perspective:
                    //v.Z = 0;
                    //zn = Vector3.Transform(v, matrix);
                    zn = state.Position;
                    break;

            }
            var r = zf - zn;
            r.Normalize();

            var ray = new Ray(zn + r * state.NearPlaneDistance, r);

            var ray1 = Point2DtoPoint3D(state, point2d);

            return ray;
        }

        public static Frustum GetFrustum(CameraState state) {
            return new Frustum(state.ProjectionMatrix, state.ViewMatrix) {

            };
        }
    }

    public class PerspectiveCameraComponent : GeneralCameraComponent {

        public float FieldOfViewRadians { get; set; }
        public float MinimumFieldOfView { get; set; }
        public float MaximumFieldOfView { get; set; }

        public PerspectiveCameraComponent() {
            ResetToDefault();
        }

        public override Matrix4x4 UpdateProjectionMatrix(float width, float height) {
            float aspectRatio = width / height;

            ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                        FieldOfViewRadians,
                        aspectRatio,
                        NearPlaneDistance,
                        FarPlaneDistance);
            
            return ProjectionMatrix;
        }

        public override void ResetToDefault() {
            UpDirection = Vector3.UnitY;
            FieldOfViewRadians = 1.05f;
            NearPlaneDistance = 1f;
            LookDirection = ForwardRH;
            Position = Vector3.UnitZ * 200f;

            FarPlaneDistance = Position.Length() * 70;
        }

        protected override void CreateState(out CameraState state) {
            state = CameraState.PerspectiveState();
        }
    }

}
