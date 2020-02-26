using D3DLab.Debugger.Presentation.PropertiesEditor;
using D3DLab.Debugger.Windows;
using D3DLab.ECS;
using D3DLab.ECS.Shaders;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Shaders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace D3DLab.Debugger.Presentation.SystemList {
    public class SystemItemViewModel : NotifyProperty {
        readonly IGraphicSystem system;

        public string Header { get; }
        public bool IsShaderEditable { get; }

        public TimeSpan ExecutionTime => system.ExecutionTime;

        public SystemItemViewModel(IGraphicSystem system) {
            this.system = system;
            Header = system.ToString().Split('.').Last();
            IsShaderEditable = system is IShadersContainer;
        }

        public IGraphicSystem GetOriginSystem() {
            return system;
        }

        public void RefreshExecutingTime() {
            RisePropertyChanged(nameof(ExecutionTime));
        }
    }

    public class ViewState {
        public int Count => items.Count;
        public ICollectionView Items { get; }
        readonly ObservableCollection<SystemItemViewModel> items;

        public ViewState() {
            items = new ObservableCollection<SystemItemViewModel>();
            Items = CollectionViewSource.GetDefaultView(items);
        }

        internal void AddItem(SystemItemViewModel item) {
            items.Add(item);
        }

        internal void RefreshExecutingTime() {
            foreach(var i in items) {
                i.RefreshExecutingTime();
            }
        }
    }
    public class SystemViewController {
        public ICommand OpenShaderEditor { get; }
        public ICommand OpenPropertiesEditor { get; }
        public class OpenShaderEditorSystemItemCommand : OpenShaderEditorCommand<SystemItemViewModel> {
            public OpenShaderEditorSystemItemCommand(IRenderUpdater updater) : base(updater) { }
            protected override IShadersContainer Convert(SystemItemViewModel i) {
                return (IShadersContainer)i.GetOriginSystem();
            }
        }
        public class OpenPropertiesEditorSystemItemCommand : OpenPropertiesEditorCommand<SystemItemViewModel> {
            public class EditingPropertiesComponentItem : IEditingProperties {
                readonly SystemItemViewModel item;

                public string Titile => item.Header;
                public object TargetObject => item.GetOriginSystem();

                public EditingPropertiesComponentItem(SystemItemViewModel item) {
                    this.item = item;
                }

                public void MarkAsModified() {
                    throw new NotImplementedException();
                }
            }

            public OpenPropertiesEditorSystemItemCommand(IRenderUpdater updater) : base(updater) { }

            protected override IEditingProperties Convert(SystemItemViewModel item) {
                return new EditingPropertiesComponentItem(item);
            }
        }

        public SystemViewController(IRenderUpdater updater) {
            OpenShaderEditor = new OpenShaderEditorSystemItemCommand(updater);
            OpenPropertiesEditor = new OpenPropertiesEditorSystemItemCommand(updater);
        }
    }

    public class SystemViewPresenter : IRenderUpdater {
        public IAppWindow GameWindow { get; set; }

        public ViewState State { get; }
        public SystemViewController Controller { get; }

        readonly Dictionary<int, SystemItemViewModel> hash;
        readonly DispatcherTimer timer;

        public SystemViewPresenter() {            
            State = new ViewState();
            Controller = new SystemViewController(this);
            State.Items.CurrentChanged += OnCurrentChanged;
            hash = new Dictionary<int, SystemItemViewModel>();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += OnTimeRefresh;
            timer.Start();
        }

        private void OnTimeRefresh(object sender, EventArgs e) {
            State.RefreshExecutingTime();
        }

        private void OnCurrentChanged(object sender, EventArgs e) {
            
        }

        public void AddSystem(IGraphicSystem system) {
            var item = new SystemItemViewModel(system);
            State.AddItem(item);
        }

        public void Update() {
            //GameWindow.InputManager.PushCommand(new Std.Engine.Core.Input.Commands.ForceRenderCommand());
        }
    }
}
