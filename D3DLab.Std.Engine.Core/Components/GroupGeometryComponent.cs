using D3DLab.ECS;
using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace D3DLab.Std.Engine.Core.Components {
    public interface IGeometryComponent : IGraphicComponent {
        void MarkAsRendered();

        ImmutableArray<Vector3> Positions { get; }
        ImmutableArray<Vector3> Normals { get; }
        ImmutableArray<int> Indices { get; }
        ImmutableArray<Vector4> Colors { get; }

        ImmutableArray<Vector2> TextureCoordinates { get; }

        BoundingBox Box { get; }
    }

    public class CompositeGeometryComponent : GraphicComponent, ICollection<GeometryComponent>, IGeometryComponent {

        public ImmutableArray<Vector3> Positions { get; private set; }
        public ImmutableArray<int> Indices { get; private set; }
        public ImmutableArray<Vector4> Colors { get; private set; }
        public ImmutableArray<Vector3> Normals { get; private set; }
        public ImmutableArray<Vector2> TextureCoordinates { get; private set; }

        public BoundingBox Box => new BoundingBox();//TODO: !

        readonly List<GeometryComponent> groups;


        public int Count => groups.Count;

        public bool IsReadOnly => false;

        public CompositeGeometryComponent() {
            IsModified = true;
            groups = new List<GeometryComponent>();

        }

        public void Combine() {
            var value = new Utilities.Helix.MeshBuilder(true, false);
            groups.ForEach(x => value.Append(x));
            var combined = value.ToGeometry3D();
            Positions = combined.Positions.ToImmutableArray();
            Indices = combined.Indices.ToImmutableArray();
            Colors = combined.Colors.ToImmutableArray();
            Normals = combined.Normals.ToImmutableArray();
        }

        public void Add(GeometryComponent item) {
            groups.Add(item);
        }

        public void Clear() {
            throw new System.NotImplementedException();
        }

        public bool Contains(GeometryComponent item) {
            throw new System.NotImplementedException();
        }

        public void CopyTo(GeometryComponent[] array, int arrayIndex) {
            throw new System.NotImplementedException();
        }

        public bool Remove(GeometryComponent item) {
            throw new System.NotImplementedException();
        }

        public IEnumerator<GeometryComponent> GetEnumerator() {
            throw new System.NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new System.NotImplementedException();
        }

        public void MarkAsRendered() {
            IsModified = false;
        }
    }

    public class VirtualGroupGeometryComponent : HittableGeometryComponent, IGeometryComponent {
        public readonly PartGeometry3D PartGeometry;

        public override ImmutableArray<Vector4> Colors {
            get {
                if (colors.IsEmpty) {
                    var colors = new Vector4[Positions.Length];
                    for (var i = 0; i < colors.Length; i++) {
                        colors[i] = V4Colors.Red; //part.Color;
                    }
                    //foreach (var part in Combined.Parts) {
                    //    for (var i = 0; i < part.PosGroupInfo.Count; i++) {
                    //        colors[part.PosGroupInfo.StartIndex + i] = V4Colors.Red; //part.Color;
                    //    }
                    //}
                    this.colors = colors.ToImmutableArray();
                }
                return colors;
            }
            set { }
        }

        ImmutableArray<Vector4> colors;

        public VirtualGroupGeometryComponent(PartGeometry3D part) {
            this.PartGeometry = part;
            Positions = part.Positions.ToImmutableArray();
            Normals = part.Positions.ToArray().CalculateNormals(part.Indices.ToArray()).ToImmutableArray();
            Indices = part.Indices.ToImmutableArray();
            colors = ImmutableArray<Vector4>.Empty;
        }
        
    }
}
