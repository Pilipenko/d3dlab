using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS.Components {
    public struct PerfomanceComponent : IGraphicComponent {

        public static PerfomanceComponent Create(double millisec, int fps) {
            return new PerfomanceComponent {
                Tag = new ElementTag(Guid.NewGuid().ToString()),
                ElapsedMilliseconds = millisec,
                FPS = fps,
            };
        }

        public ElementTag Tag { get; private set; }
        public ElementTag EntityTag { get; set; }
        public bool IsModified { get; set; }
        public bool IsValid { get; }
        public bool IsDisposed { get; private set; }

        public double ElapsedMilliseconds;
        public double FPS;

        public override string ToString() {
            return $"Perfomance[ElapsedMilliseconds:{ElapsedMilliseconds} FPS:{FPS}]";
        }

        public void Dispose() {
            IsDisposed = true;
        }
    }
}
