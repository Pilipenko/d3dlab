using System;
using System.Collections.Generic;
using System.Text;
using D3DLab.Std.Engine.Core.Utilities;

namespace D3DLab.Std.Engine.Core {
    /// <summary>
    /// Temporarily!!!! 
    /// </summary>
    public static class Statics {
        public static ICollision Collision { get; set; }
    }
    public interface ICollision {
        bool Intersects(ref BoundingBox box, ref Ray ray);
        bool Intersects(ref BoundingBox box, ref Ray ray, out float distance);
        bool Intersects(ref BoundingBox box, ref Ray ray, out System.Numerics.Vector3 point);
        void Merge(ref BoundingBox value1, ref BoundingBox value2, out BoundingBox result);
    }
}
