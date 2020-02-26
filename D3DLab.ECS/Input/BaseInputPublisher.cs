using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS.Input {
    public abstract class BaseInputPublisher : IInputPublisher {
        private readonly List<InputObserver> subscribers;
        protected readonly InputStateData state;
        public BaseInputPublisher() {
            subscribers = new List<InputObserver>();
            state = InputStateData.Create();
        }

        public void Subscrube(InputObserver s) {
            subscribers.Add(s);
        }
        public void UnSubscruber(InputObserver s) {
            subscribers.Remove(s);
        }
        public virtual void Dispose() {
            subscribers.Clear();
        }
        public bool AnySubscrubers() {
            return subscribers.Count > 0;
        }
        protected void InvokeSubscribers(Func<InputObserver, InputStateData, bool> action) {
            InvokeSubscribers(subscribers, action, state);
        }

        private static void InvokeSubscribers<T>(IEnumerable<InputObserver> subscribers, Func<InputObserver, T, bool> action, T ev) {
            foreach (var component in subscribers) {
                if (action(component, ev)) {
                    break;
                }
            }
        }
    }
}
