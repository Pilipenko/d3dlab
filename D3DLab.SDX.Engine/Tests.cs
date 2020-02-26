using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;

namespace D3DLab.SDX.Engine {
    static class Tests {

        public static void BoundingBoxTest(System.Numerics.Vector3[] vectors) {

            var v3 = vectors.Select(x=>x.ToSDXVector3()).ToArray();

            var box1 = ToBoundingBoxNumericsVector(v3);
        }


        static BoundingBox ToBoundingBoxDX(Vector3[] vector) {
            return BoundingBox.FromPoints(vector);
        }

        static BoundingBox ToBoundingBoxNumericsVector(Vector3[] vectors) {
            var simdLength = System.Numerics.Vector<Vector3>.Count;

            var v3Max = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var v3Min = Vector3.Zero;

            var vmin = new System.Numerics.Vector<Vector3>(v3Max);
            var vmax = new System.Numerics.Vector<Vector3>(v3Min);
            var i = 0;

            for (i = 0; i <= vectors.Length - simdLength; i += simdLength) {
                var va = new System.Numerics.Vector<Vector3>(vectors, i);
                vmin = System.Numerics.Vector.Min(va, vmin);
                vmax = System.Numerics.Vector.Max(va, vmax);
            }

            var minX = float.MaxValue;
            var maxX = float.MinValue;

            var minZ = float.MaxValue;
            var maxZ = float.MinValue;
            
            var maxY = float.MinValue;
            var minY = float.MaxValue;

            for (var j = 0; j < simdLength; ++j) {
                minX = Math.Min(minX, vmin[j].X);
                maxX = Math.Max(maxX, vmax[j].X);

                minZ = Math.Min(minZ, vmin[j].Y);
                maxZ = Math.Max(maxZ, vmax[j].Y);

                maxY = Math.Min(maxY, vmin[j].Z);
                minY = Math.Max(minY, vmax[j].Z);
            }

            for (; i < vectors.Length; ++i) {
                minX = Math.Min(minX, vmin[i].X);
                maxX = Math.Max(maxX, vmax[i].X);

                minZ = Math.Min(minZ, vmin[i].Y);
                maxZ = Math.Max(maxZ, vmax[i].Y);

                maxY = Math.Min(maxY, vmin[i].Z);
                minY = Math.Max(minY, vmax[i].Z);
            }

            return new BoundingBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        }

        unsafe static BoundingBox ToBoundingBoxUnsafe(this Vector3[] points) {
            if (points == null || points.Length == 0) {
                return new BoundingBox();
            }

            var minX = float.MaxValue;
            var minY = float.MaxValue;
            var minZ = float.MaxValue;
            var maxX = float.MinValue;
            var maxY = float.MinValue;
            var maxZ = float.MinValue;
            fixed (Vector3* pArr = points) {
                Vector3* p = pArr;
                Vector3* pLast = pArr + points.Length;
                for (; p < pLast; ++p) {
                    var px = p->X;
                    var py = p->Y;
                    var pz = p->Z;
                    if (minX > px) {
                        minX = px;
                    }

                    if (minY > py) {
                        minY = py;
                    }

                    if (minZ > pz) {
                        minZ = pz;
                    }

                    if (maxX < px) {
                        maxX = px;
                    }

                    if (maxY < py) {
                        maxY = py;
                    }

                    if (maxZ < pz) {
                        maxZ = pz;
                    }
                }
            }
            return new BoundingBox(new Vector3(minX, minY, minZ), new Vector3(maxX, maxY, maxZ));
        }
    }
}
