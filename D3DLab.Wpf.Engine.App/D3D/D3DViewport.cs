using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using D3DLab.ECS;
using D3DLab.ECS.Camera;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Utilities;

namespace D3DLab.SDX.Engine {
    public class D3DViewport : Viewport, IViewport {
        
        public Vector3 ScreenToV3(Vector2 screen, CameraState camera, IAppWindow window) {
            var winW = window.Width;
            var winH = window.Height;

            var c = UnProject(screen, camera, window);

            var plane = new SharpDX.Plane(camera.Position.ToSDXVector3(), camera.LookDirection.ToSDXVector3());
            var ray = new SharpDX.Ray(c.Origin.ToSDXVector3(), -c.Direction.ToSDXVector3());
            var inter = plane.Intersects(ref ray, out SharpDX.Vector3 point);

            return new Vector3(point.X, point.Y, point.Z);
        }
    }
}
