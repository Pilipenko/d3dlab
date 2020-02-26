using D3DLab.SDX.Engine;
using D3DLab.SDX.Engine.Components;
using D3DLab.SDX.Engine.Rendering;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Components.Materials;
using D3DLab.Std.Engine.Core.Systems;
using D3DLab.Wpf.Engine.App.Host;
using D3DLab.Wpf.Engine.App.Input;
using System;
using System.Numerics;
using System.Windows;
using D3DLab.Wpf.Engine.App.D3D.Techniques;
using D3DLab.Std.Engine.Core.Utilities;
using D3DLab.Wpf.Engine.App.GameObjects;
using System.Linq;
using System.Collections.Generic;
using D3DLab.SDX.Engine.Animation;
using System.Threading;
using D3DLab.ECS.Context;
using D3DLab.ECS;
using D3DLab.ECS.Systems;
using D3DLab.Wpf.Engine.App.D3D;
using D3DLab.SDX.Engine.Shader;
using System.IO;
using System.Reflection;

namespace D3DLab.Wpf.Engine.App {
    public class OctreeImp : EntityOctree {
        readonly Random random;
        readonly List<PolylineGameObject> debug;

        public OctreeImp(IContextState context,BoundingBox box, int MaximumChildren) : base(context, box, MaximumChildren) {
            random = new Random();
            debug = new List<PolylineGameObject>();
        }
        public override void Draw(IEntityManager emanager) {
            if(isActualStateDrawed) {
                return;
            }
            foreach(var obj in debug) {
                obj.Cleanup(emanager);
            }
            debug.Clear();
            base.Draw(emanager);
        }

        public override void DrawBox(ElementTag tag, BoundingBox box, IEntityManager emanager) {
            var points = GeometryBuilder.BuildBox(box);
            var color = V4Colors.NextColor(random);
            if (tag.IsEmpty) {
                Thread.Sleep(10);
                tag = new ElementTag(DateTime.Now.Ticks.ToString());
            }
            debug.Add(PolylineGameObject.Create(emanager, new ElementTag("OctreeBox_" + tag.ToString()), points, points.Select(x => color).ToArray()));
        }
    }

    class IncludeResourse : IIncludeResourse {
        readonly Assembly assembly;
        readonly string path;
        public string Key { get; }

        public IncludeResourse(Assembly assembly, string key, string path) {
            this.assembly = assembly;
            Key = key;
            this.path = path;
        }

        public Stream GetResourceStream() {
            return assembly.GetManifestResourceStream(path);
        }
    }
    public class Scene {
        private readonly FormsHost host;
        private readonly FrameworkElement overlay;
        private readonly EngineNotificator notify;
        readonly CurrentInputObserver input;

        D3DEngine game;
        public GameWindow Window { get; private set; }

        public IContextState Context { get; }
        public event Action RenderStarted = () => { };

        public Scene(FormsHost host, FrameworkElement overlay, ContextStateProcessor context, EngineNotificator notify) {
            this.host = host;
            this.overlay = overlay;
            host.HandleCreated += OnHandleCreated;
            host.Unloaded += OnUnloaded;
            this.Context = context;
            this.notify = notify;
            input = new CurrentInputObserver(Application.Current.MainWindow, new WPFInputPublisher(Application.Current.MainWindow));
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            game.Dispose();
        }

        private void OnHandleCreated(WinFormsD3DControl win) {
            Window = new GameWindow(win, input);
            game = new D3DEngine(Window, Context, notify);

            var includes = new System.Collections.Generic.Dictionary<string, IIncludeResourse>();

            var sdx = typeof(GraphicsDevice).Assembly;
            var app = typeof(Scene).Assembly;

            includes.Add("Game", new IncludeResourse(sdx, "Game", "D3DLab.SDX.Engine.Rendering.Shaders.Game.hlsl"));
            includes.Add("Light", new IncludeResourse(sdx, "Light", "D3DLab.SDX.Engine.Rendering.Shaders.Light.hlsl"));
            includes.Add("Math", new IncludeResourse(sdx, "Math", "D3DLab.SDX.Engine.Rendering.Shaders.Math.hlsl"));
            includes.Add("Common", new IncludeResourse(app, "Common", "D3DLab.Wpf.Engine.App.D3D.Animation.Shaders.Common.hlsl"));

            game.Graphics.Device.Compilator.AddInclude(new D3DLab.SDX.Engine.Shader.D3DIncludeAdapter(includes));

            game.SetOctree(new OctreeImp(Context, BoundingBox.Create(new Vector3(-1000, -1000, -1000), new Vector3(1000, 1000, 1000)), 5));
            game.Initialize += Initializing;
            game.Run(notify);

            // Initializing();

            RenderStarted();
        }

        void Initializing(SynchronizedGraphics device) {

            var em = Context.GetEntityManager();
            var cameraTag = new ElementTag("CameraEntity");
            {   //systems creating
                var smanager = Context.GetSystemManager();


                smanager.CreateSystem<DefaultInputSystem>();
                smanager.CreateSystem<D3D.D3DCameraSystem>();
                smanager.CreateSystem<DefaultLightsSystem>();
                smanager.CreateSystem<CollidingSystem>();
                smanager.CreateSystem<MovementSystem>();
                smanager.CreateSystem<EmptyAnimationSystem>();
                smanager.CreateSystem<MeshAnimationSystem>();
              //  smanager.CreateSystem<StickOnHeightMapSystem>();
                smanager.CreateSystem<ObjectMovementSystem>();
                smanager.CreateSystem<Systems.TerrainGeneratorSystem>();
                smanager.CreateSystem<Physics.Engine.PhysicalSystem>();

                smanager
                    .CreateSystem<RenderSystem>()
                    .Init(device)
                    .CreateNested<SkyGradientColoringRenderTechnique>()
                    .CreateNested<SkyPlaneWithParallaxRenderTechnique>()
                    .CreateNested<TerrainRenderTechnique>()//
                    .CreateNested<TriangleColoredVertexRenderTechnique>()
                    .CreateNested<LineVertexRenderTechnique>()
                    .CreateNested<SpherePointRenderStrategy>()
                    .CreateNested<AminRenderTechniqueSystem >();
                
                //smanager
                //    .CreateSystem<AminRenderSystem>()
                //    .Init(device);
            }
            {
               var engine = EngineInfoBuilder.Build(em, Window);               
            }
            {//entities ordering 
                Context.EntityOrder
                       .RegisterOrder<RenderSystem>(cameraTag, 0)
                       .RegisterOrder<DefaultInputSystem>(cameraTag, 0);
            }
        }
    }
}
