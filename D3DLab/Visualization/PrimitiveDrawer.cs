using D3DLab.Debugger;
using D3DLab.ECS;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Wpf.Engine.App;
using D3DLab.Wpf.Engine.App.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace D3DLab.Visualization {
    public class PrimitiveDrawer : IPrimitiveDrawer {
        IContextState context;
        public PrimitiveDrawer() {

        }

        #region IPrimitiveDrawer

        public void arrow(Vector3 start, Vector3 direction) {
            ArrowGameObject.Build(context.GetEntityManager(), new Std.Engine.Core.Utilities.ArrowData {
                center = start,
                axis = direction,
                orthogonal = Vector3.Zero,
                lenght = 20,
                color = V4Colors.Green,
                radius = 2,
                tag = new ElementTag("Arrow_" + DateTime.Now.Ticks)
            });
        }

        public void point(params Vector3[] points) {
            point(1, points);
        }

        public void point(float radius, params Vector3[] points) {
            foreach (var p in points) {
                SphereGameObject.Create(context.GetEntityManager());
            }
        }

        public void poly(params Vector3[] points) {
            var colors = new List<Vector4>();
            foreach(var p in points) {
                colors.Add(V4Colors.Red);
            }
            PolylineGameObject.Create(context.GetEntityManager(), 
                new ElementTag("Poly_" + DateTime.Now.Ticks), points, colors.ToArray());
        } 

        #endregion

        internal void SetContext(IContextState context) {
            this.context = context;
        }
    }
}
