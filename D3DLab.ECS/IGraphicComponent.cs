using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS {
    public interface IGraphicComponent : IDisposable {
        ElementTag Tag { get; }
        ElementTag EntityTag { get; set; }
        [Obsolete("Not usable fot struct, remote later")]
        bool IsModified { get; set; }
        bool IsValid { get; }
        bool IsDisposed { get; }
    }

    public interface IFlyweightGraphicComponent : IDisposable {
        ElementTag Tag { get; }
        bool IsModified { get; set; }
        bool IsDisposed { get; }
    }
}
