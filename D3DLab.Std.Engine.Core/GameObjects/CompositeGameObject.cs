using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Components.Movements;
using D3DLab.Std.Engine.Core.Utilities;

namespace D3DLab.Std.Engine.Core.GameObjects {
    public class CompositeGameObject : GeometryGameObject {
        public List<ElementTag> Tags { get; }

        public CompositeGameObject(IEnumerable<ElementTag> tags) : this(tags, "Group") {
        }
        public CompositeGameObject(string desc) : this(new ElementTag[0], desc) {
        }
        public CompositeGameObject(IEnumerable<ElementTag> tags, string desc) : base(desc) {
            Tags = new List<ElementTag>(tags);
        }

        public void AddEntity(ElementTag tag) {
            Tags.Add(tag);
        }
        public void RemoveEntity(ElementTag tag) {
            Tags.Remove(tag);
        }

        public override void Hide(IEntityManager manager) {
            foreach (var tag in Tags) {
                manager.GetEntity(tag)
                      .GetComponent<IRenderableComponent>()
                      .CanRender = false;
            }
        }

        public override void Show(IEntityManager manager) {
            foreach (var tag in Tags) {
                manager.GetEntity(tag)
                  .GetComponent<IRenderableComponent>()
                  .CanRender = true;
            }
        }

        public override void LookAtSelf(IEntityManager manager) {
            var combinedBox = new BoundingBox();
            GraphicEntity entity = null;
            foreach (var tag in Tags) {
                entity = manager.GetEntity(tag);
                if (entity.GetComponents<IRenderableComponent>().Any(x => x.CanRender)) {
                    var geos = entity.GetComponents<IGeometryComponent>();
                    if (geos.Any()) {
                        var geo = geos.First();
                        combinedBox = combinedBox.Merge(geo.Box);
                    }
                }
            }

            var com = new MoveCameraToTargetComponent { Target = entity.Tag, TargetPosition = combinedBox.GetCenter() };
            entity.AddComponent(com);
        }

        public override void Cleanup(IEntityManager manager) {
            base.Cleanup(manager);
            foreach (var tag in Tags) {
                manager.RemoveEntity(tag);
            }
        }

        public override IEnumerable<GraphicEntity> GetEntities(IEntityManager manager) {
            return Tags.Select(x => manager.GetEntity(x));
        }
    }
}
