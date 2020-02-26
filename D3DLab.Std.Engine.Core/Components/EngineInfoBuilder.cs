using D3DLab.ECS;
using D3DLab.ECS.Components;
using System;
using System.Diagnostics;
using System.Timers;

namespace D3DLab.Std.Engine.Core.Components {
    public sealed class WindowHeaderUpdateSystem : GraphicComponent {
        public IAppWindow Window { get; }

        //cpuUsage.NextValue()
        //TODO: add properties for PerformanceCounter

        readonly PerformanceCounter cpuUsage;
        readonly PerformanceCounter memUsage;
        readonly Timer timer;

        public WindowHeaderUpdateSystem(IAppWindow window) {
            cpuUsage = new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);//"_Total"
                                                                                                                      // memUsage = new PerformanceCounter("Process", "Working Set", Process.GetCurrentProcess().ProcessName);
            memUsage = new PerformanceCounter("Memory", "Available MBytes");
            // memUsage = new PerformanceCounter("Hyper-v Dynamic Memory VM", "Physical Memory", Process.GetCurrentProcess().ProcessName);
            Window = window;

            timer = new Timer();
            timer.Elapsed += RiseUpdate;
            timer.Interval = TimeSpan.FromSeconds(1).TotalMilliseconds;
            timer.Enabled = true;
        }

        void RiseUpdate(object sender, ElapsedEventArgs e) {
            //Window.SetTitleText($"| FPS: {FPS} | Milliseconds:{ElapsedMilliseconds} | CPU: {cpuUsage.NextValue()} % | Memory: {memUsage.NextValue()} |");
        }

        public override void Dispose() {
            timer.Stop();
            timer.Dispose();
            base.Dispose();
        }
    }

    public static class EngineInfoBuilder {
        

        public sealed class InputInfoComponent : GraphicComponent {
            public double EventCount { get; set; }


            public override string ToString() {
                return $"InputInfo[EventCount:{EventCount}";
            }
        }

        public static GraphicEntity Build(IEntityManager context, IAppWindow window) {
            var view = context.CreateEntity(new ElementTag("EngineInfo"));

            view//.AddComponent(new WindowHeaderUpdaterComponent(window))
                .AddComponent(new InputInfoComponent())
                .AddComponent(PerfomanceComponent.Create(0,0));

            return view;
        }
    }
}
