using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace D3DLab.ECS.Common {
    public class DisposeWatcher : IDisposable {
        readonly List<IDisposable> disposable;
        public DisposeWatcher() {
            disposable = new List<IDisposable>();
        }

        public void Dispose() {
            Disposer.DisposeAll(disposable);
        }

        public void Watch(IDisposable dis) {
            disposable.Add(dis);
        }

    }

    public class DisposableSetter<T> : IDisposable where T : IDisposable {
        public bool HasValue => disposable != null;

        T disposable;
        public void Dispose() {
            Disposer.DisposeAll(disposable);
        }
        public T Get() => disposable;
        public void Set(T b) {
            Dispose();
            disposable = b;
        }

        public DisposableSetter(DisposeWatcher watcher) {
            watcher.Watch(this);
        }
        public DisposableSetter() {
        }
    }

    public class EnumerableDisposableSetter<T> : IDisposable where T : IEnumerable<IDisposable> {
        T disposable;

        public EnumerableDisposableSetter(DisposeWatcher watcher) {
            watcher.Watch(this);
        }
        public EnumerableDisposableSetter() {
        }

        public void Dispose() {
            Disposer.DisposeAll(disposable);
        }
        public T Get() => disposable;
        public bool HasValue => disposable != null && disposable.Any();

        public void Set(T b) {
            Disposer.DisposeAll(disposable);
            disposable = b;
        }
    }
}
