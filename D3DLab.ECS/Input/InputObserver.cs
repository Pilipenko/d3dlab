using D3DLab.ECS.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace D3DLab.ECS.Input {
    public abstract class InputObserver : IDisposable {
        protected sealed class StateDictionary : Dictionary<int, Func<StateProcessor, InputState>> { }

        public interface IHandler { }
        protected sealed class StateHandleProcessor<THandler> : InputObserver.StateProcessor where THandler : IHandler {
            private readonly THandler inputHandler;
            public StateHandleProcessor(StateDictionary states, THandler inputHandler) : base(states) {
                this.inputHandler = inputHandler;
            }

            public override void InvokeHandler<T>(Action<T> action) {
                //                Dispatcher.CurrentDispatcher.BeginInvoke(action, inputHandler);
                // action.BeginInvoke(inputHandler, null, null);
                //                Task.Run(() => action(inputHandler));
                var handler = (IHandler)inputHandler;
                action((T)handler);
            }
        }
        protected abstract class StateProcessor : InputState {
            private InputState current;
            private readonly StateDictionary states;
            protected StateProcessor(StateDictionary states) : base() {
                this.states = states;
                /*
                var matrix = new Action<InputStateDate>[3, 3] {
                      //Left    //Right                 //Middle
                    {   null,   InputHandler.Pan,       null    },//Left 
                    {   null,   InputHandler.Rotate,    null    },//Right
                    {   null,   null,                   null    } //Middle
                };
                */
            }

            public abstract void InvokeHandler<T>(Action<T> action) where T : IHandler;

            public override void SwitchTo(int stateTo, InputStateData state) {
                if (states.ContainsKey(stateTo)) {
                    current?.LeaveState(state);
                    current = states[stateTo](this);
                    current.EnterState(state);
                } else {
                   Debug.WriteLine($"No handled state {stateTo}");
                }     
            }
            public override bool OnMouseMove(InputStateData state) { return current.OnMouseMove(state); }
            public override bool OnMouseDown(InputStateData state) { return current.OnMouseDown(state); }
            public override bool OnMouseUp(InputStateData state) { return current.OnMouseUp(state); }
            public override bool OnMouseWheel(InputStateData ev) { return current.OnMouseWheel(ev); }
            public override bool OnMouseDoubleDown(InputStateData state) => current.OnMouseDoubleDown(state);

            public override bool KeyDown(InputStateData ev) { return current.KeyDown(ev); }
            public override bool KeyUp(InputStateData ev) { return current.KeyUp(ev); }
        }

        protected abstract class InputStateMachine : InputState {
            protected readonly StateProcessor Processor;

            protected InputStateMachine(StateProcessor processor) {
                this.Processor = processor;
            }

            public override void SwitchTo(int stateTo, InputStateData state) {
                Processor.SwitchTo(stateTo, state);
            }
        }

        protected abstract class InputState {
            public virtual void EnterState(InputStateData inputStateDate) {

            }
            public void LeaveState(InputStateData inputStateDate) {

            }

            public virtual bool OnMouseMove(InputStateData state) {
                return false;
            }
            public virtual bool OnMouseDown(InputStateData state) {
                return false;
            }
            public virtual bool OnMouseDoubleDown(InputStateData state) => false;

            public virtual bool OnMouseUp(InputStateData state) {
                return false;
            }
            public abstract void SwitchTo(int stateTo, InputStateData state);
            public virtual bool OnMouseWheel(InputStateData ev) {
                return false;
            }


            public virtual bool KeyDown(InputStateData ev) {
                return false;
            }
            public virtual bool KeyUp(InputStateData ev) {
                return false;
            }            
        }

        private static readonly object loker;

        static InputObserver() {
            loker = new object();
        }

        private InputState stateMachine;
        private InputState StateMachine {
            get { return stateMachine ?? (stateMachine = GetIdleState()); }
        }

        protected InputSnapshot currentSnapshot;
        //
        protected InputObserver(IInputPublisher publisher) {
            lock (loker) {
                currentSnapshot = new InputSnapshot();
                publisher.Subscrube(this);
            }
        }
        //protected InputObserver(Control control) {
        //    this.control = control;
        //    lock (loker) {
        //        if (publisher == null) {
        //            publisher = new WinFormInputPublisher(control);
        //        }
        //        publisher.Subscrube(this);
        //    }
        //}
        //protected InputObserver(System.Windows.FrameworkElement control) {
        //    //this.control = control;
        //    lock (loker) {
        //        if (publisher == null) {
        //            publisher = new WPFInputPublisher(control);
        //        }
        //        publisher.Subscrube(this);
        //    }
        //}
        protected abstract InputState GetIdleState();

        public bool OnMouseMove(InputStateData state) {
            currentSnapshot.CurrentInputState = state.Clone();
            return StateMachine.OnMouseMove(state);
        }
        public bool OnMouseDown(InputStateData state) {
            currentSnapshot.CurrentInputState = state.Clone();
            return StateMachine.OnMouseDown(state);
        }
        public bool OnMouseDoubleDown(InputStateData state) {
            currentSnapshot.CurrentInputState = state.Clone();
            return StateMachine.OnMouseDoubleDown(state);
        }
        public bool OnMouseUp(InputStateData state) {
            currentSnapshot.CurrentInputState = state.Clone();
            return StateMachine.OnMouseUp(state);
        }
        public bool OnMouseWheel(InputStateData state) {
            currentSnapshot.CurrentInputState = state.Clone();
            return StateMachine.OnMouseWheel(state);
        }

        public bool KeyDown(InputStateData state) {
            currentSnapshot.CurrentInputState = state.Clone();
            return StateMachine.KeyDown(state);
        }
        public bool KeyUp(InputStateData state) {
            currentSnapshot.CurrentInputState = state.Clone();
            return StateMachine.KeyUp(state);
        }

        public void Dispose() {
            
        }

        public void PushCommand(IInputCommand cmd) {
            currentSnapshot.AddEvent(cmd);
        }
        public InputSnapshot GetInputSnapshot() {
            return currentSnapshot.CloneAndClear();
        }
    }
}
