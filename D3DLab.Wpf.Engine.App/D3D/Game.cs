using D3DLab.ECS;
using D3DLab.SDX.Engine.Components;
using D3DLab.SDX.Engine.Rendering;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Components.Materials;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Render;
using D3DLab.Std.Engine.Core.Systems;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace D3DLab.SDX.Engine {
    
    public class D3DEngine : EngineCore {
        public readonly SynchronizedGraphics Graphics;
        public event Action<SynchronizedGraphics> Initialize;

        public D3DEngine(IAppWindow window, IContextState context, EngineNotificator notificator) :
            base(window, context, new D3DViewport(), notificator) {
            Statics.Collision = new SDXCollision();

            Graphics = new SynchronizedGraphics(window);
        }

        protected override void OnSynchronizing() {
            Graphics.Synchronize(System.Threading.Thread.CurrentThread.ManagedThreadId);
            base.OnSynchronizing();
        }

        public override void Dispose() {
            base.Dispose();
            Graphics.Dispose();
        }

        protected override void Initializing() {
            Initialize?.Invoke(Graphics);
        }
    }
}
