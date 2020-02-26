using D3DLab.ECS;
using D3DLab.ECS.Components;
using D3DLab.ECS.Input;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Input;
using D3DLab.Std.Engine.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace D3DLab.Std.Engine.Core.Render {
    public abstract class EngineCore {
        static readonly double total = TimeSpan.FromSeconds(1).TotalMilliseconds;
        static readonly double oneFrameMilliseconds = (total / 60);
        //static double _desiredFrameLengthSeconds = 1.0 / 60.0;

        public IAppWindow Window { get; }
        IEntityRenderNotify notify;

        Task loopTask;
        readonly CancellationTokenSource tokensource;
        readonly CancellationToken token;
        readonly IViewport viewport;
        readonly EngineNotificator notificator;
        public IOctree Octree { get; private set; }

        public IContextState Context { get; }

        public EngineCore(IAppWindow window, IContextState context, IViewport viewport, EngineNotificator notificator) {
            Context = context;
            this.viewport = viewport;
            this.Window = window;
            this.notificator = notificator;
            tokensource = new CancellationTokenSource();
            token = tokensource.Token;
        }

        public void SetOctree(IOctree o) {
            Octree = o;
            notificator.Subscribe(Octree);
        }

        protected abstract void Initializing();

        public void Run(IEntityRenderNotify notify) {
            this.notify = notify;
            Initializing();
            loopTask = Task.Factory.StartNew((Action)Loop, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);            
        }

        protected virtual void OnSynchronizing() {

        }

        void Loop() {
            System.Threading.Thread.CurrentThread.Name = "Game Loop";
            var imanager = Window.InputManager;

            //first synchronization
            Context.GetEntityManager().Synchronize(Thread.CurrentThread.ManagedThreadId);
            imanager.Synchronize(Thread.CurrentThread.ManagedThreadId);

            var speed = new Stopwatch();
            var any = Context
                .GetEntityManager()
                .GetEntities()
                .Where(x => x.Has<PerfomanceComponent>());
            ElementTag engineInfoTag;

            if (any.Any()) {
                engineInfoTag = any.Single().Tag;
            }

            double millisec = oneFrameMilliseconds;
            while (Window.IsActive && !token.IsCancellationRequested) {
                speed.Restart();

                imanager.Synchronize(Thread.CurrentThread.ManagedThreadId);
                OnSynchronizing();

                var eman = Context.GetEntityManager();

                Rendering(eman, imanager, millisec);

                millisec = speed.ElapsedMilliseconds;

                if (millisec < oneFrameMilliseconds) {
                    Thread.Sleep((int)(oneFrameMilliseconds - millisec));
                }
                speed.Stop();

                millisec = speed.ElapsedMilliseconds;

                eman.GetEntity(engineInfoTag)
                    .UpdateComponent(PerfomanceComponent.Create(millisec, (int)(total / millisec)));
                //Debug.WriteLine($"FPS {(int)(total / speed.ElapsedMilliseconds)} / {speed.ElapsedMilliseconds} ms");

                notify.NotifyRender(eman.GetEntities().ToArray());
            }

            Window.InputManager.Dispose();
            Context.Dispose();
        }

        void Rendering(IEntityManager emanager, IInputManager imanager, double millisec) {
            var ishapshot = imanager.GetInputSnapshot();

            //if (!ishapshot.Events.Any() && !emanager.HasChanges) {//no input/changes no rendering 
            //    return;
            //}

            var id = Thread.CurrentThread.ManagedThreadId;

            Octree.Synchronize(id);
            //Octree.Draw(Context.GetEntityManager());
            
            emanager.Synchronize(id);

            var snapshot = new SceneSnapshot(Window, notificator, viewport, Octree, ishapshot, TimeSpan.FromMilliseconds(millisec));
            foreach (var sys in Context.GetSystemManager().GetSystems()) {
                try {
                    sys.Execute(snapshot);
                    //run synchronization after each exetuted system, to synchronize state for the next system
                    emanager.FrameSynchronize(id);
                } catch (Exception ex) {
                    ex.ToString();
#if !DEBUG
                    throw ex;
#endif
                }
            }
        }

        public virtual void Dispose() {
            if (loopTask.Status == TaskStatus.Running) {
                tokensource.Cancel();
                loopTask.Wait();
            }
        }
    }
}
