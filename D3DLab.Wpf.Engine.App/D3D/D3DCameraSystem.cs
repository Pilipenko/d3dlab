using D3DLab.ECS;
using D3DLab.ECS.Ext;
using D3DLab.ECS.Components;
using D3DLab.SDX.Engine;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Components.Movements;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Systems;
using D3DLab.Std.Engine.Core.Utilities;
using System;
using System.Numerics;

namespace D3DLab.Wpf.Engine.App.D3D {
    //TODO: refactor this!!!
    public class D3DCameraSystem : CameraSystem {
        static class D3DExt {
            public static Vector3 ScreenToV3(GeneralCameraComponent camera, Vector2 screen, float winW, float winH, float delta) {
                var c = new Viewport().UnProject(camera.GetState(), winW, winH, screen);

                var plane = new SharpDX.Plane(camera.Position.ToSDXVector3(), camera.LookDirection.ToSDXVector3());
                var ray = new SharpDX.Ray(c.Origin.ToSDXVector3(), -c.Direction.ToSDXVector3());
                var inter = plane.Intersects(ref ray, out SharpDX.Vector3 point);

                return new Vector3(point.X, point.Y, point.Z);
            }
        }

        class D3DPerspMoveHandler : PerspectiveCameraMoveHandler {
             readonly IContextState context;

            public D3DPerspMoveHandler(PerspectiveCameraComponent camera, SceneSnapshot snapshot, IContextState context) : base(camera, snapshot) {
                this.context = context;
            }

            public override void Handle(CameraZoomingComponent component) {
                var winW = snapshot.Window.Width;
                var winH = snapshot.Window.Height;
                float delta = component.Delta;
                var screen = component.MovementData.End;

                var zoomAround = snapshot.Viewport.ScreenToV3(screen, camera.GetState(), snapshot.Window);

                var sign = Math.Sign(delta);
                delta = delta * 0.01f; //);

                camera.Position -= camera.LookDirection * (delta);

                context
                    .GetEntityManager()
                    .GetEntity(camera.EntityTag)
                    .RemoveComponent(component);
            }

            void ChangeCameraDistance1(float delta, Vector3 zoomAround) {
                // Handle the 'zoomAround' point
                var target = camera.Position + camera.LookDirection;
                var relativeTarget = zoomAround - target;
                var relativePosition = zoomAround - camera.Position;

                var f = (float)Math.Pow(2.5, delta);
                var newRelativePosition = relativePosition * f;
                var newRelativeTarget = relativeTarget * f;

                var newTarget = zoomAround - newRelativeTarget;
                var newPosition = zoomAround - newRelativePosition;
                var newLookDirection = newTarget - newPosition;

                camera.LookDirection = newLookDirection;
                camera.Position = newPosition;
            }
            void ZoomByChangingFieldOfView1(float delta) {
                float fov = camera.FieldOfViewRadians;
                float d = camera.LookDirection.Length();
                float r = d * (float)Math.Tan(0.5f * fov / 180 * Math.PI);

                fov *= 1f + (delta * 0.5f);
                //if (fov < controller.MinimumFieldOfView) {
                //    fov = controller.MinimumFieldOfView;
                //}

                //if (fov > controller.MaximumFieldOfView) {
                //    fov = controller.MaximumFieldOfView;
                //}
                System.Diagnostics.Trace.WriteLine("FOV "+ fov);
                camera.FieldOfViewRadians = fov;
                float d2 = r / (float)Math.Tan(0.5f * fov / 180 * Math.PI);
                var newLookDirection = camera.LookDirection;
                newLookDirection.Normalize();
                newLookDirection *= (float)d2;
                var target = camera.Position + camera.LookDirection;
                camera.Position = target - newLookDirection;
                camera.LookDirection = newLookDirection;
            }
        }
        class D3DOrthoMoveHandler : OrthographicCameraMoveHandler {
            public D3DOrthoMoveHandler(OrthographicCameraComponent camera, SceneSnapshot snapshot) : base(camera, snapshot) { }

            public override void Handle(CameraZoomingComponent component) {
                var winW = snapshot.Window.Width;
                var winH = snapshot.Window.Height;
                float delta = component.Delta;
                var screen = component.MovementData.End;

                var zoomAround = D3DExt.ScreenToV3(camera, screen, winW, winH, delta);

                delta = delta * 0.001f;
                if (Ext.ChangeCameraDistance(camera, ref delta, zoomAround)) {
                    // Modify the camera width
                    camera.Width *= (float)Math.Pow(2.5f, delta);
                    System.Diagnostics.Trace.WriteLine($"ORTO W:{camera.Width}, D: {delta}, Center:{zoomAround}");
                }
            }
        }

        protected override ICameraMovementComponentHandler CreateHandlerOrthographicHandler(OrthographicCameraComponent com, SceneSnapshot snapshot) {
            return new D3DOrthoMoveHandler(com, snapshot);
        }
        protected override ICameraMovementComponentHandler CreateHandlerPerspectiveHandler(PerspectiveCameraComponent com, SceneSnapshot snapshot) {
            return new D3DPerspMoveHandler(com, snapshot, ContextState);
        }
    }
}
