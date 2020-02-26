using System;
using System.Threading;

namespace D3DLab.ECS {
    public abstract class GraphicComponent : IGraphicComponent {
        int threadSafeBool = 0;
        public bool IsModified {
            get { return Interlocked.CompareExchange(ref threadSafeBool, 1, 1) == 1; }
            set {
                if (value) Interlocked.CompareExchange(ref threadSafeBool, 1, 0);
                else Interlocked.CompareExchange(ref threadSafeBool, 0, 1);
            }
        }

        public bool IsDisposed { get; protected set; }
        public virtual bool IsValid => true;
        public ElementTag Tag { get; }
        public ElementTag EntityTag { get; set; }

        protected GraphicComponent() {
            Tag = new ElementTag(Guid.NewGuid().ToString());
        }

        public virtual void Dispose() {
            IsDisposed = true;
        }
    }
}
