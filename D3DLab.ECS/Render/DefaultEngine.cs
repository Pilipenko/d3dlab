using D3DLab.ECS.Components;
using D3DLab.ECS.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace D3DLab.ECS.Render {
    public abstract class DefaultEngine {
        static readonly double total = TimeSpan.FromSeconds(1).TotalMilliseconds;
        static readonly double oneFrameMilliseconds = (total / 60);
        //static double _desiredFrameLengthSeconds = 1.0 / 60.0;
        
        IEntityRenderNotify notify;

        Task loopTask;
        readonly CancellationTokenSource tokensource;
        readonly CancellationToken token;

        public EngineNotificator Notificator { get; }
        public IContextState Context { get; }
        public IAppWindow Window { get; }

        public DefaultEngine(IAppWindow window, IContextState context,EngineNotificator notificator) {
            Context = context;
            this.Window = window;
            this.Notificator = notificator;
            tokensource = new CancellationTokenSource();
            token = tokensource.Token;
        }

        protected abstract void Initializing();
        protected abstract ISceneSnapshot CreateSceneSnapshot(TimeSpan frameRateTime);

        public void Run(IEntityRenderNotify notify) {
            this.notify = notify;
            Initializing();
            loopTask = Task.Factory.StartNew((Action)Loop, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        protected virtual void OnSynchronizing() {}

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
            var engineInfoTag = ElementTag.Empty;

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
            //var ishapshot = imanager.GetInputSnapshot();
            var id = Thread.CurrentThread.ManagedThreadId;
            
            emanager.Synchronize(id);

            var snapshot = CreateSceneSnapshot(TimeSpan.FromMilliseconds(millisec));// new SceneSnapshot(Window, notificator, viewport, Octree, ishapshot, TimeSpan.FromMilliseconds(millisec));
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
