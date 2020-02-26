using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace D3DLab.ECS {
    public interface ISynchronizationContext {
        void Synchronize(int theadId);
    }

    public class SynchronizationContext<TOwner, TInput> {
        Queue<Tuple<Action<TOwner, TInput>, TInput>> queue;
        readonly TOwner owner;
        readonly object _loker;
        int theadId;

        public bool IsChanged { get; private set; }

        public SynchronizationContext(TOwner owner) :this(owner, new object()){
            theadId = -1;
        }
        public SynchronizationContext(TOwner owner, object _loker) {
            this.queue = new Queue<Tuple<Action<TOwner, TInput>, TInput>>();
            this.owner = owner;
            this._loker = _loker;
        }

        public void Synchronize(int theadId) {
            this.theadId = theadId;
            //copy to local
            Queue<Tuple<Action<TOwner, TInput>, TInput>> local;
            lock (_loker) { 
                local = new Queue<Tuple<Action<TOwner, TInput>, TInput>>(queue);
                queue = new Queue<Tuple<Action<TOwner, TInput>, TInput>>();
                IsChanged = false;
            }
            while (local.Any()) {
                var item = local.Dequeue();
                try {
                    item.Item1(owner, item.Item2);
                }catch(Exception ex) {
                    System.Diagnostics.Trace.WriteLine($"retry, move action to next render iteration [{ex.Message}]");
                    Add(item.Item1, item.Item2);
                }
            }
        }
        public void Add(Action<TOwner, TInput> action, TInput input) {
            //if(Thread.CurrentThread.ManagedThreadId == theadId) {
            //    action(owner, input);
            //    return;
            //}
            lock (_loker) {
                IsChanged = true;
                queue.Enqueue(Tuple.Create(action, input));
            }
        }
        public void AddRange(Action<TOwner, TInput> action, IEnumerable<TInput> inputs) {
            //if (Thread.CurrentThread.ManagedThreadId == theadId) {
            //    foreach (var input in inputs) {
            //        action(owner, input);
            //    }
            //    return;
            //}
            lock (_loker) {
                IsChanged = true;
                foreach (var input in inputs) {
                    queue.Enqueue(Tuple.Create(action, input));
                }
            }
        }

        public void Dispose() {
            queue.Clear();
        }
    }
}
