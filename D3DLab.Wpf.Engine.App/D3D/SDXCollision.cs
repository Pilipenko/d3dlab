using D3DLab.Std.Engine.Core;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.SDX.Engine {
    public class SDXCollision : ICollision {
        public bool Intersects(ref Std.Engine.Core.Utilities.BoundingBox box, ref Std.Engine.Core.Utilities.Ray ray) {
            float distance;
            var sbox = new BoundingBox(box.Minimum.ToSDXVector3(), box.Maximum.ToSDXVector3());
            var sray = new Ray(ray.Origin.ToSDXVector3(), ray.Direction.ToSDXVector3());
            return Collision.RayIntersectsBox(ref sray, ref sbox, out distance);
        }

        public bool Intersects(ref Std.Engine.Core.Utilities.BoundingBox box, ref Std.Engine.Core.Utilities.Ray ray, out float distance) {
            var sbox = new BoundingBox(box.Minimum.ToSDXVector3(), box.Maximum.ToSDXVector3());
            var sray = new Ray(ray.Origin.ToSDXVector3(), ray.Direction.ToSDXVector3());

            var b = Collision.RayIntersectsBox(ref sray, ref sbox, out SharpDX.Vector3 p);

            return Collision.RayIntersectsBox(ref sray, ref sbox, out distance);
        }

        public bool Intersects(ref Std.Engine.Core.Utilities.BoundingBox box, ref Std.Engine.Core.Utilities.Ray ray, out System.Numerics.Vector3 point) {
            var sbox = new BoundingBox(box.Minimum.ToSDXVector3(), box.Maximum.ToSDXVector3());
            var sray = new Ray(ray.Origin.ToSDXVector3(), ray.Direction.ToSDXVector3());
            var res = Collision.RayIntersectsBox(ref sray, ref sbox, out SharpDX.Vector3 p);

            point = p.ToNVector3();

            return res;
        }

        public void Merge(ref Std.Engine.Core.Utilities.BoundingBox value1, ref Std.Engine.Core.Utilities.BoundingBox value2, out Std.Engine.Core.Utilities.BoundingBox result) {
            var b1 = new BoundingBox(value1.Minimum.ToSDXVector3(), value1.Maximum.ToSDXVector3());
            var b2 = new BoundingBox(value2.Minimum.ToSDXVector3(), value2.Maximum.ToSDXVector3());
            BoundingBox.Merge(ref b1, ref b2, out var res);
            result = new Std.Engine.Core.Utilities.BoundingBox(res.Minimum.ToNVector3(), res.Maximum.ToNVector3());
        }
    }
}
