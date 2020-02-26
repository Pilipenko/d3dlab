using D3DLab.ECS;
using D3DLab.Std.Engine.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace D3DLab.Debugger.Infrastructure {
    public interface IVisualEntityItem {

    }
    public interface IVisualComponentItem {
        event PropertyChangedEventHandler PropertyChanged;
        ElementTag Guid { get; }
        string Name { get; }
        string Value { get; }

        IGraphicComponent GetOriginComponent();

        void Refresh();
        void MarkAsModified();
    }

    public interface IVisualTreeEntityItem {
        ElementTag Name { get; }
        ObservableCollection<IVisualComponentItem> Components { get; }
        void Add(IVisualComponentItem com);
        void Remove(IVisualComponentItem com);
        void Clear();
        bool TryRefresh(IGraphicComponent com);
        //void Refresh();


        //ICommand OpenShaderEditor { get; }
    }
}
