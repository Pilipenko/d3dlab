using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Ext;
using g3;
using Veldrid.Utilities;

namespace D3DLab.Std.Engine.Core.Utilities {
    public struct Ray {
        public readonly Vector3 Origin;
        public readonly Vector3 Direction;
        internal readonly Ray3f g3Rayf;
        internal readonly Ray3d g3Rayd;

        public Ray(Vector3 or, Vector3 dir) {
            Origin = or;
            Direction = dir;
            Direction.Normalize();
            g3Rayf = new Ray3f(or.ToVector3f(), dir.ToVector3f(), true);
            g3Rayd = new Ray3d(g3Rayf.Origin, g3Rayf.Direction, true);
        }
        public bool Intersects(ref BoundingBox box) {
            // http://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-box-intersection

            float tmin = (box.Minimum.X - Origin.X) / Direction.X;
            float tmax = (box.Maximum.X - Origin.X) / Direction.X;

            if (tmin > tmax) {
                Swap(ref tmin, ref tmax);
            }

            float tymin = (box.Minimum.Y - Origin.Y) / Direction.Y;
            float tymax = (box.Maximum.Y - Origin.Y) / Direction.Y;

            if (tymin > tymax) {
                Swap(ref tymin, ref tymax);
            }

            if ((tmin > tymax) || (tymin > tmax)) {
                return false;
            }

            if (tymin > tmin) {
                tmin = tymin;
            }

            if (tymax < tmax) {
                tmax = tymax;
            }

            float tzmin = (box.Minimum.Z - Origin.Z) / Direction.Z;
            float tzmax = (box.Maximum.Z - Origin.Z) / Direction.Z;

            if (tzmin > tzmax) {
                Swap(ref tzmin, ref tzmax);
            }

            if ((tmin > tzmax) || (tzmin > tmax)) {
                return false;
            }

            if (tzmin > tmin) {
                tmin = tzmin;
            }

            if (tzmax < tmax) {
                tmax = tzmax;
            }

            return true;
        }

        public Vector3 IntersecWithPlane(Vector3 position, Vector3 normal) {
            var dn = Vector3.Dot(normal, this.Direction);
            var u = Vector3.Dot(normal, position - this.Origin) / dn;
            return this.Origin + u * this.Direction;
        }

        void Swap(ref float a, ref float b) {
            var temp = a;
            a = b;
            b = temp;
        }

        public Ray Transformed(Matrix4x4 m) {
            var or = Vector3.Transform(Origin, m);
            var dir = Vector3.TransformNormal(Direction, m);
            return new Ray(or, dir);
        }

        public Ray Inverted() {
            return new Ray(Origin, -Direction);
        }
    }

    public enum BoundingContainmentType {
        Disjoint,
        Contains,
        Intersects,
    }
    /// <summary>
    /// COPY/PASTE
    /// </summary>
    public struct BoundingBox : IEquatable<BoundingBox> {
        public static BoundingBox Zero => new BoundingBox(AxisAlignedBox3d.Zero);
        public static BoundingBox Create(Vector3 min, Vector3 max) {
            //BoundsUtil.Bounds(,)
            return new BoundingBox(min, max, new AxisAlignedBox3f(min.ToVector3f(), max.ToVector3f()));
        }
        //public static BoundingBox CreateFromComponent(HittableGeometryComponent com) {
        //    return new BoundingBox(com.DMesh.GetBounds());
        //}
        //TODO
        readonly AxisAlignedBox3f boxf;
        readonly AxisAlignedBox3d boxd;
        //g3.BoundsUtil.Bounds

        public readonly Vector3 Minimum;
        public readonly Vector3 Maximum;

        Vector3[] corners;

        public BoundingBox(Vector3 min, Vector3 max) {
            Minimum = min;
            Maximum = max; 
            boxf = new AxisAlignedBox3f(Minimum.X, Minimum.Y, Minimum.Z, Maximum.X, Maximum.Y, Maximum.Z);
            boxd = new AxisAlignedBox3d(Minimum.X, Minimum.Y, Minimum.Z, Maximum.X, Maximum.Y, Maximum.Z);
            corners = null;
        }
        BoundingBox(Vector3 min, Vector3 max, AxisAlignedBox3f box3F) {
            Minimum = min;
            Maximum = max;
            boxf = box3F;
            boxd = boxf;
            corners = null;
        }
        internal BoundingBox(AxisAlignedBox3d box3d) {
            Minimum = box3d.Min.ToVector3();
            Maximum = box3d.Max.ToVector3();
            boxf = new AxisAlignedBox3f(Minimum.X, Minimum.Y, Minimum.Z, Maximum.X, Maximum.Y, Maximum.Z);
            boxd = box3d; 
             corners = null;
        }

