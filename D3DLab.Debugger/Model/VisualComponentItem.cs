using D3DLab.Debugger.Infrastructure;
using D3DLab.Debugger.Windows;
using D3DLab.ECS;
using D3DLab.ECS.Shaders;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Shaders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace D3DLab.Debugger.Model {
    public class VisualTreeItem : IVisualTreeEntityItem {
        public ICommand RemoveItem { get;  }

        public Visibility CanEditShader { get; private set; }
        public ElementTag Name { get { return entity.Tag; } }

        //  public System.ComponentModel.ICollectionView Components { get; set; }
        public ObservableCollection<IVisualComponentItem> Components { get; set; }

        //public ICommand OpenShaderEditor { get; }
        //public ICommand OpenPropertiesEditor { get; }

        readonly Dictionary<ElementTag, IVisualComponentItem> hash;

        readonly GraphicEntityDecorator entity;
        readonly ITreeItemActions actions;

        public VisualTreeItem(GraphicEntityDecorator entity, ITreeItemActions actions) {
            this.entity = entity;
            this.actions = actions;
            Components = new ObservableCollection<IVisualComponentItem>();
            hash = new Dictionary<ElementTag, IVisualComponentItem>();
            RemoveItem = new WpfActionCommand(OnSelfRemove);
        }

        private void OnSelfRemove() {
            entity.Remove();
            actions.Removed(this);
        }

        public void Add(IVisualComponentItem com) {
            Components.Add(com);
            hash.Add(com.Guid, com);

            CanEditShader = com.GetOriginComponent() is IShadersContainer ? Visibility.Visible : Visibility.Collapsed;
        }
        public void Clear() {
            Components.Clear();
            hash.Clear();
        }
        public void Remove(IVisualComponentItem com) {
            Components.Remove(com);
            hash.Remove(com.Guid);
            CanEditShader = !(com.GetOriginComponent() is IShadersContainer) ? Visibility.Collapsed : Visibility.Visible;
        }

        public bool TryRefresh(IGraphicComponent com) {
            if (!hash.ContainsKey(com.Tag)) {
                return false;
            }
            hash[com.Tag].Refresh();
            return true;
        }

        //public void Refresh() {
        //    foreach (var i in Components) {

        //        i.Refresh();
        //    }
        //}
    }
    public class VisualComponentItem : IVisualComponentItem, System.ComponentModel.INotifyPropertyChanged {
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public ICommand OpenPropertiesEditor { get; }

        readonly IGraphicComponent com;
        readonly IRenderUpdater updater;

        public VisualComponentItem(IGraphicComponent com, IRenderUpdater updater) {
            this.com = com;
            this.updater = updater;

            OpenPropertiesEditor = new Windows.VisualTreeviewerViewModel.OpenPropertiesEditorComponentItemCommand(updater);
        }

        public string Name { get { return com.GetType().Name; } }

        public ElementTag Guid { get { return com.Tag; } }

        public string Value { get; set; }

        public IGraphicComponent GetOriginComponent() {
            return com;
        }

        public void MarkAsModified() {
            com.IsModified = true;
        }

        public void Refresh() {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Name)));
        }
    }
}
