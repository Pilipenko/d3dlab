using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Utilities;
using D3DLab.Wpf.Engine.App.GameObjects;
using System.Numerics;

namespace D3DLab.Wpf.Engine.App {
    public class CoordinateSystemLinesGameObject : Std.Engine.Core.GeometryGameObject {
        public ElementTag Lines { get; private set; }
        public ElementTag[] Arrows { get; private set; }

        public static CoordinateSystemLinesGameObject Build(IEntityManager manager, Vector3 center = new Vector3()) {
            var llength = 100;
            var obj = new CoordinateSystemLinesGameObject();
            var points = new[] {
                center - Vector3.UnitX * llength, center + Vector3.UnitX * llength,
                center- Vector3.UnitY  * llength, center + Vector3.UnitY * llength,
                center- Vector3.UnitZ  * llength, center + Vector3.UnitZ * llength,
            };
            var color = new[] {
                V4Colors.Green, V4Colors.Green,
                V4Colors.Red, V4Colors.Red,
                V4Colors.Blue, V4Colors.Blue,
            };
            obj.Lines = PolylineGameObject.Create(manager, new ElementTag("Coordinate System"),
                points, color).Tag;
            var lenght = 20.0f;
            var radius = 5.0f;

            obj.Arrows = new ElementTag[3];
            obj.Arrows[0] = ArrowGameObject.Build(manager, new ArrowData {
                axis = Vector3.UnitZ,
                orthogonal = Vector3.UnitX,
                center = center + Vector3.UnitZ * (llength - lenght + 5),
                lenght = lenght,
                radius = radius,
                color = V4Colors.Blue,
                tag = new ElementTag("Arrow_Z")
            }).Tag;
            obj.Arrows[1] = ArrowGameObject.Build(manager, new ArrowData {
                axis = Vector3.UnitX,
                orthogonal = Vector3.UnitY,
                center = center + Vector3.UnitX * (llength - lenght + 5),
                lenght = lenght,
                radius = radius,
                color = V4Colors.Green,
                tag = new ElementTag("Arrow_X")
            }).Tag;
            obj.Arrows[2] = ArrowGameObject.Build(manager, new ArrowData {
                axis = Vector3.UnitY,
                orthogonal = Vector3.UnitZ,
                center = center + Vector3.UnitY * (llength - lenght + 5),
                lenght = lenght,
                radius = radius,
                color = V4Colors.Red,
                tag = new ElementTag("Arrow_Y")
            }).Tag;

            return obj;
        }

        public override void Show(IEntityManager manager) {
            foreach (var tag in Arrows) {
                manager.GetEntity(tag)
                    .GetComponent<IRenderableComponent>()
                    .CanRender = true;
            }
            manager.GetEntity(Lines)
                .GetComponent<IRenderableComponent>()
                .CanRender = true;
        }

        public override void Hide(IEntityManager manager) {
            foreach (var tag in Arrows) {
                manager.GetEntity(tag)
                    .GetComponent<IRenderableComponent>()
                    .CanRender = false;
            }
            manager.GetEntity(Lines)
                .GetComponent<IRenderableComponent>()
                .CanRender = false;
        }

        public CoordinateSystemLinesGameObject() : base(typeof(CoordinateSystemLinesGameObject).Name) {

        }
    }
}
