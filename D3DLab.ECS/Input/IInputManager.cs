using D3DLab.ECS;
using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS.Input {
    public interface IInputManager : ISynchronizationContext {
        void PushCommand(IInputCommand cmd);
        InputSnapshot GetInputSnapshot();
        void Dispose();
    }
}
