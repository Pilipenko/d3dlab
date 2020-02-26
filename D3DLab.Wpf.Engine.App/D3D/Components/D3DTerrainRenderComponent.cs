using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;
using System.Threading.Tasks;
using D3DLab.ECS;
using D3DLab.ECS.Common;
using D3DLab.SDX.Engine.Components;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Systems;
using D3DLab.Std.Engine.Core.Utilities;
using SharpDX.Direct3D11;

namespace D3DLab.Wpf.Engine.App.D3D.Components {

    public class D3DTerrainCellRenderComponent : D3DRenderComponent {
        public int IndexCount { get; set; }
    }

    class CacheArray<T> : IReadOnlyList<T> {
        readonly IList<T> origin;
        readonly int[] origIndexes;

        public T this[int index] {
            get {
                var origIndex = origIndexes[index];
                return origin[origIndex];
            }
        }

        public int Count => origIndexes.Length;

        public CacheArray(IList<T> origin, int[] origIndexes) {
            this.origin = origin;
            this.origIndexes = origIndexes;
        }

        public IEnumerator<T> GetEnumerator() {
            for (var i = 0; i < origIndexes.Length; i++) {
                var orig = origIndexes[i];
                yield return origin[orig];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    public class TerrainGeometryCellsComponent : HittableGeometryComponent, IHeightMapSourceComponent {

        public class TerrainCell {
            public int VertexCount { get; set; }
            public int IndexCount { get; set; }

            public readonly IReadOnlyList<Vector3> Positions;            
            public readonly IReadOnlyList<Vector3> Normals;
            public readonly IReadOnlyList<Vector3> Tangents;
            public readonly IReadOnlyList<Vector3> Binormal;
            public readonly IReadOnlyList<Vector4> Colors;
            public readonly IReadOnlyList<Vector2> TextureCoordinates;
            public readonly IReadOnlyList<Vector2> NormalMapTexCoordinates;

            readonly int[] OrigIndices;
            public readonly int[] Indices;
            public readonly GeometryTree Tree;

            public TerrainCell(int[] origIndices, int[] indices, TerrainGeometryCellsComponent full) {
                OrigIndices = origIndices;
                Indices = indices;
                Tree = new GeometryTree();

                Positions = new CacheArray<Vector3>(full.Positions, OrigIndices);
                Normals = new CacheArray<Vector3>(full.Normals, OrigIndices);
                Tangents = new CacheArray<Vector3>(full.Tangents, OrigIndices);
                Binormal = new CacheArray<Vector3>(full.Binormal, OrigIndices);
                Colors = new CacheArray<Vector4>(full.Colors, OrigIndices);
                TextureCoordinates = new CacheArray<Vector2>(full.TextureCoordinates, OrigIndices);
                NormalMapTexCoordinates = new CacheArray<Vector2>(full.NormalMapTexCoordinates, OrigIndices);
            }
        }

        public Vector2[] NormalMapTexCoordinates { get; set; }

        public float MaxHeight { get; set; }
        public float MinHeight { get; set; }

        public TerrainCell[] Cells;

        Task building;
        public TerrainGeometryCellsComponent() {
            building = Task.FromResult(0);
        }

        public TerrainCell BuildCell(int nodeIndexX, int nodeIndexY, int cellHeight, int cellWidth, int terrainWidth) {
            
            // Calculate the number of vertices in this terrain cell.
            var vCount = (cellHeight - 1) * (cellWidth - 1) * 6;

            // Set the index count to the same as the vertex count.
            var origIndices = new int[vCount];
            var indices = new int[vCount];

            // Setup the indexes into the terrain model data and the local vertex/index array.
            int modelIndex = ((nodeIndexX * (cellWidth - 1)) + (nodeIndexY * (cellHeight - 1) * (terrainWidth - 1))) * 6;
            int index = 0;

            var vertex = new Vector3[vCount];
            var normals = new Vector3[vCount];

            for (int j = 0; j < (cellHeight - 1); j++) {
                for (int i = 0; i < ((cellWidth - 1) * 6); i++) {
                    try {
                        var indice = Indices[modelIndex];

                        vertex[index] = Positions[indice];
                        normals[index] = Normals[indice];

                        indices[index] = index;
                        origIndices[index] = indice;
                    }catch(Exception ex) {
                        ex.ToString();
                    }
                    modelIndex++;
                    index++;
                }
                modelIndex += (terrainWidth * 6) - (cellWidth * 6);
            }

            var cell = new TerrainCell(origIndices, indices, this) {
                IndexCount = vCount,
                VertexCount = vCount
            };

            building = building.ContinueWith(x=> cell.Tree.BuildTreeAsync(vertex, normals, indices)).Unwrap();

            return cell;
        }

        public Matrix4x4 GetTransfromToMap(ref Ray rayLocal) {
            var m = Matrix4x4.Identity;

            if (!Tree.IsBuilt) {
                return m;
            }

            var moveto = rayLocal.Origin;

            var hit = Tree.HitLocalBy(rayLocal);
            if (hit.IsHitted) {
                moveto.Y = hit.Point.Y;
                return Matrix4x4.CreateTranslation(moveto - rayLocal.Origin);
            }
            hit = Tree.HitLocalBy(rayLocal.Inverted());
            if (hit.IsHitted) {
                moveto.Y = hit.Point.Y;
                return Matrix4x4.CreateTranslation(moveto - rayLocal.Origin);
            }

            return m;
        }

        protected override BoundingBox CalcuateBox() {
            return base.CalcuateBox();
        }
    }


    public class D3DTerrainRenderComponent : D3DRenderComponent {
        [IgnoreDebuging]
        internal EnumerableDisposableSetter<ShaderResourceView[]> TextureResources { get; set; }
        [IgnoreDebuging]
        internal DisposableSetter<SamplerState> SampleState { get; set; }

        internal readonly EnumerableDisposableSetter<SharpDX.Direct3D11.Buffer[]> VertexBuffers;
        internal readonly EnumerableDisposableSetter<SharpDX.Direct3D11.Buffer[]> IndexBuffers;

        public D3DTerrainRenderComponent() {
            VertexBuffers = new EnumerableDisposableSetter<SharpDX.Direct3D11.Buffer[]>(disposer);
            IndexBuffers = new EnumerableDisposableSetter<SharpDX.Direct3D11.Buffer[]>(disposer);
            SampleState = new DisposableSetter<SamplerState>(disposer);
            TextureResources = new EnumerableDisposableSetter<ShaderResourceView[]>(disposer);
        }

        public override void Dispose() {
            base.Dispose();
        }
    }

    class ClipmapsTerrainComponent : GraphicComponent {
        public float ZScaleFactor;
        public Vector4 ScaleFactor;
        public Vector4 FineTextureBlockOrigin;
        public Vector2 AlphaOffset;
        public float OneOverWidth;
    }

}
