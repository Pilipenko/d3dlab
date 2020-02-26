using D3DLab.ECS;
using D3DLab.ECS.Camera;
using D3DLab.ECS.Ext;
using D3DLab.ECS.Input;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Input;
using D3DLab.Std.Engine.Core.Shaders;
using D3DLab.Std.Engine.Core.Systems;
using D3DLab.Std.Engine.Core.Utilities;
using System;
using System.Numerics;

namespace D3DLab.Std.Engine.Core {
    public interface IViewport {
        Vector3 ScreenToV3(Vector2 screen, CameraState camera, IAppWindow window);
        Ray UnProject(Vector2 screen, CameraState camera, IAppWindow window);
    }

    public class SceneSnapshot : ISceneSnapshot {
        public IViewport Viewport { get; }
      //  public IContextState ContextState { get; }
        public IManagerChangeNotify Notifier { get; }
        public IAppWindow Window { get; }

        public InputSnapshot InputSnapshot { get; }
        public TimeSpan FrameRateTime { get; }

        public ElementTag CurrentCameraTag { get; private set; }

        public CameraState Camera { get; private set; }
        public LightState[] Lights { get; private set; }

        public IOctree Octree { get; }

        public SceneSnapshot(IAppWindow win, IManagerChangeNotify notifier,
            IViewport viewport,
            IOctree octree, InputSnapshot isnapshot, TimeSpan time) {
            Viewport = viewport;
            Notifier = notifier;
            InputSnapshot = isnapshot;
            FrameRateTime = time;
            Window = win;
            Lights = new LightState[LightStructBuffer.MaxCount];
            Octree = octree;
        }

        public void UpdateCamera(ElementTag tag, CameraState state) {
            Camera = state;
            CurrentCameraTag = tag;
        }
        public void UpdateLight(int index, LightState state) {
            Lights[index] = state;
        }
    }



    public class Viewport {


        void Unproject(IAppWindow window, ref Vector3 source, ref Matrix4x4 matrix, out Vector3 vector) {
            var X = 0;
            var Y = 0;
            var MinDepth = 0f;
            var MaxDepth = 1f;
            vector.X = (((source.X - X) / (window.Width)) * 2f) - 1f;
            vector.Y = -((((source.Y - Y) / (window.Height)) * 2f) - 1f);
            vector.Z = (source.Z - MinDepth) / (MaxDepth - MinDepth);

            float a = (((vector.X * matrix.M14) + (vector.Y * matrix.M24)) + (vector.Z * matrix.M34)) + matrix.M44;
            vector = Vector3.Transform(vector, matrix);

            if (!MathUtil.IsOne(a)) {
                vector = (vector / a);
            }
        }

        
        public Ray UnProject(Vector2 screen, CameraState camera, IAppWindow window) {
            var winW = window.Width;
            var winH = window.Height;

            var matrix = camera.ViewMatrix * camera.ProjectionMatrix;
            Matrix4x4.Invert(matrix, out matrix);

            //по X,Y позицией курсора, по Z: 0-мин. глубина, 1-максимальная глубина.
            var nearSource = new Vector3(screen, 0);
            var farSource = new Vector3(screen, 1);

            Unproject( window, ref nearSource, ref matrix, out var nearPoint);
            Unproject(window, ref farSource, ref matrix, out var farPoint);

            //вычисляем направление и нормируем его.
            var direction = farPoint - nearPoint;
            direction.Normalize();

            return new Ray(nearPoint, direction);
            //return UnProject(camera, winW, winH, screen);
        }

        [Obsolete]
        public Ray UnProject(CameraState camera, float w, float h, Vector2 point2d) {//IAppWindow win,
            var px = (float)point2d.X;
            var py = (float)point2d.Y;

            var viewMatrix = camera.ViewMatrix;
            Vector3 v = new Vector3();

            var matrix = viewMatrix.PsudoInverted();
            //float w = win.Width;
            //float h = win.Height;

            var projMatrix = camera.ProjectionMatrix;
            Vector3 zn;
            v.X = (2 * px / w - 1) / projMatrix.M11;
            v.Y = -(2 * py / h - 1) / projMatrix.M22;
            v.Z = 1 / projMatrix.M33;
            Vector3 zf = Vector3.Transform(v, matrix);

            if (camera is PerspectiveCameraComponent) {
                v.Z = 0;
                //zn = Vector3.Transform(v, matrix);
                zn = camera.Position;
            } else {
                v.Z = 0;
                zn = Vector3.Transform(v, matrix);
            }
            Vector3 r = zf - zn;
            r.Normalize();

            return new Ray(zn + r * camera.NearPlaneDistance, r);
        }
    }
}
