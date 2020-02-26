using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.ECS.Ext;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Input.Commands;
using D3DLab.Std.Engine.Core.Utilities;
using g3;

namespace D3DLab.Std.Engine.Core.Systems {

    public interface ICollidableComponent : IGraphicComponent {
    }

    public class CollidingWithScreenRayComponent : GraphicComponent, ICollidableComponent {
        public Vector2 ScreenPosition { get; set; }
    }

    public class RayCollidedWithEntityComponent : GraphicComponent {
        public ElementTag With { get; set; }
        public Vector3 IntersectionPositionWorld { get; set; }
    }

    public class CollidingSystem : BaseEntitySystem, IGraphicSystem, IGraphicSystemContextDependent {
        public IContextState ContextState { get; set; }

        protected override void Executing(ISceneSnapshot ss) {
            var snapshot = (SceneSnapshot)ss;

            var colliding = new Colliding(snapshot, ContextState);
            var emanager = ContextState.GetEntityManager();

            foreach (var ev in snapshot.InputSnapshot.Events) {
                switch (ev) {
                    case CaptureTargetUnderMouseCameraCommand capture:
                        if (colliding.TryToColliding(capture.ScreenPosition, out var collidedWith)) {
                            var entity = emanager.GetEntity(collidedWith.With);
                            var has = entity.GetComponents<ManipulatableComponent>();
                            if (has.Any() && !entity.Has<CapturedToManipulateComponent>()) {
                                entity.AddComponent(new CapturedToManipulateComponent() {
                                    CapturePointWorld = collidedWith.IntersectionPositionWorld,
                                });
                                snapshot.InputSnapshot.RemoveEvent(ev);
                            }
                        }
                        break;                   
                }
            }

            foreach (var entity in emanager.GetEntities()) {
                foreach (var com in entity.GetComponents<ICollidableComponent>()) {
                    switch (com) {
                        case CollidingWithScreenRayComponent byRay:
                            try {
                                if (colliding.TryToColliding(byRay.ScreenPosition, out var collidedWith)) {
                                    entity.AddComponent(collidedWith);
                                }
                            } finally {
                                entity.RemoveComponent(byRay);
                            }
                            break;
                    }
                }
            }
        }

        class Colliding {
            readonly SceneSnapshot snapshot;
            readonly IContextState contextState;

            public Colliding(SceneSnapshot snapshot, IContextState contextState) {
                this.snapshot = snapshot;
                this.contextState = contextState;
            }

            public bool TryToColliding(Vector2 pos, out RayCollidedWithEntityComponent collided) {
                var rayWorld = snapshot.Viewport.UnProject(pos, snapshot.Camera, snapshot.Window);
                collided = new RayCollidedWithEntityComponent();

                var minDistance = double.MaxValue;

                Vector3 intersecWorld = Vector3.Zero;
                //find object
                var res = snapshot.Octree.GetColliding(rayWorld, tag => {
                    var entity = contextState.GetEntityManager().GetEntity(tag);

                    var renderable = entity.GetComponents<IRenderableComponent>().Any(x => x.CanRender);
                    if (!renderable) {
                        return false;
                    }

                    var geo = entity.GetComponent<HittableGeometryComponent>();
                    if (geo.IsNull() || !geo.Tree.IsBuilt) {
                        return false;
                    }

                    var hasTransform = entity.GetComponents<TransformComponent>();
                    var toLocal = Matrix4x4.Identity;
                    var toWorld = Matrix4x4.Identity;
                    var rayLocal = rayWorld;
                    if (hasTransform.Any()) {
                        toWorld = hasTransform.Single().MatrixWorld;
                        toLocal = toWorld.Inverted();
                        rayLocal = rayLocal.Transformed(toLocal);//to local
                    }

                    var hitlocal = geo.Tree.HitLocalBy(rayLocal);

                    //int hit_tid = geo.Tree.FindNearestHitTriangle(rayLocal.g3Rayf);
                    //if (hit_tid == DMesh3.InvalidID) {
                    //    return false;
                    //}
                    //var intr = MeshQueries.TriangleIntersection(geo.DMesh, hit_tid, rayLocal.g3Rayf);
                    //double hit_dist = rayLocal.g3Rayd.Origin.Distance(rayLocal.g3Rayd.PointAt(intr.RayParameter));

                    if (minDistance > hitlocal.Distance) {
                        minDistance = hitlocal.Distance;
                        //intersecWorld = intr.Triangle.V1.ToVector3();
                        intersecWorld = hitlocal.Point;
                        //to world
                        intersecWorld = intersecWorld.TransformedCoordinate(toWorld);
                        return true;
                    }
                    return false;
                });
                if (!res.Any() || intersecWorld.IsZero()) {
                    return false;
                }

                collided.IntersectionPositionWorld = intersecWorld;
                collided.With = res.First().Item;

                return true;
            }
        }


    }
}
