using D3DLab.ECS;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Utilities;
using g3;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace D3DLab.Std.Engine.Core.Components {
    public struct HitResultLocal {
        public Vector3 Point;
        public float Distance;
        public bool IsHitted;
    }

    public class GeometryTree {
        public bool IsBuilt { get; private set; }

        DMeshAABBTree3 TreeLocal;
        DMesh3 DMeshLocal;
        
        public Task BuildTreeAsync(IGeometryComponent geo) {
            if (!geo.IsValid) { return Task.FromResult(0); }
            return Task.Run(() => {
                var norm = geo.Normals.IsDefaultOrEmpty ? null : ConvertToVector3f(geo.Normals);
                DMeshLocal = DMesh3Builder.Build(ConvertToVector3f(geo.Positions), geo.Indices, norm);
                //var sm = new LaplacianMeshSmoother(DMesh);
                //sm.Initialize();
                //sm.SolveAndUpdateMesh();

                //DMesh = sm.Mesh;
                TreeLocal = new DMeshAABBTree3(DMeshLocal);
                TreeLocal.Build();

                box = new BoundingBox(DMeshLocal.GetBounds());

                IsBuilt = true;

                return this;
            });
        }
        public Task BuildTreeAsync(Vector3[] positions, Vector3[] normals, int[] indices) {
            return Task.Run(() => {
                var norm = ConvertToVector3f(normals);
                DMeshLocal = DMesh3Builder.Build(ConvertToVector3f(positions), indices, norm);

                TreeLocal = new DMeshAABBTree3(DMeshLocal);
                TreeLocal.Build();

                box = new BoundingBox(DMeshLocal.GetBounds());

                IsBuilt = true;

                return this;
            });
        }

        BoundingBox box = BoundingBox.Zero;
        public BoundingBox GetBounds() {
            return box;
        }

        static Vector3f[] ConvertToVector3f(ImmutableArray<Vector3> pos) {
            var v3f = new Vector3f[pos.Length];
            for (var i = 0; i < v3f.Length; ++i) {
                v3f[i] = pos[i].ToVector3f();
            }
            return v3f;
        }
        static Vector3f[] ConvertToVector3f(Vector3[] pos) {
            var v3f = new Vector3f[pos.Length];
            for (var i = 0; i < v3f.Length; ++i) {
                v3f[i] = pos[i].ToVector3f();
            }
            return v3f;
        }

        public HitResultLocal HitLocalBy(Ray rayLocal) {
            var res = new HitResultLocal();
            try {
                int hit_tid = TreeLocal.FindNearestHitTriangle(rayLocal.g3Rayf);
                if (hit_tid == DMesh3.InvalidID) {
                    return res;
                }

                var intr = MeshQueries.TriangleIntersection(DMeshLocal, hit_tid, rayLocal.g3Rayf);
                res.Distance = (float)rayLocal.g3Rayd.Origin.Distance(rayLocal.g3Rayd.PointAt(intr.RayParameter));
                res.Point = intr.Triangle.V1.ToVector3();
                res.IsHitted = true;
            }catch(Exception ex) {
                System.Diagnostics.Trace.WriteLine(ex.Message);
            }

            return res;
        }
    }

    public class HittableGeometryComponent : GeometryComponent, IGeometryComponent {
        
        public GeometryTree Tree { get; }

        public HittableGeometryComponent() {
            Tree = new GeometryTree();
        }

        internal Task<HittableGeometryComponent> BuildTreeAsync() {
            if (!IsValid) { return Task<HittableGeometryComponent>.FromResult(this); }
            return Tree.BuildTreeAsync(this).ContinueWith(x => this);
        }

        protected override BoundingBox CalcuateBox() {
            return Tree.IsBuilt ? Tree.GetBounds() : BoundingBox.Zero;
        }        
    }
    /*
    public class GeometryComponent : BaseGeometryComponent, IGeometryComponent {

        //TODO: maybe need to separate DMesh functional to HittableGeometryComponent
        #region geometry3Sharp 

        internal bool IsBuilt { get; private set; }
        internal DMeshAABBTree3 Tree { get; private set; }
        internal DMesh3 DMesh { get; private set; }

        internal Task<GeometryComponent> BuildTreeAsync() {
            if (!IsValid) { return Task<GeometryComponent>.FromResult(this); }
            return Task.Run(() => {
                var norm = Normals.IsDefaultOrEmpty ? null : ConvertToVector3f(Normals);
                DMesh = DMesh3Builder.Build(ConvertToVector3f(Positions), Indices, norm);
                Tree = new DMeshAABBTree3(DMesh);
                Tree.Build();
                IsBuilt = true;
                return this;
            });
        }
        #endregion

        protected override BoundingBox CalcuateBox() {
            return IsBuilt ? BoundingBox.CreateFromComponent(this) : BoundingBox.Empty;
        }

        static Vector3f[] ConvertToVector3f(ImmutableArray<Vector3> pos) {
            var v3f = new Vector3f[pos.Length];
            for(var i=0;i < v3f.Length; ++i) {
                v3f[i] = pos[i].ToVector3f();
            }
            return v3f;
        }
    }*/

    public class SimpleGeometryComponent : GeometryComponent, IGeometryComponent {
        protected override BoundingBox CalcuateBox() {
            return BoundingBox.CreateFromVertices(Positions.ToArray());
        }
    }


    public abstract class GeometryComponent : GraphicComponent {
        public override bool IsValid => Positions.Length > 0 && Indices.Length > 0;

        public virtual ImmutableArray<Vector3> Positions { get; set; }
        public virtual ImmutableArray<Vector3> Normals { get; set; }
        public virtual ImmutableArray<Vector4> Colors {
            get {
                if (!colors.Any()) {
                    colors = Positions.Length.SelectToList(() => Color).ToImmutableArray();
                }
                return colors;
            }
            set {
                colors = value;
            }
        }
        public ImmutableArray<Vector2> TextureCoordinates { get; set; }
        public ImmutableArray<int> Indices { get; set; }

        public List<Vector3> Tangents { get; set; }
        public List<Vector3> Binormal { get; set; }

        [Obsolete("MOve to color component")]
        public Vector4 Color { get; set; }

        public BoundingBox Box => CalcuateBox();

        ImmutableArray<Vector4> colors;
        readonly Lazy<BoundingBox> box;

        public GeometryComponent() {
            colors = ImmutableArray<Vector4>.Empty;
            TextureCoordinates = ImmutableArray<Vector2>.Empty;
            IsModified = true;
            //
           // box = new Lazy<BoundingBox>(CalcuateBox);
        }

        protected abstract BoundingBox CalcuateBox();

        public void MarkAsRendered() {
            IsModified = false;
        }        
    }
}
