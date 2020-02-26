using D3DLab.ECS;
using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS.Components {
    public interface IRenderableComponent : IGraphicComponent {
        [Obsolete("Flag is not good decision, component should be removed that case")]
        bool CanRender { get; set; }
    }
}