        public BoundingBox Merge(BoundingBox box) {
            Statics.Collision.Merge(ref this, ref box, out var res);
            return res;
        }
        public bool Contains(ref Vector3 p) {
            return boxf.Contains(p.ToVector3f());
        }

        public BoundingContainmentType Contains(ref BoundingBox other) {
            if (Maximum.X < other.Minimum.X || Minimum.X > other.Maximum.X
                || Maximum.Y < other.Minimum.Y || Minimum.Y > other.Maximum.Y
                || Maximum.Z < other.Minimum.Z || Minimum.Z > other.Minimum.Z) {
                return BoundingContainmentType.Disjoint;
            } else if (Minimum.X <= other.Minimum.X && Maximum.X >= other.Maximum.X
                  && Minimum.Y <= other.Minimum.Y && Maximum.Y >= other.Maximum.Y
                  && Minimum.Z <= other.Minimum.Z && Maximum.Z >= other.Maximum.Z) {
                return BoundingContainmentType.Contains;
            } else {
                return BoundingContainmentType.Intersects;
            }
        }

        public bool Intersects(ref Ray ray, out float distance) {
            //var b1 = ray.Intersects(ref this);
            var b = Statics.Collision.Intersects(ref this, ref ray, out distance);
            return b;
        }
        public bool Intersects(ref Ray ray, out Vector3 p) {
            return Statics.Collision.Intersects(ref this, ref ray, out p);
        }

        public bool Intersects(ref BoundingBox bb) {
            return boxf.Intersects(bb.boxf);
        }

        public Vector3 GetCenter() {
            return (Maximum + Minimum) / 2f;
        }

        public Vector3 GetDimensions() {
            return Maximum - Minimum;
        }
        public float GetDiagonal() {
            return GetDimensions().Length();
        }

        public BoundingBox Transform(Matrix4x4 mat) {
            return Transform(this, mat);
        }

        public static unsafe BoundingBox Transform(BoundingBox box, Matrix4x4 mat) {
            AlignedBoxCorners corners = box.GetCorners();
            Vector3* cornersPtr = (Vector3*)&corners;

            Vector3 min = Vector3.Transform(cornersPtr[0], mat);
            Vector3 max = Vector3.Transform(cornersPtr[0], mat);

            for (int i = 1; i < 8; i++) {
                min = Vector3.Min(min, Vector3.Transform(cornersPtr[i], mat));
                max = Vector3.Max(max, Vector3.Transform(cornersPtr[i], mat));
            }

        //    box.boxf.Translate(new Vector3f(mat.Translation.X, mat.Translation.Y, mat.Translation.Z));

            return new BoundingBox(min, max);
        }

        public Vector3[] Corners() {
            if (corners.IsNotNull()) {
                return corners;
            }
            corners = new[] {
                boxf.Corner(0).ToVector3(),
                boxf.Corner(1).ToVector3(),
                boxf.Corner(2).ToVector3(),
                boxf.Corner(3).ToVector3(),
                boxf.Corner(4).ToVector3(),
                boxf.Corner(5).ToVector3(),
                boxf.Corner(6).ToVector3(),
                boxf.Corner(7).ToVector3()
            };
            return corners;
        }


        public static unsafe BoundingBox CreateFromVertices(
            Vector3* vertices,
            int numVertices,
            Quaternion rotation,
            Vector3 offset,
            Vector3 scale)
            => CreateFromPoints(vertices, Unsafe.SizeOf<Vector3>(), numVertices, rotation, offset, scale);
        public static unsafe BoundingBox CreateFromPoints(
            Vector3* vertexPtr,
            int numVertices,
            int vertexStride,
            Quaternion rotation,
            Vector3 offset,
            Vector3 scale) {
            byte* bytePtr = (byte*)vertexPtr;
            Vector3 min = Vector3.Transform(*vertexPtr, rotation);
            Vector3 max = Vector3.Transform(*vertexPtr, rotation);

            for (int i = 1; i < numVertices; i++) {
                bytePtr = bytePtr + vertexStride;
                vertexPtr = (Vector3*)bytePtr;
                Vector3 pos = Vector3.Transform(*vertexPtr, rotation);

                if (min.X > pos.X) min.X = pos.X;
                if (max.X < pos.X) max.X = pos.X;

                if (min.Y > pos.Y) min.Y = pos.Y;
                if (max.Y < pos.Y) max.Y = pos.Y;

                if (min.Z > pos.Z) min.Z = pos.Z;
                if (max.Z < pos.Z) max.Z = pos.Z;
            }

            return new BoundingBox((min * scale) + offset, (max * scale) + offset);
        }




