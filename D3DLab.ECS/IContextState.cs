using D3DLab.ECS.Camera;
using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS {
    public interface IContextState {
        void BeginState();
        void EndState();
        IComponentManager GetComponentManager();
        IEntityManager GetEntityManager();
        ISystemManager GetSystemManager();
        void SwitchTo(int stateTo);
        EntityOrderContainer EntityOrder { get; }

        void Dispose();
    }

    public interface IRenderState {
        IContextState ContextState { get; }
        float Ticks { get; }
        IAppWindow Window { get; }
        CameraState Camera { get; }
    }
}
