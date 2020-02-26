using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.SDX.Engine.Components;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Utilities;
using System.Numerics;

namespace D3DLab.Wpf.Engine.App.GameObjects {
    public class OrbitsRotationGameObject {
        public static OrbitsRotationGameObject Create(IEntityManager manager) {
            var geo = GeometryBuilder.BuildRotationOrbits(100, Vector3.Zero);
            var tag = manager
               .CreateEntity(new ElementTag("OrbitsRotationGameObject"))
               .AddComponent(geo)
               .AddComponent(D3DLineVertexRenderComponent.AsLineStrip())
               .AddComponent(new TransformComponent())
               .Tag;

            return new OrbitsRotationGameObject();
        }
    }
}
