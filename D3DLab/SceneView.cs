using D3DLab.ECS;
using D3DLab.ECS.Context;
using D3DLab.Std.Engine.Core;
using D3DLab.Wpf.Engine.App.Host;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;

namespace D3DLab {
    public sealed class SceneView : Wpf.Engine.App.Scene {

        public SceneView(FormsHost host, FrameworkElement overlay, ContextStateProcessor context, EngineNotificator notify)
            : base(host, overlay, context, notify) {


            //try {
            //    Fwk.ImageSharp.ImagePr.Load(Path.Combine(AppContext.BaseDirectory, "Textures", "spnza_bricks_a_diff.png"));
            //} catch (Exception ex) {
            //    ex.ToString();
            //}

            //var center = new Vector3();
            //var point = new Vector3(10, 10, 10);
            //var res = point + center;

            //var v = new Vector3(10, 10, 10) + new Vector3(5, 20, 0);
            //var v = new Vector3(5, 20, 0) - new Vector3(10, 10, 10);
            //var normal = v;
            //normal.Normalize();

            //var point1 = new Vector3(5, 20, 0) - normal * v.Length()/2;
            //var point2 = new Vector3(10, 10, 10) + normal * v.Length() / 2;



        }

        public class LineBuilder {
            private readonly List<Vector3> positions;
            private readonly List<int> lineListIndices;

            public LineBuilder() {
                positions = new List<Vector3>();
                lineListIndices = new List<int>();
            }

            public void Build(IEnumerable<Vector3> points, bool closed = false) {
                var first = positions.Count;
                positions.AddRange(points);
                var lineCount = positions.Count - first - 1;

                for (var i = 0; i < lineCount; i++) {
                    lineListIndices.Add(first + i);
                    lineListIndices.Add(first + i + 1);
                }

                if (closed) {
                    lineListIndices.Add(positions.Count - 1);
                    lineListIndices.Add(first);
                }
            }

        }



    }


}
