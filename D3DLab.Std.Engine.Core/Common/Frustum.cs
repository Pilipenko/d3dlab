using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace D3DLab.Std.Engine.Core.Common {
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// http://gamedevs.org/uploads/fast-extraction-viewing-frustum-planes-from-world-view-projection-matrix.pdf
    /// </remarks>
    public class Frustum {
        public enum ContainmentType {
            Disjoint,
            Contains,
            Intersects,
        }
        private struct SixPlane {
            public Plane Left;
            public Plane Right;
            public Plane Bottom;
            public Plane Top;
            public Plane Near;
            public Plane Far;
        }

        readonly Plane[] planes = new Plane[6];
        private SixPlane _planes;

        public Frustum(Matrix4x4 projection, Matrix4x4 view) {
            var m = view * projection;
            _planes.Left = Plane.Normalize(
                new Plane(
                    m.M14 + m.M11,
                    m.M24 + m.M21,
                    m.M34 + m.M31,
                    m.M44 + m.M41));

            _planes.Right = Plane.Normalize(
                new Plane(
                    m.M14 - m.M11,
                    m.M24 - m.M21,
                    m.M34 - m.M31,
                    m.M44 - m.M41));

            _planes.Bottom = Plane.Normalize(
                new Plane(
                    m.M14 + m.M12,
                    m.M24 + m.M22,
                    m.M34 + m.M32,
                    m.M44 + m.M42));

            _planes.Top = Plane.Normalize(
                new Plane(
                    m.M14 - m.M12,
                    m.M24 - m.M22,
                    m.M34 - m.M32,
                    m.M44 - m.M42));

            _planes.Near = Plane.Normalize(
                new Plane(
                    m.M13,
                    m.M23,
                    m.M33,
                    m.M43));

            _planes.Far = Plane.Normalize(
                new Plane(
                    m.M14 - m.M13,
                    m.M24 - m.M23,
                    m.M34 - m.M33,
                    m.M44 - m.M43));
        }

        public Frustum(float screenDepth, Matrix4x4 projection, Matrix4x4 view) {
            // Calculate the minimum Z distance in the frustum.
            float zMinimum = -projection.M43 / projection.M33;
            float r = screenDepth / (screenDepth - zMinimum);
            projection.M33 = r;
            projection.M43 = -r * zMinimum;

            // Create the frustum matrix from the view matrix and updated projection matrix.
            Matrix4x4 matrix = view * projection;

            // Calculate near plane of frustum.
            planes[0] = new Plane(matrix.M14 + matrix.M13, matrix.M24 + matrix.M23, matrix.M34 + matrix.M33, matrix.M44 + matrix.M43);
            planes[0].Normalize();

            // Calculate far plane of frustum.
            planes[1] = new Plane(matrix.M14 - matrix.M13, matrix.M24 - matrix.M23, matrix.M34 - matrix.M33, matrix.M44 - matrix.M43);
            planes[1].Normalize();

            // Calculate left plane of frustum.
            planes[2] = new Plane(matrix.M14 + matrix.M11, matrix.M24 + matrix.M21, matrix.M34 + matrix.M31, matrix.M44 + matrix.M41);
            planes[2].Normalize();

            // Calculate right plane of frustum.
            planes[3] = new Plane(matrix.M14 - matrix.M11, matrix.M24 - matrix.M21, matrix.M34 - matrix.M31, matrix.M44 - matrix.M41);
            planes[3].Normalize();

            // Calculate top plane of frustum.
            planes[4] = new Plane(matrix.M14 - matrix.M12, matrix.M24 - matrix.M22, matrix.M34 - matrix.M32, matrix.M44 - matrix.M42);
            planes[4].Normalize();

            // Calculate bottom plane of frustum.
            planes[5] = new Plane(matrix.M14 + matrix.M12, matrix.M24 + matrix.M22, matrix.M34 + matrix.M32, matrix.M44 + matrix.M42);
            planes[5].Normalize();
        }

        public bool CheckRectangle(float xCenter, float yCenter, float zCenter, float xSize, float ySize, float zSize) {
            // Check if any of the 6 planes of the rectangle are inside the view frustum.
            for (var i = 0; i < 6; i++) {
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter - xSize, yCenter - ySize, zCenter - zSize)) >= 0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter + xSize, yCenter - ySize, zCenter - zSize)) >= 0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter - xSize, yCenter + ySize, zCenter - zSize)) >= 0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter + xSize, yCenter + ySize, zCenter - zSize)) >= 0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter - xSize, yCenter - ySize, zCenter + zSize)) >= 0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter + xSize, yCenter - ySize, zCenter + zSize)) >= 0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter - xSize, yCenter + ySize, zCenter + zSize)) >= 0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter + xSize, yCenter + ySize, zCenter + zSize)) >= 0f)
                    continue;

                return false;
            }

            return true;
        }
        public bool Intersec(BoundingBox box) {

            var corners = box.Corners();
            // Check if any of the 6 planes of the rectangle are inside the view frustum.
            for (var i = 0; i < 6; i++) {
                if (Plane.DotCoordinate(planes[i], corners[0]) >= 0f) continue;                      
                if (Plane.DotCoordinate(planes[i], corners[1]) >= 0f) continue;                      
                if (Plane.DotCoordinate(planes[i], corners[2]) >= 0f) continue;                      
                if (Plane.DotCoordinate(planes[i], corners[3]) >= 0f) continue;                      
                if (Plane.DotCoordinate(planes[i], corners[4]) >= 0f) continue;                      
                if (Plane.DotCoordinate(planes[i], corners[5]) >= 0f) continue;                      
                if (Plane.DotCoordinate(planes[i], corners[6]) >= 0f) continue;                      
                if (Plane.DotCoordinate(planes[i], corners[7]) >= 0f) continue;
                return false;
            }

            return true;
        }
        bool CheckSphere(Vector3 center, float radius) {
            // Check if the radius of the sphere is inside the view frustum.
            for (int i = 0; i < 6; i++) {
                if (Plane.DotCoordinate(planes[i], center) < -radius)
                    return false;
            }
            return true;
        }
        bool CheckCube(float xCenter, float yCenter, float zCenter, float radius) {
            // Check if any one point of the cube is in the view frustum.
            for (var i = 0; i < 6; i++) {
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter - radius, yCenter - radius, zCenter - radius)) >= 0.0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter + radius, yCenter - radius, zCenter - radius)) >= 0.0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter - radius, yCenter + radius, zCenter - radius)) >= 0.0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter + radius, yCenter + radius, zCenter - radius)) >= 0.0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter - radius, yCenter - radius, zCenter + radius)) >= 0.0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter + radius, yCenter - radius, zCenter + radius)) >= 0.0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter - radius, yCenter + radius, zCenter + radius)) >= 0.0f)
                    continue;
                if (Plane.DotCoordinate(planes[i], new Vector3(xCenter + radius, yCenter + radius, zCenter + radius)) >= 0.0f)
                    continue;

                return false;
            }
            return true;
        }
        bool CheckPoint(Vector3 point) {
            // Check if the point is inside all six planes of the view frustum.
            for (var i = 0; i < 6; i++)
                if (Plane.DotCoordinate(planes[i], point) < 0f)
                    return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ContainmentType Contains(ref BoundingBox box) {
            Plane* planes = (Plane*)Unsafe.AsPointer(ref _planes);

            ContainmentType result = ContainmentType.Contains;
            for (int i = 0; i < 6; i++) {
                Plane plane = planes[i];

                // Approach: http://zach.in.tu-clausthal.de/teaching/cg_literatur/lighthouse3d_view_frustum_culling/index.html

                Vector3 positive = new Vector3(box.Minimum.X, box.Minimum.Y, box.Minimum.Z);
                Vector3 negative = new Vector3(box.Maximum.X, box.Maximum.Y, box.Maximum.Z);

                if (plane.Normal.X >= 0) {
                    positive.X = box.Maximum.X;
                    negative.X = box.Minimum.X;
                }
                if (plane.Normal.Y >= 0) {
                    positive.Y = box.Maximum.Y;
                    negative.Y = box.Minimum.Y;
                }
                if (plane.Normal.Z >= 0) {
                    positive.Z = box.Maximum.Z;
                    negative.Z = box.Minimum.Z;
                }

                // If the positive vertex is outside (behind plane), the box is disjoint.
                float positiveDistance = Plane.DotCoordinate(plane, positive);
                if (Math.Round(positiveDistance, 3) < 0) {
                    return ContainmentType.Disjoint;
                }

                // If the negative vertex is outside (behind plane), the box is intersecting.
                // Because the above check failed, the positive vertex is in front of the plane,
                // and the negative vertex is behind. Thus, the box is intersecting this plane.
                float negativeDistance = Plane.DotCoordinate(plane, negative);
                if (negativeDistance < 0) {
                    result = ContainmentType.Intersects;
                }
            }

            return result;
        }
    }
}
