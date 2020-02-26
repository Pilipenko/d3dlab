using D3DLab.ECS;
using D3DLab.ECS.Ext;
using D3DLab.ECS.Input;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Input;
using D3DLab.Wpf.Engine.App.Host;
using D3DLab.Wpf.Engine.App.Input;
using System;
using System.Threading;
using System.Windows;

namespace D3DLab.Wpf.Engine.App {
    public sealed class GameWindow : IAppWindow {
        private WinFormsD3DControl win;

        public GameWindow(WinFormsD3DControl win, CurrentInputObserver input) {
            this.win = win;
            Width = (float)Application.Current.MainWindow.Width;
            Height = (float)Application.Current.MainWindow.Height;
            InputManager = new InputManager(input);

            //win.Resize += OnResize;
            Application.Current.MainWindow.SizeChanged += OnSizeChanged;          
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            Width = (float)e.NewSize.Width;
            Height = (float)e.NewSize.Height;
            Resized();
        }

        private void OnResize(object sender, EventArgs e) {
            Resized();
            Width = win.Width;
            Height = win.Height;
            //Width = (float)Application.Current.MainWindow.Width;
            //Height = (float)Application.Current.MainWindow.Height;

            
            System.Diagnostics.Trace.WriteLine($"Resize[App:{Application.Current.MainWindow.Width}/{Application.Current.MainWindow.Height}, WinForm:{win.Width}/{win.Height}]");
        }

        public float Width { get; private set; }

        public float Height { get; private set; }

        public bool IsActive => true;

        public IntPtr Handle => win.Handle;

        public IInputManager InputManager { get; }

        public event Action Resized;

        public WaitHandle BeginInvoke(Action action) {
            return win.BeginInvoke(action).AsyncWaitHandle;
        }

        public void SetTitleText(string txt) {
            Application.Current.Dispatcher.InvokeAsync(
                () => {
                    Application.Current.Return(x => x.MainWindow).Do(x => x.Title = txt);
                });
        }
    }
}
