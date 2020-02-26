using D3DLab.ECS;
using D3DLab.Std.Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace D3DLab {
    public sealed class ViewportSubscriber :
        IManagerChangeSubscriber<GraphicEntity>,
        IManagerChangeSubscriber<IGraphicSystem>,
        IEntityRenderSubscriber {
        private readonly MainWindowViewModel mv;

        public ViewportSubscriber(MainWindowViewModel mv) {
            this.mv = mv;
        }

        public void Change(GraphicEntity entity) {
            var en = new Debugger.Windows.GraphicEntityDecorator(entity);
            App.Current.Dispatcher.BeginInvoke(new Action(() => {
                mv.VisualTreeviewer.Change(en);
            }));
        }

        public void Change(IGraphicSystem sys) {
            App.Current.Dispatcher.BeginInvoke(new Action(() => {
                mv.SystemsView.AddSystem(sys);
            }));
        }

        public void Render(IEnumerable<GraphicEntity> entities) {
            if (App.Current == null) { return; }
            var en = entities.Select(x => new Debugger.Windows.GraphicEntityDecorator(x)).ToList();
            App.Current.Dispatcher.BeginInvoke(new Action(() => {
                mv.VisualTreeviewer.Refresh(en);
            }));
        }
    }


}
