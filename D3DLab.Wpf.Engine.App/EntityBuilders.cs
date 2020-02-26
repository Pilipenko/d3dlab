using D3DLab.ECS;
using D3DLab.ECS.Ext;
using D3DLab.ECS.Components;
using D3DLab.SDX.Engine.Components;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Ext;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace D3DLab.Wpf.Engine.App {
    public static class EntityBuilders {
        public static ElementTag BuildMeshElement(this IEntityManager manager, List<Vector3> pos, List<int> indexes, Vector4 color) {
            return Build(manager, pos, indexes, pos.Select(x => color).ToList());
        }

        public static ElementTag BuildGroupMeshElement(this IEntityManager manager, IEnumerable<AbstractGeometry3D> objs) {
            var group = new Std.Engine.Core.Components.CompositeGeometryComponent();
            objs.ForEach(x => group.Add(new SimpleGeometryComponent {
                Positions = x.Positions.ToImmutableArray(),
                Indices = x.Indices.ToImmutableArray(),
                Normals = x.Normals.ToImmutableArray(),
                Color = x.Color
            }));
            group.Combine();
            return manager
                .CreateEntity(new ElementTag("GroupGeometry" + Guid.NewGuid()))
                .AddComponent(group)
                .AddComponent(new D3DTriangleColoredVertexRenderComponent())
                .AddComponent(TransformComponent.Create(Matrix4x4.CreateTranslation(Vector3.UnitY * 30)))
                .Tag;
        }

        public static ElementTag BuildLineEntity(this IEntityManager manager, Vector3[] points) {
            var geo = new SimpleGeometryComponent() {
                Positions = points.ToImmutableArray(),
                Indices = ImmutableArray.Create<int>(),
                Color = new Vector4(0, 1, 0, 1)
            };
            return manager
               .CreateEntity(new ElementTag("Points" + Guid.NewGuid()))
               .AddComponent(geo)
               .AddComponent(new SDX.Engine.Components.D3DLineVertexRenderComponent())
               .Tag;
        }


        #region components 
        public static IRenderableComponent GetObjGroupsRender() {
            return D3DTriangleColoredVertexRenderComponent.AsTriangleListCullNone();
        }

        public static IRenderableComponent GetRenderAsTriangleColored() {
            return new SDX.Engine.Components.D3DTriangleColoredVertexRenderComponent();
        }
        public static TransformComponent GetTransformation() {
            return TransformComponent.Identity();
        }

        #endregion

        public static ElementTag Build(IEntityManager manager, List<Vector3> pos, List<int> indexes, List<Vector4> colors) {
            var geo = new SimpleGeometryComponent() {
                Positions = pos.ToImmutableArray(),
                Indices = indexes.ToImmutableArray(),
                Colors = colors.ToImmutableArray(),
                Normals = pos.CalculateNormals(indexes).ToImmutableArray()
            };
            return manager
                .CreateEntity(new ElementTag("Geometry" + Guid.NewGuid()))
                .AddComponent(geo)
                .AddComponent(TransformComponent.Identity())
                .AddComponent(GetObjGroupsRender())
                .Tag;
        }
    }
}
