using D3DLab.ECS.Camera;
using D3DLab.ECS.Input;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace D3DLab.ECS {
   

    public enum LightTypes : uint {
        Undefined = 0,
        Ambient = 1,
        Point = 2,
        Directional = 3
    }

    public struct LightState {
        public float Intensity;
        public Vector3 Position;
        public Vector3 Direction;
        public LightTypes Type;
        public Vector4 Color;
    }

    public interface ISceneSnapshot {
        //IViewport Viewport { get; }
        //IContextState ContextState { get; }
        IManagerChangeNotify Notifier { get; }
        IAppWindow Window { get; }

        InputSnapshot InputSnapshot { get; }
        TimeSpan FrameRateTime { get; }

        ElementTag CurrentCameraTag { get; }

        CameraState Camera { get;  }
        LightState[] Lights { get; }


        void UpdateCamera(ElementTag tag, CameraState state);
        void UpdateLight(int index, LightState state);
    }
}
