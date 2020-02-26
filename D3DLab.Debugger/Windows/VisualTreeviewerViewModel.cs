using D3DLab.Debugger.Infrastructure;
using D3DLab.Debugger.Model;
using D3DLab.Debugger.Presentation.PropertiesEditor;
using D3DLab.ECS;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Shaders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace D3DLab.Debugger.Windows {
    public interface IInputProvider {

    }
    public sealed class VisualTreeviewerPopup {
        private VisualTreeviewer win;
        public VisualTreeviewerViewModel ViewModel { get; }
        public VisualTreeviewerPopup() {
            win = new VisualTreeviewer();
            ViewModel = (VisualTreeviewerViewModel)win.tree.DataContext;
        }
        public void Show() {
            win.Show();
        }
    }

    public interface IRenderUpdater {
        void Update();
    }
    public class EditingPropertiesComponentItem : IEditingProperties {
        readonly IVisualComponentItem item;

        public string Titile => item.Name;
        public object TargetObject => item.GetOriginComponent();

        public EditingPropertiesComponentItem(IVisualComponentItem item) {
            this.item = item;
        }

        public void MarkAsModified() {
            item.MarkAsModified();
        }
    }

    public interface ITreeItemActions {
        void Removed(VisualTreeItem item);
    }

    public class GraphicEntityDecorator {
        readonly GraphicEntity entity;
        public ElementTag Tag => entity.Tag;
        public bool IsDestroyed => entity.IsDestroyed;

        readonly IEnumerable<IGraphicComponent> coms;

        public GraphicEntityDecorator(GraphicEntity entity) {
            this.entity = entity;
            coms = entity.GetComponents().ToList();
            
        }

        public IEnumerable<IGraphicComponent> GetComponents() {
            return coms;
        }

        public void Remove() {
            if (!entity.IsDestroyed) {
                entity.Remove();
            }
        }
    }

    public sealed class VisualTreeviewerViewModel : System.ComponentModel.INotifyPropertyChanged, IRenderUpdater, ITreeItemActions {
       
        public class OpenPropertiesEditorComponentItemCommand : Presentation.OpenPropertiesEditorCommand<IVisualComponentItem> {
            public OpenPropertiesEditorComponentItemCommand(IRenderUpdater updater):base(updater) { }

            protected override IEditingProperties Convert(IVisualComponentItem item) {
                return new EditingPropertiesComponentItem(item);
            }
        }


        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        string consoleText;

        public string ConsoleText {
            get {
                return consoleText;
            }

            set {
                consoleText = value;
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(ConsoleText)));
            }
        }

        string title;
        private BaseWPFCommand<IVisualTreeEntityItem> _renderModeSwither;

        public string Title {
            get {
                return title;
            }

            set {
                title = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public int ItemsCount => items.Count;

        public System.ComponentModel.ICollectionView Items { get; set; }
        ObservableCollection<IVisualTreeEntityItem> items;
        public IVisualComponentItem SelectedComponent { get; set; }

        public BaseWPFCommand<IVisualTreeEntityItem> RenderModeSwither {
            get => _renderModeSwither;
            set {
                _renderModeSwither = value; 
                 PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(RenderModeSwither)));
            }
        }

        public IAppWindow GameWindow { get; set; }

        private readonly Dictionary<ElementTag, IVisualTreeEntityItem> hash;

        public VisualTreeviewerViewModel() {
            items = new ObservableCollection<IVisualTreeEntityItem>();
            Items = CollectionViewSource.GetDefaultView(items);
            hash = new Dictionary<ElementTag, IVisualTreeEntityItem>();
            //SelectedComponent = Items.First().Properties.First();
            //OpenShaderEditor = new OpenPropertiesEditorCommand();
            //OpenPropertiesEditor = new OpenPropertiesEditorCommand();
        }

        public void Update() {
            //GameWindow.InputManager.PushCommand(new Std.Engine.Core.Input.Commands.ForceRenderCommand());
        }

        public void Change(GraphicEntityDecorator entity) {
            if (entity.IsDestroyed) {
                if (hash.ContainsKey(entity.Tag)) {
                    items.Remove(hash[entity.Tag]);
                    hash.Remove(entity.Tag);
                }
                return;
            }

            var found = items.SingleOrDefault(x => x.Name == entity.Tag);
            if (found == null) {
                found = new VisualTreeItem(entity, this);
                foreach (var com in entity.GetComponents()) {
                    found.Add(new VisualComponentItem(com, this));
                }
                items.Add(found);
                hash.Add(found.Name, found);
            } else {
                found.Clear();
                var coms = entity.GetComponents();
                foreach (var com in coms) {
                    found.Add(new VisualComponentItem(com, this));//{ RenderModeSwither = _renderModeSwither }
                }
            }
        }

        public void Refresh(IEnumerable<GraphicEntityDecorator> entities) {
            Title = $"Visual Tree [entities {entities.Count()}]";
            foreach (var en in entities) {
                if (hash.ContainsKey(en.Tag)) {
                    var item = hash[en.Tag];
                    var existed = new HashSet<ElementTag>();
                    var coms = en.GetComponents();
                    foreach (var com in coms) {
                        if(com == null) { continue; }
                        if (!item.TryRefresh(com)) {
                            item.Add(new VisualComponentItem(com, this));
                        }
                        existed.Add(com.Tag);
                    }
                    //clear removed
                    foreach (var com in item.Components.ToArray()) {
                        if (!existed.Contains(com.Guid)) {
                            item.Remove(com);
                        }
                    }
                } else {

                }
            }
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(ItemsCount)));
        }

        public void Execute(string code) {
            var console = ConsoleText.Trim();
            ConsoleText = console;
            //var executer = new ScriptExetuter();

            //executer.Execute(new ScriptEnvironment { CurrentWatch = items }, console).ContinueWith(x => {
            //    var text = "";
            //    if (x.Exception != null) {
            //        text = x.Exception.InnerException.Message;
            //    } else {
            //        text = x.Result.ToString();
            //    }
            //    ConsoleText += Environment.NewLine + text;
            //});
        }

        #region IItemActions

        public void Removed(VisualTreeItem item) {
            hash.Remove(item.Name);
            items.Remove(item);
        } 

        #endregion
    }
}
