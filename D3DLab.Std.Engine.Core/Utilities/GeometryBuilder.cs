using D3DLab.ECS;
using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Ext;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;

namespace D3DLab.Std.Engine.Core.Utilities {
    public struct ArrowData {
        public ElementTag tag;
        public Vector3 axis;
        public Vector3 orthogonal;
        public Vector3 center;
        public Vector4 color;
        public float lenght;
        public float radius;
    }

    public struct SkyPlaneData {
        public static SkyPlaneData Default {
            get {
                return new SkyPlaneData {
                    PlaneResolution = 10,
                    PlaneWidth = 10.0f,
                    PlaneTop = 0.5f,
                    PlaneBottom = 0.0f,
                    TextureRepeat = 4
                };
            }
        }

        public int PlaneResolution;
        public float PlaneWidth;
        public float PlaneTop;
        public float PlaneBottom;
        public int TextureRepeat;
    }


    public class GeometryBuilder {

        public static IGeometryComponent BuildRotationOrbits(float radius, Vector3 center) {
            var step = 10f;

            var axises = new[] {
                new { asix = Vector3.UnitX, color =V4Colors.Green},
                new { asix = Vector3.UnitY, color = V4Colors.Red },
                new { asix = Vector3.UnitZ, color = V4Colors.Blue }
            };
            var lines = new List<Vector3>();
            var color = new List<Vector4>();
            foreach (var a in axises) {
                var axis = a.asix;
                var start = center + axis.FindAnyPerpendicular() * radius;
                var rotate = Matrix4x4.CreateFromAxisAngle(axis, step.ToRad());
                lines.Add(start);
                color.Add(a.color);
                for (var angle = step; angle < 360; angle += step) {
                    start = Vector3.Transform(start, rotate);
                    lines.Add(start);
                    color.Add(a.color);
                }
            }
            return new SimpleGeometryComponent {
                Colors = color.ToImmutableArray(),
                Positions = lines.ToImmutableArray(),
                Indices = ImmutableArray.Create<int>(),
            };
        }

        public static SimpleGeometryComponent BuildArrow(ArrowData data) {
            var axis = data.axis;
            var zero = Vector3.Zero;
            var lenght = data.lenght;
            var radius = data.radius;

            var rotate = Matrix4x4.CreateFromAxisAngle(axis, 10f.ToRad()); // Matrix4x4.CreateFromQuaternion(new Quaternion(axis,10));
            var move = Matrix4x4.CreateTranslation(data.center - zero);

            var points = new List<Vector3>();
            var normals = new List<Vector3>();
            var index = new List<int>();

            points.Add(zero + axis * lenght);
            normals.Add(axis);

            var orto = data.orthogonal;
            var corner = zero + orto * radius;
            normals.Add((corner - zero).Normalized());

            points.Add(corner);
            for (var i = 10; i < 360; i += 10) {
                corner = Vector3.Transform(corner, rotate);
                points.Add(corner);
                normals.Add((corner - zero).Normalized());
            }
            points.Add(zero);
            normals.Add(-axis);

            for (var i = 0; i < points.Count - 2; ++i) {
                index.AddRange(new[] { 0, i, i + 1 });
                index.AddRange(new[] { points.Count - 1, i, i + 1 });
            }
            index.AddRange(new[] { 0, points.Count - 2, 1 });
            index.AddRange(new[] { points.Count - 1, points.Count - 2, 1 });

            var pp = points.ToArray();
            points.Clear();
            pp.ForEach(x => points.Add(Vector3.Transform(x, move)));

            return new SimpleGeometryComponent {
                Positions = points.ToImmutableArray(),
                Indices = index.ToImmutableArray(),
                Normals = normals.ToImmutableArray(),
                //Color = data.color
            };
        }

