using D3DLab.ECS;
using System;
using System.IO;
using System.Threading.Tasks;

namespace D3DLab.SDX.Engine {
    public class SynchronizedGraphics : ISynchronizationContext {
        struct Size {
            public float Width;
            public float Height;
        }
        internal event Action<GraphicsDevice> Changed;
        public readonly GraphicsDevice Device;
        readonly IAppWindow window;
        readonly SynchronizationContext<SynchronizedGraphics, Size> synchronizer;

        public bool IsChanged => synchronizer.IsChanged;

        public SynchronizedGraphics(IAppWindow window) {
            Device = new GraphicsDevice(window);
            window.Resized += OnResized;
            this.window = window;
            synchronizer = new SynchronizationContext<SynchronizedGraphics, Size>(this); 
        }

        private void OnResized() {
            synchronizer.Add((_this, size) => {
                _this.Device.Resize(size.Width, size.Height);
                Changed(_this.Device);
            }, new Size { Height = window.Height, Width = window.Width });
        }

        public void Dispose() {
            Device.Dispose();
        }

        public void Synchronize(int theadId) {
            synchronizer.Synchronize(theadId);
        }

        public void GetBackBufferBitmapInvokeAsync(Action<System.Drawing.Bitmap> callback) {
            synchronizer.Add((_this, size) => {
                var btm = this.Device.CopyBackBufferTexture();
                Task.Run(()=>callback.Invoke(btm));
            }, new Size());
        }
    }
}
