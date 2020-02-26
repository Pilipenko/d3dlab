using BepuPhysics;
using BepuPhysics.Collidables;
using D3DLab.ECS;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.Physics.Engine.Bepu {
    class StaticAABBPhysicalComponent : PhysicalComponent {

        public BoundingBox AABBox;

        public StaticAABBPhysicalComponent() {
            
        }

        internal override bool TryConstructBody(GraphicEntity entity, IPhysicsShapeConstructor constructor) {
            return constructor.TryConstructShape(entity, this);
        }
    }
    class StaticMeshPhysicalComponent : PhysicalComponent {

        public StaticMeshPhysicalComponent() {

        }

        internal override bool TryConstructBody(GraphicEntity entity, IPhysicsShapeConstructor constructor) {
            return constructor.TryConstructShape(entity, this);
        }
    }

    
}
