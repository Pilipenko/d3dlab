using D3DLab.ECS.Input;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Input;
using D3DLab.Std.Engine.Core.Input.Commands;
using System.Windows;
using System.Windows.Forms;

namespace D3DLab.Wpf.Engine.App.Input {
    public enum AllInputStates {
        Idle = 0,
        Rotate = 1,
        Pan = 2,
        Zoom = 3,
        Target = 4,
        //UnTarget = 5,
        KeywordDown = 6,
        ChangeFocus = 7,
    }
    public struct InputEventState {
        public AllInputStates Type { get; set; }
        public InputStateData Data { get; set; }
    }

    public class CurrentInputObserver : InputObserver,
        CurrentInputObserver.ICameraInputHandler, CurrentInputObserver.ITargetingInputHandler {

        public interface ICameraInputHandler : InputObserver.IHandler {
            bool Rotate(InputStateData state);
            void Zoom(InputStateData state);
            void Pan(InputStateData state);
            void Idle();
            void KeywordMove(InputStateData state);
            void FocusToObject(InputStateData state);
            
        }

        public interface ITargetingInputHandler : InputObserver.IHandler {
            void TargetCapture(InputStateData state);
            void TargetMove(InputStateData state);
            void UnTarget(InputStateData state);
        }

        protected sealed class InputIdleState : CurrentStateMachine {
            public InputIdleState(StateProcessor processor) : base(processor) {
                Processor.InvokeHandler<ICameraInputHandler>(x => x.Idle());
            }

            public override void EnterState(InputStateData state) {
                switch (state.Buttons) {
                    case GeneralMouseButtons.Right:
                        SwitchTo((int)AllInputStates.Rotate, state);
                        break;
                }
            }

            public override bool OnMouseDown(InputStateData state) {
                switch (state.Buttons) {
                    //camera
                    case GeneralMouseButtons.Left | GeneralMouseButtons.Right:
                        SwitchTo((int)AllInputStates.Pan, state);
                        break;
                    case GeneralMouseButtons.Right:
                        SwitchTo((int)AllInputStates.Rotate, state);
                        break;

                    //case GeneralMouseButtons.Middle:
                    //    break;

                    //manipulation
                    case GeneralMouseButtons.Left:
                        SwitchTo((int)AllInputStates.Target, state);
                        break;
                }
                return base.OnMouseDown(state);
            }

            public override bool OnMouseWheel(InputStateData state) {
                SwitchTo((int)AllInputStates.Zoom, state);
                return base.OnMouseWheel(state);
            }

            public override bool KeyDown(InputStateData state) {
                SwitchTo((int)AllInputStates.KeywordDown, state);
                return true;
            }

            public override bool OnMouseDoubleDown(InputStateData state) {
                SwitchTo((int)AllInputStates.ChangeFocus, state);
                return base.OnMouseDoubleDown(state);
            }
        }

        #region Camera

        protected sealed class InputRotateState : CurrentStateMachine {
            public InputRotateState(StateProcessor processor) : base(processor) {
                // Cursor.Hide();
            }
            public override void EnterState(InputStateData state) {
                Processor.InvokeHandler<ICameraInputHandler>(x => x.Rotate(state));
            }
            public override bool OnMouseUp(InputStateData state) {
                if ((state.Buttons & GeneralMouseButtons.Right) != GeneralMouseButtons.Right) {
                    SwitchTo((int)AllInputStates.Idle, state);
                }

                return base.OnMouseUp(state);
            }
            public override bool OnMouseDown(InputStateData state) {
                switch (state.Buttons) {
                    case GeneralMouseButtons.Left | GeneralMouseButtons.Right:
                        SwitchTo((int)AllInputStates.Pan, state);
                        break;
                }
                return base.OnMouseDown(state);
            }
            public override bool OnMouseMove(InputStateData state) {
                // Cursor.Position = state.ButtonsStates[GeneralMouseButtons.Right].CursorPointDown.ToDrawingPoint();
                Processor.InvokeHandler<ICameraInputHandler>(x => x.Rotate(state));
                return true;
            }
            public override bool OnMouseDoubleDown(InputStateData state) {
                SwitchTo((int)AllInputStates.ChangeFocus, state);
                return base.OnMouseDoubleDown(state);
            }
        }
        protected sealed class InputPanState : CurrentStateMachine {
            public InputPanState(StateProcessor processor) : base(processor) {
            }
            public override bool OnMouseUp(InputStateData state) {
                if ((state.Buttons & GeneralMouseButtons.Right) != GeneralMouseButtons.Right) {
                    SwitchTo((int)AllInputStates.Idle, state);
                }

                return base.OnMouseUp(state);
            }

            public override bool OnMouseMove(InputStateData state) {
                Processor.InvokeHandler<ICameraInputHandler>(x => x.Pan(state));
                return false;
            }
        }
        protected sealed class InputZoomState : CurrentStateMachine {
            public InputZoomState(StateProcessor processor) : base(processor) { }

            public override void EnterState(InputStateData ev) {
                Processor.InvokeHandler<ICameraInputHandler>(x => x.Zoom(ev));
            }

            public override bool OnMouseDown(InputStateData state) {
                SwitchTo((int)AllInputStates.Idle, state);
                return base.KeyDown(state);
            }

            public override bool OnMouseWheel(InputStateData ev) {
                Processor.InvokeHandler<ICameraInputHandler>(x => x.Zoom(ev));
                return true;
            }
            public override bool OnMouseDoubleDown(InputStateData state) {
                SwitchTo((int)AllInputStates.ChangeFocus, state);
                return base.OnMouseDoubleDown(state);
            }

            //public override bool OnMouseMove(InputStateData state) {
            //    SwitchTo((int)AllInputStates.Idle, state);
            //    return false;
            //}
        }

        #endregion

        #region moving

        protected class KeywordMovingState : CurrentStateMachine {
            public KeywordMovingState(StateProcessor processor) : base(processor) { }

            public override void EnterState(InputStateData state) {
                state.IsKeywordDown = true;
                Processor.InvokeHandler<ICameraInputHandler>(x => x.KeywordMove(state));
            }

            public override bool OnMouseMove(InputStateData state) {
                state.IsKeywordDown = false;
                SwitchTo((int)AllInputStates.Idle, state);
                return base.OnMouseMove(state);
            }

            public override bool KeyUp(InputStateData state) {
                state.IsKeywordDown = false;
                switch (state.Keyword) {
                    case GeneralKeywords.W:
                    case GeneralKeywords.S:
                    case GeneralKeywords.A:
                    case GeneralKeywords.D:
                        Processor.InvokeHandler<ICameraInputHandler>(x => x.KeywordMove(state));
                        return true;
                    default:
                        SwitchTo((int)AllInputStates.Idle, state);
                        return false;

                }
            }

            public override bool KeyDown(InputStateData state) {
                state.IsKeywordDown = true;
                switch (state.Keyword) {
                    case GeneralKeywords.W:
                        Processor.InvokeHandler<ICameraInputHandler>(x => x.KeywordMove(state));
                        return true;
                    default:
                        SwitchTo((int)AllInputStates.Idle, state);
                        return false;

                }
            }
        }

        protected class FocusToObjectState : CurrentStateMachine {
            public FocusToObjectState(StateProcessor processor) : base(processor) {}

            public override void EnterState(InputStateData state) {
                Processor.InvokeHandler<ICameraInputHandler>(x => x.FocusToObject(state));
                SwitchTo((int)AllInputStates.Idle, state);
            }
        }

        #endregion


        protected sealed class InputTargetState : CurrentStateMachine {
            public InputTargetState(StateProcessor processor) : base(processor) {

            }

            public override void EnterState(InputStateData state) {
                Processor.InvokeHandler<ITargetingInputHandler>(x => x.TargetCapture(state));
            }

            public override bool OnMouseDown(InputStateData state) {
                switch (state.Buttons) {
                    case GeneralMouseButtons.Left:
                        break;
                }
                return base.OnMouseDown(state);
            }
            public override bool OnMouseUp(InputStateData state) {
                if ((state.Buttons & GeneralMouseButtons.Left) != GeneralMouseButtons.Left) {
                    Processor.InvokeHandler<ITargetingInputHandler>(x => x.UnTarget(state));
                    SwitchTo((int)AllInputStates.Idle, state);
                }

                return base.OnMouseUp(state);
            }
            public override bool OnMouseMove(InputStateData state) {
                Processor.InvokeHandler<ITargetingInputHandler>(x => x.TargetMove(state));
                return true;
            }
        }

        protected override InputState GetIdleState() {//initilization 
            var states = new StateDictionary();

            states.Add((int)AllInputStates.Idle, s => new InputIdleState(s));
            states.Add((int)AllInputStates.Rotate, s => new InputRotateState(s));
            states.Add((int)AllInputStates.Zoom, s => new InputZoomState(s));
            states.Add((int)AllInputStates.Pan, s => new InputPanState(s));
            states.Add((int)AllInputStates.Target, s => new InputTargetState(s));
            states.Add((int)AllInputStates.KeywordDown, s => new KeywordMovingState(s));
            states.Add((int)AllInputStates.ChangeFocus, s => new FocusToObjectState(s));

            var router = new StateHandleProcessor<ICameraInputHandler>(states, this);
            router.SwitchTo((int)AllInputStates.Idle, InputStateData.Create());




            return router;
        }

        protected abstract class CurrentStateMachine : InputStateMachine {
            protected CurrentStateMachine(StateProcessor processor) : base(processor) { }
        }


        readonly FrameworkElement control;

        public CurrentInputObserver(FrameworkElement control, IInputPublisher publisher) : base(publisher) {
            this.currentSnapshot = new InputSnapshot();
            this.control = control;
        }
        public void Zoom(InputStateData state) {
            currentSnapshot.AddEvent(new CameraZoomCommand(state.Clone()));
        }
        public bool Rotate(InputStateData state) {
            currentSnapshot.AddEvent(new CameraRotateCommand(state.Clone()));
            return true;
        }
        public void Pan(InputStateData state) {
            currentSnapshot.AddEvent(new CameraPanCommand(state.Clone()));
        }
        public void FocusToObject(InputStateData state) {
            currentSnapshot.AddEvent(new FocusToObjectCommand(state.Clone()));
        }

        public void Idle() {
            currentSnapshot.AddEvent(new CameraIdleCommand());
        }
               
        public void KeywordMove(InputStateData state) {
            currentSnapshot.AddEvent(new KeywordsMovingCommand(state.Clone()));
        }


        public void TargetCapture(InputStateData state) {
            currentSnapshot.AddEvent(new CaptureTargetUnderMouseCameraCommand(state.Clone()));
        }
        public void TargetMove(InputStateData state) {

        }
        public void UnTarget(InputStateData state) {
            //currentSnapshot.AddEvent(new CaptureTargetUnderMouseCameraCommand(state.Clone()));
        }
    }
}
