using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS.Input {
    public interface IInputCommand {
        InputStateData InputState { get; }
        bool Execute(GraphicEntity entity);
    }
}