        public static AbstractGeometry3D BuildSkyPlane(SkyPlaneData data) {
            var count = (data.PlaneResolution + 1) * (data.PlaneResolution + 1);
            var points = new Vector3[count];
            var tex = new Vector2[count];            

            // Determine the size of each quad on the sky plane.
            float quadSize = data.PlaneWidth / (float)data.PlaneResolution;
            // Calculate the radius of the sky plane based on the width.
            float radius = data.PlaneWidth / 2.0f;
            // Calculate the height constant to increment by.
            float constant = (data.PlaneTop - data.PlaneBottom) / (radius * radius);
            // Calculate the texture coordinate increment value.
            float textureDelta = (float)data.TextureRepeat / (float)data.PlaneResolution;

            // Loop through the sky plane and build the coordinates based on the increment values given.
            for (int j = 0; j <= data.PlaneResolution; j++) {
                for (int i = 0; i <= data.PlaneResolution; i++) {
                    // Calculate the vertex coordinates.
                    float positionX = (-0.5f * data.PlaneWidth) + ((float)i * quadSize);
                    float positionZ = (-0.5f * data.PlaneWidth) + ((float)j * quadSize);
                    float positionY = data.PlaneTop - (constant * ((positionX * positionX) + (positionZ * positionZ)));

                    // Calculate the texture coordinates.
                    float tu = (float)i * textureDelta;
                    float tv = (float)j * textureDelta;

                    // Calculate the index into the sky plane array to add this coordinate.
                    int index = j * (data.PlaneResolution + 1) + i;

                    // Add the coordinates to the sky plane array.
                    points[index] = new Vector3(positionX, positionY, positionZ);
                    tex[index] = new Vector2(tu, tv);
                }
            }
            var vertexCount = (data.PlaneResolution + 1) * (data.PlaneResolution + 1) * 6;

            var indices = new int[vertexCount];
            var positions = new Vector3[vertexCount];
            var texture = new Vector2[vertexCount];

            // Initialize the index into the vertex array.
            int indx = 0;
            // Load the vertex and index array with the sky plane array data.
            for (int j = 0; j < data.PlaneResolution; j++) {
                for (int i = 0; i < data.PlaneResolution; i++) {
                    int index1 = j * (data.PlaneResolution + 1) + i;
                    int index2 = j * (data.PlaneResolution + 1) + (i + 1);
                    int index3 = (j + 1) * (data.PlaneResolution + 1) + i;
                    int index4 = (j + 1) * (data.PlaneResolution + 1) + (i + 1);

                    // Triangle 1 - Upper Left
                    positions[indx] = points[index1];
                    texture[indx] = tex[index1];
                    indices[indx] = indx;
                    indx++;

                    // Triangle 1 - Upper Right
                    positions[indx] = points[index2];
                    texture[indx] = tex[index2];
                    indices[indx] = indx;
                    indx++;

                    // Triangle 1 - Bottom Left
                    positions[indx] = points[index3];
                    texture[indx] = tex[index3];
                    indices[indx] = indx;
                    indx++;

                    // Triangle 2 - Bottom Left
                    positions[indx] = points[index3];
                    texture[indx] = tex[index3];
                    indices[indx] = indx;
                    indx++;

                    // Triangle 2 - Upper Right
                    positions[indx] =points[index2];
                    texture[indx] = tex[index2];
                    indices[indx] = indx;
                    indx++;

                    // Triangle 2 - Bottom Right
                    positions[indx] =points[index4];
                    texture[indx] = tex[index4];
                    indices[indx] = indx;
                    indx++;
                }
            }

            return new AbstractGeometry3D {
                Positions = positions.ToList(),
                Indices = indices.ToList(),
                TextureCoordinates = texture.ToList(),
            };
        }

        public static AbstractGeometry3D BuildGeoBox(BoundingBox box) {
            var indx = new List<int>();
            var dic = new Dictionary<Vector3, int>();

            var corners = box.GetCorners();
            var index = 0;

            dic.Add(corners.FarBottomLeft, index);
            dic.Add(corners.FarBottomRight, ++index);
            dic.Add(corners.FarTopRight, ++index);
            dic.Add(corners.FarTopLeft, ++index);

            dic.Add(corners.NearBottomLeft, ++index);
            dic.Add(corners.NearBottomRight, ++index);
            dic.Add(corners.NearTopRight, ++index);
            dic.Add(corners.NearTopLeft, ++index);

            indx.AddRange(new[] {
                //top
                dic[corners.FarTopRight], dic[corners.FarTopLeft], dic[corners.NearTopRight],
                dic[corners.NearTopLeft],dic[corners.NearTopRight],  dic[corners.FarTopLeft],

                //Bottom
                dic[corners.FarBottomRight],  dic[corners.NearBottomRight], dic[corners.FarBottomLeft],
                dic[corners.NearBottomLeft], dic[corners.FarBottomLeft],dic[corners.NearBottomRight], 

                //left
                dic[corners.NearTopLeft], dic[corners.FarTopLeft], dic[corners.FarBottomLeft],
                dic[corners.NearBottomLeft],  dic[corners.NearTopLeft], dic[corners.FarBottomLeft],

                //right
                dic[corners.NearTopRight],  dic[corners.NearBottomRight],dic[corners.FarTopRight],
                dic[corners.NearBottomRight], dic[corners.FarBottomRight], dic[corners.FarTopRight],

                //near
                dic[corners.NearBottomLeft],  dic[corners.NearBottomRight], dic[corners.NearTopRight],
                dic[corners.NearBottomLeft],  dic[corners.NearTopRight], dic[corners.NearTopLeft],

                //far
                dic[corners.FarBottomLeft], dic[corners.FarTopRight], dic[corners.FarBottomRight],
                dic[corners.FarBottomLeft], dic[corners.FarTopLeft], dic[corners.FarTopRight],
            });


            return new AbstractGeometry3D() {
                Positions = dic.Keys.ToList(),
                Indices = indx,
            };
        }

        public static IEnumerable<Vector3> BuildBox(BoundingBox box) {
            var pos = new List<Vector3>();
            var corners = box.GetCorners();

            pos.Add(corners.FarBottomLeft);
            pos.Add(corners.FarBottomRight);

            pos.Add(corners.FarBottomRight);
            pos.Add(corners.FarTopRight);

            pos.Add(corners.FarTopRight);
            pos.Add(corners.FarTopLeft);

            pos.Add(corners.FarTopLeft);
            pos.Add(corners.FarBottomLeft);
            

            pos.Add(corners.NearBottomLeft);
            pos.Add(corners.NearBottomRight);

            pos.Add(corners.NearBottomRight);
            pos.Add(corners.NearTopRight);

            pos.Add(corners.NearTopRight);
            pos.Add(corners.NearTopLeft);

            pos.Add(corners.NearTopLeft);
            pos.Add(corners.NearBottomLeft);


            pos.Add(corners.NearBottomLeft);
            pos.Add(corners.FarBottomLeft);

            pos.Add(corners.NearBottomRight);
            pos.Add(corners.FarBottomRight);

            pos.Add(corners.NearTopRight);
            pos.Add(corners.FarTopRight);

            pos.Add(corners.NearTopLeft);
            pos.Add(corners.FarTopLeft);

            return pos;
        }
    }
}
