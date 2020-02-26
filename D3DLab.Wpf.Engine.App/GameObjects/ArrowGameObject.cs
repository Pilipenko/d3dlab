using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.SDX.Engine.Components;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Utilities;
using System;

namespace D3DLab.Wpf.Engine.App {
    public class ArrowGameObject {
        public ElementTag Tag { get; }

        public ArrowGameObject(ElementTag tag) {
            Tag = tag;
        }

        public static ArrowGameObject Build(IEntityManager manager, ArrowData data) {
            var geo = GeometryBuilder.BuildArrow(data);
            geo.Color = data.color;
            var en = manager
              .CreateEntity(data.tag)
              .AddComponents(
                geo,
                D3DTriangleColoredVertexRenderComponent.AsStrip(),
                new TransformComponent()
                );

            return new ArrowGameObject(en.Tag);
        }

        [Obsolete("Remove")]
        public static ElementTag BuildArrow(IEntityManager manager, ArrowData data) {
            var geo = GeometryBuilder.BuildArrow(data);
            return manager
               .CreateEntity(data.tag)
               .AddComponent(geo)
               .AddComponent(D3DTriangleColoredVertexRenderComponent.AsStrip())
               .AddComponent(new TransformComponent())
               .Tag;
        }
    }
}
