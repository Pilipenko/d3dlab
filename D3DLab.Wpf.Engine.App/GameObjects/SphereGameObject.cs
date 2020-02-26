using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.SDX.Engine.Components;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using System;
using System.Collections.Immutable;
using System.Numerics;

namespace D3DLab.Wpf.Engine.App.GameObjects {
    public class SphereGameObject : SingleGameObject {
        public struct Data {
            public Vector3 Center;
            public Vector4 Color;
            public float Radius;
        }

        public SphereGameObject(ElementTag tag) : base(tag, "SphereByPoint") {
        }

        public static SphereGameObject Create(IEntityManager manager) {
            var tag = manager
               .CreateEntity(new ElementTag("Sphere_" + DateTime.Now.Ticks))
               .AddComponent(new SimpleGeometryComponent() {
                   Indices = new[] { 0 }.ToImmutableArray(),
                   Positions = new Vector3[] { Vector3.Zero }.ToImmutableArray(),
                   Color = V4Colors.Red,
               })
               .AddComponent(new D3DSpherePointRenderComponent())
               .AddComponent(new TransformComponent())
               .Tag;


            return new SphereGameObject(tag);
        }
        public static SphereGameObject Create(IEntityManager manager, Data data) {
            var tag = manager
               .CreateEntity(new ElementTag("Sphere_" + DateTime.Now.Ticks))
               .AddComponent(new SimpleGeometryComponent() {
                   Indices = new[] { 0 }.ToImmutableArray(),
                   Positions = new Vector3[] { data.Center }.ToImmutableArray(),
                   Color = data.Color,
               })
               .AddComponent(new D3DSpherePointRenderComponent())
               .AddComponent(TransformComponent.Identity())
               .Tag;


            return new SphereGameObject(tag);
        }
    }
}
