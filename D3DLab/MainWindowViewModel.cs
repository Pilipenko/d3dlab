using D3DLab.Debugger;
using D3DLab.Debugger.Modules.OBJFileFormat;
using D3DLab.Debugger.Presentation.ScriptConsole;
using D3DLab.Debugger.Presentation.SystemList;
using D3DLab.Debugger.Windows;
using D3DLab.ECS;
using D3DLab.ECS.Context;
using D3DLab.ECS.Ext;
using D3DLab.Parser;
using D3DLab.Plugin.Contracts.Parsers;
using D3DLab.Plugins;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.MeshFormats;
using D3DLab.Std.Engine.Core.Utilities;
using D3DLab.Visualization;
using D3DLab.Wpf.Engine.App;
using D3DLab.Wpf.Engine.App.GameObjects;
using D3DLab.Wpf.Engine.App.Host;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace D3DLab {
    public interface IDropFiles {
        void Dropped(string[] files);
    }

    public sealed class MainWindowViewModel : IDropFiles, IFileLoader {

        #region commands
        class ClearConsoleOutputCommand : Debugger.BaseWPFCommand {
            readonly MainWindowViewModel main;
            public ClearConsoleOutputCommand(MainWindowViewModel main) { this.main = main; }
            public override void Execute(object parameter) {
                main.ClearConsole();
            }
        }

        class ShowAxisCommand : Debugger.BaseWPFCommand {
            MainWindowViewModel main;
            CoordinateSystemLinesGameObject gameObj;

            bool isShowed;
            public ShowAxisCommand(MainWindowViewModel main) {
                this.main = main;
                isShowed = true;
            }

            public override void Execute(object parameter) {
                if (gameObj == null) {
                    gameObj = CoordinateSystemLinesGameObject.Build(main.context.GetEntityManager());
                    return;
                }
                if (isShowed) {
                    gameObj.Hide(main.context.GetEntityManager());
                } else {
                    gameObj.Show(main.context.GetEntityManager());
                }
                isShowed = !isShowed;
                main.ForceRender();
            }
        }
        class MoveToCenterWorldCommand : Debugger.BaseWPFCommand {
            private MainWindowViewModel main;

            public MoveToCenterWorldCommand(MainWindowViewModel main) {
                this.main = main;
            }

            public override void Execute(object parameter) {
                main.scene.Window.InputManager.PushCommand(new Std.Engine.Core.Input.Commands.ToCenterWorldCommand());
            }
        }
        class LoadCommand : ICommand {
            private MainWindowViewModel main;

            public LoadCommand(MainWindowViewModel mainWindowViewModel) {
                this.main = mainWindowViewModel;
            }

            public event EventHandler CanExecuteChanged = (s, r) => { };


            public bool CanExecute(object parameter) {
                return true;
            }

            public void Execute(object parameter) {

                // var file = this.GetType().Assembly.GetManifestResourceStream("D3DLab.Resources.ducky.obj");

                //main.plugins.Import();

                //   var path = @"D:\Storage_D\trash\3d\SharpDX-Rastertek-Tutorials-master\SharpDXWinForm\Externals\Models\skydome.txt";

                //main.items.ForEach(i=> i.GameObj.Cleanup(main.context.GetEntityManager()));
                //main.items.Clear();               

                main.items.Add(new LoadedItem(main, SkyGameObject.Create(main.context.GetEntityManager())));
                main.items.Add(new LoadedItem(main, TerrainGameObject.Create(main.context.GetEntityManager())));

                var obj = AnimCMOObject.Create(main.context.GetEntityManager());
                main.items.Add(new LoadedItem(main, obj));
            }
        }

        class PhysicsTestCommand : ICommand {
            private MainWindowViewModel main;
            readonly Random r;
            LoadedItem stat;

            public PhysicsTestCommand(MainWindowViewModel mainWindowViewModel) {
                this.main = mainWindowViewModel;
                r = new Random((int)DateTime.Now.Ticks);
            }

            public event EventHandler CanExecuteChanged = (s, r) => { };
            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter) {
                Task.Run(() => {
                    var count = 1;// 00;
                    while (count --> 0) {//64, 4, 32
                        Thread.Sleep(TimeSpan.FromSeconds(0.5));
                        var max = RandomUtil.NextVector3(r, new Vector3(0, 0, 0), new Vector3(10, 0, 10));
                        PhysicsObjectTest.Create(main.context.GetEntityManager(),
                                                new BoundingBox(
                                                    new Vector3(max.X - 10, 90, max.Y - 10),
                                                    new Vector3(max.X, 100, max.Z)),
                                                V4Colors.NextColor(r));
                    }
                });
                //if (stat.IsNull()) {
                //    stat = new LoadedItem(main, PhysicsObjectTest.CreateStatic(main.context.GetEntityManager(),
                //        new BoundingBox(new Vector3(-50, -50, -50), new Vector3(50, 50, 50))));
                //    main.items.Add(stat);

                //    PhysicsObjectTest.CreateStaticAABB(main.context.GetEntityManager(),
                //        new BoundingBox(new Vector3(-150, -30, -150), new Vector3(150, -15, 150)));
                //}
            }
        }

        public class RenderModeSwitherCommand : Debugger.BaseWPFCommand<Debugger.Infrastructure.IVisualTreeEntityItem> {
            readonly IContextState context;

            public RenderModeSwitherCommand(IContextState context) {
                this.context = context;
            }
            public override void Execute(Debugger.Infrastructure.IVisualTreeEntityItem item) {
                var tag = item.Name;
                //var sw = new EntityRenderModeSwither(context.GetEntityManager().GetEntity(tag));
                //sw.TurnOn(EntityRenderModeSwither.Modes.BoundingBox);
            }
        }

        #endregion

        #region items

        public class LoadedItem {
            public ICommand OpenProperties { get; }
            public ICommand VisiblityChanged { get; }
            public ICommand ShowDebuggingVisualization { get; }
            public ICommand LookAt { get; }
            public string Header { get { return GameObj.Description; } }

            public LoadedItem() { }

            public Std.Engine.Core.GeometryGameObject GameObj { get; protected set; }
            protected readonly MainWindowViewModel main;

            public LoadedItem(MainWindowViewModel main, Std.Engine.Core.GeometryGameObject gobj) {
                this.main = main;
                this.GameObj = gobj;
                VisiblityChanged = new Command(this);
                ShowDebuggingVisualization = new WpfActionCommand<bool?>(OnShowDebugVisualization);
                LookAt = new WpfActionCommand(OnLookAt);
                OpenProperties = new WpfActionCommand(OnOpenProperties);
            }

            protected virtual void OnOpenProperties() {

            }

            private void OnLookAt() {
                GameObj.LookAtSelf(main.context.GetEntityManager());
                main.ForceRender();
            }

            void OnShowDebugVisualization(bool? ischecked) {
                if (ischecked.HasValue && ischecked.Value) {
                    GameObj.ShowDebugVisualization(main.context.GetEntityManager());
                } else {
                    GameObj.HideDebugVisualization(main.context.GetEntityManager());
                }
                main.ForceRender();
            }

            public override string ToString() {
                return GameObj.ToString();
            }

            private void Hide() {
                GameObj.Hide(main.context.GetEntityManager());
                main.ForceRender();
            }

            private void Show() {
                GameObj.Show(main.context.GetEntityManager());
                main.ForceRender();
            }

            class Command : Debugger.BaseWPFCommand<bool> {
                private LoadedItem item;

                public Command(LoadedItem item) {
                    this.item = item;
                }

                public override void Execute(bool _checked) {
                    if (_checked) {
                        item.Show();
                    } else {
                        item.Hide();
                    }

                }
            }
        }



        public class ImportFileLoadedItem : LoadedItem {
            readonly ImportFileInfo info;
            readonly FileSystemWatcher watcher;
            readonly ManualResetEventSlim reset;
            readonly string tempFileName;
            readonly CompositeGameObjectFromFile compositeGameObject;
            public ImportFileLoadedItem(MainWindowViewModel main, ImportFileInfo info) : base(main, null) {
                this.info = info;
                reset = new ManualResetEventSlim(true);
                tempFileName = Path.ChangeExtension(info.File.FullName, ".temp");
                File.Delete(tempFileName);
                //
                var bl = new GameObjectBuilder(main.context.GetEntityManager());
                compositeGameObject = bl.Build(info.File, info.Parser);

                base.GameObj = compositeGameObject;
                //
                watcher = new FileSystemWatcher(info.File.DirectoryName, "*" + Path.GetExtension(info.File.Name));
                watcher.EnableRaisingEvents = true;
                watcher.Changed += OnFileChanged;
            }

            protected override void OnOpenProperties() {
                main.OpenObjDetailsWin(compositeGameObject);
            }

            DateTime lastWriteTime;
            private void OnFileChanged(object sender, FileSystemEventArgs e) {
                var lastTime = File.GetLastWriteTime(info.File.FullName);
                if (e.FullPath == info.File.FullName && lastWriteTime < lastTime) {
                    Thread.Sleep(100);
                    //reset.Wait();
                    //reset.Reset();
                    Stream stream = null;
                    var tries = 3;
                    while (tries-- > 0) {
                        try {
                            File.Copy(info.File.FullName, tempFileName, true);
                            stream = File.OpenRead(tempFileName);
                            //using (var readed = File.OpenRead(temp)) {
                            //stream = new MemoryStream();
                            //readed.CopyTo(stream);
                            //stream.Position = 0;
                            //}                            
                            lastWriteTime = lastTime;
                            break;
                        } catch {
                            Thread.Sleep(100);
                        }
                    }
                    if (stream.IsNull()) {
                        return;
                    }

                    System.Diagnostics.Trace.WriteLine($"Reload [{info.File.FullName}]");

                    try {
                        compositeGameObject.Cleanup(main.context.GetEntityManager());

                        var rbl = new GameObjectReBuilder(compositeGameObject, main.context.GetEntityManager());
                        rbl.Build(stream, info.Parser);
                    } catch (Exception ex) {
                        System.Diagnostics.Trace.WriteLine($"Error [{ex.Message}]");
                    } finally {
                        stream.Dispose();
                        //reset.Set();
                    }
                }
            }
        }

        #endregion

        private SceneView scene;
        private readonly EngineNotificator notificator;
        ContextStateProcessor context;

        public VisualTreeviewerViewModel VisualTreeviewer { get; }
        public SystemViewPresenter SystemsView { get; }
        public ScriptsConsoleVM ScriptsConsole { get; }

        public ICommand LoadDuck { get; set; }
        public ICommand PhysicsTest { get; set; }

        public ICommand MoveToCenterWorld { get; }
        public ICommand ShowAxis { get; }
        public ICommand ClearConsoleOutput { get; }



        public ICollectionView Items { get; set; }
        public ObservableCollection<string> ConsoleOutput { get; }
        readonly ObservableCollection<LoadedItem> items;
        readonly PluginImporter plugins;
        readonly PrimitiveDrawer primitiveDrawer;

        public MainWindowViewModel() {
            LoadDuck = new LoadCommand(this);
            PhysicsTest = new PhysicsTestCommand(this);

            MoveToCenterWorld = new MoveToCenterWorldCommand(this);
            ShowAxis = new ShowAxisCommand(this);
            ClearConsoleOutput = new ClearConsoleOutputCommand(this);

            primitiveDrawer = new PrimitiveDrawer();

            VisualTreeviewer = new VisualTreeviewerViewModel();
            SystemsView = new SystemViewPresenter();
            ScriptsConsole = new ScriptsConsoleVM(primitiveDrawer);

            items = new ObservableCollection<LoadedItem>();
            Items = CollectionViewSource.GetDefaultView(items);
            notificator = new EngineNotificator();

            notificator.Subscribe(new ViewportSubscriber(this));

            plugins = new PluginImporter();
            ConsoleOutput = new ObservableCollection<string>();
            System.Diagnostics.Trace.Listeners.Add(new TraceOutputListener(ConsoleOutput, App.Current.Dispatcher));

            //new Debugger.Modules.Obj.ObjDetailsWindow().Show();
        }

        public void Init(FormsHost host, FrameworkElement overlay) {
            context = new ContextStateProcessor();
            context.AddState(0, x => new GenneralContextState(x, notificator));

            context.SwitchTo(0);

            scene = new SceneView(host, overlay, context, notificator);
            scene.RenderStarted += OnRenderStarted;

            primitiveDrawer.SetContext(context);

            VisualTreeviewer.RenderModeSwither = new RenderModeSwitherCommand(context);
        }


        private void OnRenderStarted() {
            VisualTreeviewer.GameWindow = scene.Window;
            SystemsView.GameWindow = scene.Window;

            var manager = context.GetEntityManager();
            //
            items.Add(new LoadedItem(this, CameraGameObject.Create(context)));
            items.Add(new LoadedItem(this, LightGameObject.CreateAmbientLight(manager)));
            items.Add(new LoadedItem(this, LightGameObject.CreatePointLight(manager, Vector3.Zero + Vector3.UnitZ * 1000)));
            items.Add(new LoadedItem(this, LightGameObject.CreateDirectionLight(manager, new Vector3(1, 4, 4).Normalized())));

        }

        public void Dropped(string[] files) {
            plugins.Import();
            var win = new ChooseParseWindow();
            win.ViewModel.AddFiles(files);
            win.ViewModel.AddParsers(plugins.ParserPlugins);
            win.ViewModel.SetLoader(this);
            win.ShowDialog();
        }

        public void Load(ImportFileInfo info) {
            items.Add(new ImportFileLoadedItem(this, info));
        }

        void ForceRender() {
            scene.Window.InputManager.PushCommand(new Std.Engine.Core.Input.Commands.ForceRenderCommand());
        }

        void ClearConsole() {
            ConsoleOutput.Clear();
        }

        #region windows

        void OpenObjDetailsWin(CompositeGameObjectFromFile gobj) {
            ObjDetailsPopup.Open(gobj, context.GetEntityManager());
        }

        void ObjDetailsRefreshEntity(IEnumerable<ObjGroupsViewModel.ColorFilterViewItem> obj) {

        }


        #endregion
    }
}