        public static unsafe BoundingBox CreateFromVertices(Vector3[] vertices) {
            return CreateFromVertices(vertices, Quaternion.Identity, Vector3.Zero, Vector3.One);
        }

        public static unsafe BoundingBox CreateFromVertices(Vector3[] vertices, Quaternion rotation, Vector3 offset, Vector3 scale) {
            Vector3 min = Vector3.Transform(vertices[0], rotation);
            Vector3 max = Vector3.Transform(vertices[0], rotation);

            for (int i = 1; i < vertices.Length; i++) {
                Vector3 pos = Vector3.Transform(vertices[i], rotation);

                if (min.X > pos.X) min.X = pos.X;
                if (max.X < pos.X) max.X = pos.X;

                if (min.Y > pos.Y) min.Y = pos.Y;
                if (max.Y < pos.Y) max.Y = pos.Y;

                if (min.Z > pos.Z) min.Z = pos.Z;
                if (max.Z < pos.Z) max.Z = pos.Z;
            }

            return new BoundingBox((min * scale) + offset, (max * scale) + offset);
        }

        public static BoundingBox Combine(BoundingBox box1, BoundingBox box2) {
            return new BoundingBox(
                Vector3.Min(box1.Minimum, box2.Minimum),
                Vector3.Max(box1.Maximum, box2.Maximum));
        }

        public static bool operator ==(BoundingBox first, BoundingBox second) {
            return first.Equals(second);
        }

        public static bool operator !=(BoundingBox first, BoundingBox second) {
            return !first.Equals(second);
        }

        public bool Equals(BoundingBox other) {
            return Minimum == other.Minimum && Maximum == other.Maximum;
        }

        public override string ToString() {
            return string.Format("Min:{0}, Max:{1}", Minimum, Maximum);
        }

        public override bool Equals(object obj) {
            return obj is BoundingBox && ((BoundingBox)obj).Equals(this);
        }

        public override int GetHashCode() {
            int h1 = Minimum.GetHashCode();
            int h2 = Maximum.GetHashCode();
            uint shift5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)shift5 + h1) ^ h2;
        }

        public AlignedBoxCorners GetCorners() {
            AlignedBoxCorners corners;
            corners.NearBottomLeft = new Vector3(Minimum.X, Minimum.Y, Maximum.Z);
            corners.NearBottomRight = new Vector3(Maximum.X, Minimum.Y, Maximum.Z);
            corners.NearTopLeft = new Vector3(Minimum.X, Maximum.Y, Maximum.Z);
            corners.NearTopRight = new Vector3(Maximum.X, Maximum.Y, Maximum.Z);

            corners.FarBottomLeft = new Vector3(Minimum.X, Minimum.Y, Minimum.Z);
            corners.FarBottomRight = new Vector3(Maximum.X, Minimum.Y, Minimum.Z);
            corners.FarTopLeft = new Vector3(Minimum.X, Maximum.Y, Minimum.Z);
            corners.FarTopRight = new Vector3(Maximum.X, Maximum.Y, Minimum.Z);

            return corners;
        }

        public Vector3 Size() {
            return new Vector3(
                Math.Abs(Minimum.X - Maximum.X),
                Math.Abs(Minimum.Y - Maximum.Y),
                Math.Abs(Minimum.Z - Maximum.Z));
        }

        public bool ContainsNaN() {
            return float.IsNaN(Minimum.X) || float.IsNaN(Minimum.Y) || float.IsNaN(Minimum.Z)
                || float.IsNaN(Maximum.X) || float.IsNaN(Maximum.Y) || float.IsNaN(Maximum.Z);
        }

    }
}