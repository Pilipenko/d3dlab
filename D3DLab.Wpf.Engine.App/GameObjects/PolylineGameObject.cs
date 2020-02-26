using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.SDX.Engine.Components;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Components;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace D3DLab.Wpf.Engine.App.GameObjects {
    public class PolylineGameObject : Std.Engine.Core.GeometryGameObject {
        public ElementTag Tag { get; }

        public PolylineGameObject(ElementTag tag1):base("poly") {
            this.Tag = tag1;
        }

        public static PolylineGameObject Create(IEntityManager manager, ElementTag tag,
            IEnumerable<Vector3> points, Vector4[] color) {
            var indeces = new List<int>();
            for (var i = 0; i < points.Count(); i++) {
                indeces.AddRange(new[] { i, i });
            }
            var geo = new SimpleGeometryComponent() {
                Positions = points.ToImmutableArray(),
                Indices = indeces.ToImmutableArray(),
                Colors = color.ToImmutableArray(),
            };
            var tag1 = manager
               .CreateEntity(tag)
               .AddComponent(geo)
               .AddComponent(SDX.Engine.Components.D3DLineVertexRenderComponent.AsLineList())
               .AddComponent(new TransformComponent())
               //.AddComponent(new PositionColorsComponent { Colors = color })
               .Tag;

            return new PolylineGameObject(tag1);
        }

        public override void Cleanup(IEntityManager manager) {
            manager.RemoveEntity(Tag);
        }

        public override void Hide(IEntityManager manager) {
         
        }

        public override void Show(IEntityManager manager) {
         
        }
    }
}
