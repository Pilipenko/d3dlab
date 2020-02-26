using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace D3DLab.ECS.Ext {
    public static class Vector2Ex {
        public static Vector2 Normalized(this Vector2 v) {
            return Vector2.Normalize(v);
        }
        public static float AngleRad(this Vector2 u, Vector2 v) {
            return (float)(Math.Atan2(v.Y, v.X) - Math.Atan2(u.Y, u.X));
        }
        public static Vector3 ToVector3(this Vector2 v, float z = 0) {
            return new Vector3(v, z);
        }
    }
    public static class Vector3Ex {
        public static void Normalize(ref this Vector3 v) {
            var n = Vector3.Normalize(v);
            v.X = n.X;
            v.Y = n.Y;
            v.Z = n.Z;
        }
        public static Vector3 Normalized(this Vector3 v) {
            return Vector3.Normalize(v);
            
        }
        public static Vector3 Cross(this Vector3 v1, Vector3 v2) {
            return Vector3.Cross(v1, v2);
        }
        public static float AngleRad(this Vector3 u, Vector3 v) {
            return (float)Math.Atan2(Vector3.Cross(u, v).Length(), Vector3.Dot(u, v));
        }
        public static Vector4 ToVector4(this Vector3 v) {
            return new Vector4(v.X, v.Y, v.Z, 1);
        }
        public static Vector3 XYZ(this Vector4 v) {
            return new Vector3(v.X, v.Y, v.Z);
        }
      
        public static Vector3 Round(this Vector3 v, int round) {
            return new Vector3(
                (float)Math.Round(v.X, round),
                (float)Math.Round(v.Y, round), 
                (float)Math.Round(v.Z, round));
        }
        public static bool IsZero(ref this Vector3 v) {
            return Vector3.Zero == v;
        }
        

        public static Vector3 TransformedNormal(this Vector3 vector, Matrix4x4 matrix) {
            return Vector3.TransformNormal(vector, matrix);
        }
        public static Vector3 TransformedCoordinate(this Vector3 vector, Matrix4x4 matrix) {
            return Vector3.Transform(vector, matrix);
        }


        public static Matrix4x4 RotationAround(this Vector3 axis, float angleRadians) {
            return Matrix4x4.CreateFromAxisAngle(axis, angleRadians);
        }
        public static Matrix4x4 RotationAround(this Vector3 axis, float angle, Vector3 center) {
            var m1 = Matrix4x4.CreateTranslation(center * -1f);
            var m2 = axis.RotationAround(angle);
            var m3 = Matrix4x4.CreateTranslation(center);
            var m = m1 * m2 * m3;
            return m;
        }
        public static Vector3 FindAnyPerpendicular(this Vector3 n) {
            n.Normalize();
            Vector3 u = Vector3.Cross(new Vector3(0, 1, 0), n);
            if (u.LengthSquared() < 1e-3) {
                u = Vector3.Cross(new Vector3(1, 0, 0), n);
            }

            return u;
        }
        //public static Vector4 ToVector4(this System.Windows.Media.Color color) {
        //    color.Clamp();
        //    return new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        //}
        public static bool EqualsRound(this Vector3 a, Vector3 b, int round) {
            return Math.Round(a.X, round) == Math.Round(b.X, round)
                && Math.Round(a.Y, round) == Math.Round(b.Y, round)
                && Math.Round(a.Z, round) == Math.Round(b.Z, round);
        }
    }

    public static class Vector3CollectionEx {
        unsafe public static Vector3[] Transform(this Vector3[] positions, ref Matrix4x4 matrix) {
            if (positions == null || positions.Length == 0) {
                return null;
            }

            var result = new Vector3[positions.Length];
            fixed (Vector3* _pSrc = positions) {
                fixed (Vector3* _pDst = result) {
                    Vector3* pSrc = _pSrc, pDst = _pDst;
                    var end = pSrc + positions.Length;
                    for (; pSrc < end; ++pSrc, ++pDst) {
                        *pDst = Vector3.Transform(*pSrc, matrix);
                    }
                }
            }
            return result;
        }
        unsafe public static Vector3[] CalculateNormals(this Vector3[] positions, int[] indices) {
            var aNormals = new Vector3[positions.Length];
            var aPos = positions.ToArray();
            var aInd = indices.ToArray();
            fixed (Vector3* pNormal = aNormals) {
                fixed (Vector3* pPos = aPos) {
                    fixed (int* pInd = aInd) {
                        for (var i = 0; i < indices.Length; i += 3) {
                            var index0 = *(pInd + i);
                            var index1 = *(pInd + i + 1);
                            var index2 = *(pInd + i + 2);
                            Vector3 u = Vector3.Subtract(*(pPos + index1), *(pPos + index0));
                            Vector3 v = Vector3.Subtract(*(pPos + index2), *(pPos + index0));
                            Vector3 w = Vector3.Cross(u, v);
                            w.Normalize();

                            if (float.IsNaN(w.X)) {

                            }

                            *(pNormal + index0) = Vector3.Add(*(pNormal + index0), w);
                            *(pNormal + index1) = Vector3.Add(*(pNormal + index1), w);
                            *(pNormal + index2) = Vector3.Add(*(pNormal + index2), w);
                        }
                    }
                }
                for (int i = 0; i < aNormals.Length; i++) {
                    *(pNormal + i) = (*(pNormal + i)).Normalized();
                }
            }

            return aNormals;
        }

        unsafe public static List<Vector3> CalculateNormals(this List<Vector3> positions, List<int> indices) {

            var aNormals = new Vector3[positions.Count];
            var aPos = positions.ToArray();
            var aInd = indices.ToArray();
            fixed (Vector3* pNormal = aNormals) {
                fixed (Vector3* pPos = aPos) {
                    fixed (int* pInd = aInd) {
                        for (var i = 0; i < indices.Count; i += 3) {
                            var index0 = *(pInd + i);
                            var index1 = *(pInd + i + 1);
                            var index2 = *(pInd + i + 2);
                            Vector3 u = Vector3.Subtract(*(pPos + index1), *(pPos + index0));
                            Vector3 v = Vector3.Subtract(*(pPos + index2), *(pPos + index0));
                            Vector3 w = Vector3.Cross(u, v);
                            w.Normalize();

                            *(pNormal + index0) = Vector3.Add(*(pNormal + index0), w);
                            *(pNormal + index1) = Vector3.Add(*(pNormal + index1), w);
                            *(pNormal + index2) = Vector3.Add(*(pNormal + index2), w);
                        }
                    }
                }
                for (int i = 0; i < aNormals.Length; i++) {
                    *(pNormal + i) = (*(pNormal + i)).Normalized();
                }
            }

            return aNormals.ToList();
        }

    }

    public static class ConvertorsEx {
        public static float ToRad(this float degrees) {
            return (float)(degrees * Math.PI / 180f);
        }
        public static float ToDeg(this float radians) {
            return (float)(radians * 180f / Math.PI);
        }
    }


    public static class PlaneEx{
        public static void Normalize(this Plane plane) {
            var normal = plane.Normal;
            float magnitude = 1.0f / (float)(Math.Sqrt((normal.X * normal.X) + (normal.Y * normal.Y) + (normal.Z * normal.Z)));

            plane.Normal.X *= magnitude;
            plane.Normal.Y *= magnitude;
            plane.Normal.Z *= magnitude;
            plane.D *= magnitude;
        }
    }    
}
