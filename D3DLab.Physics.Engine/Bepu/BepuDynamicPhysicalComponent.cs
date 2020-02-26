using BepuPhysics;
using BepuPhysics.Collidables;
using D3DLab.ECS;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.Physics.Engine.Bepu {
    interface IBepuDynamicPhysicalComponent : IGraphicComponent {
        BoundingBox AABBox { get; }
    }
    class DynamicAABBPhysicalComponent : PhysicalComponent , IBepuDynamicPhysicalComponent {

        public BoundingBox AABBox { get; set; }

        public DynamicAABBPhysicalComponent() {
        }

        public override void Dispose() {

            base.Dispose();
        }

        internal override bool TryConstructBody(GraphicEntity entity, IPhysicsShapeConstructor constructor) {
            return constructor.TryConstructShape(entity, this);
            //var sphere = new Sphere(box.Size().X);
            //var size = box.Size();
            //var fbox = new Box(size.X, size.Y, size.Z);
            //fbox.ComputeInertia(1, out var sphereInertia);

            //var t = simulation.Shapes.Add(fbox);
            //ShapeIndex = t.Index;

            //simulation.Bodies.Add(BodyDescription.CreateDynamic(
            //    box.GetCenter(),
            //    sphereInertia,
            //    new CollidableDescription(t, 0.1f),
            //    new BodyActivityDescription(0.01f)));

            //var position = new Vector3();
            //var orientation = BepuUtilities.Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), 0);
            //var pose = new RigidPose(position, orientation);
            //var ringBoxShape = new Box(0.5f, 1, 3);
            //ringBoxShape.ComputeInertia(1, out var ringBoxInertia);
            //var boxDescription = BodyDescription.CreateDynamic(new Vector3(), ringBoxInertia,
            //    new CollidableDescription(simulation.Shapes.Add(ringBoxShape), 0.1f),
            //    new BodyActivityDescription(0.01f));

            //Data.Body = simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, bodyInertia, new CollidableDescription(bodyShape, 0.1f), new BodyActivityDescription(0.01f)));

        }
    }

    class DynamicMeshPhysicalComponent : PhysicalComponent, IBepuDynamicPhysicalComponent {
        public BoundingBox AABBox { get; set; }
        public DynamicMeshPhysicalComponent() {

        }

        internal override bool TryConstructBody(GraphicEntity entity, IPhysicsShapeConstructor constructor) {
            return constructor.TryConstructShape(entity, this);
        }
    }
}
