using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace D3DLab.ECS.Input {
    [Flags]
    public enum GeneralMouseButtons {
        None = 0,
        Left = 1,
        Right = 2,
        Middle = 3,
        XButton1 = 4,
        XButton2 = 5
    }
    [Flags]
    public enum GeneralKeywords {
        None = 0,
        W,S,A,D

    }

    public struct WindowPoint {
        public readonly int X;
        public readonly int Y;
        public WindowPoint(int x, int y) {
            X = x;
            Y = y;
        }
    }

    #region input data

    public enum ButtonStates {
        Undefined,
        Released,
        Pressed 
    }
    public struct ButtonsState {
        public ButtonStates Condition { get; set; }
        public Vector2 PointV2 { get; set; }
        public WindowPoint CursorPoint { get; set; }
    }
    public class InputStateData {
        public GeneralKeywords Keyword { get; set; }
        public GeneralMouseButtons Buttons { get; set; }
        public Vector2 PrevPosition { get; set; }
        public Vector2 CurrentPosition { get; set; }//=> control.PointToClient(CursorCurrentPosition).ToVector2();
        public WindowPoint CursorCurrentPosition { get; set; }
        public int Delta { get; set; }
        public Dictionary<GeneralMouseButtons, ButtonsState> ButtonsStates {
            get {
                return buttonsStates;
            }
        }

        public bool IsKeywordDown { get; set; }
        public int ClickCount { get; set; }

        readonly Dictionary<GeneralMouseButtons, ButtonsState> buttonsStates;
        public bool IsPressed(GeneralMouseButtons button) {
            return (Buttons & button) == button;
        }

        public InputStateData(Dictionary<GeneralMouseButtons, ButtonsState> buttonsStates) {
            this.buttonsStates = buttonsStates;
            Delta = 0;
            CurrentPosition = Vector2.Zero;
            CursorCurrentPosition = new WindowPoint();
            Buttons = GeneralMouseButtons.None;
        }

        public static InputStateData Create() {
            var buttonsStates = new Dictionary<GeneralMouseButtons, ButtonsState> {
                { GeneralMouseButtons.Right, new ButtonsState() },
                { GeneralMouseButtons.Left, new ButtonsState() },
                { GeneralMouseButtons.Middle, new ButtonsState() }
            };
            return new InputStateData(buttonsStates);
        }

        public InputStateData Clone() {
            var buttonsStates = new Dictionary<GeneralMouseButtons, ButtonsState>();
            foreach (var i in this.buttonsStates) {
                buttonsStates.Add(i.Key, i.Value);
            }
            var cloned = new InputStateData(buttonsStates);
            cloned.Buttons = Buttons;
            cloned.CurrentPosition = CurrentPosition;
            cloned.PrevPosition = PrevPosition;
            cloned.CursorCurrentPosition = CursorCurrentPosition;
            cloned.Delta = Delta;
            cloned.IsKeywordDown = IsKeywordDown;
            cloned.Keyword = Keyword;

            return cloned;
        }
    }

    #endregion
}
