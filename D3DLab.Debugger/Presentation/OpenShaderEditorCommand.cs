using D3DLab.Debugger.Windows;
using D3DLab.ECS.Shaders;
using D3DLab.Std.Engine.Core.Shaders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D3DLab.Debugger.Presentation {
    public abstract class OpenShaderEditorCommand<T> : BaseWPFCommand<T> {
        readonly IRenderUpdater updater;

        public OpenShaderEditorCommand(IRenderUpdater updater) {
            this.updater = updater;
        }

        protected abstract IShadersContainer Convert(T i);

        public override void Execute(T item) {
            var editable = Convert(item);
            var win = new Avalonia.ShaderEditorPopup();
            win.ViewModel.LoadShader(editable.Pass, editable.GetCompilator(), updater);
            win.Show();
        }
    }
}
