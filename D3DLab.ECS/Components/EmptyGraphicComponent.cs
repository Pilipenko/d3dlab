using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS.Components {
    public struct EmptyGraphicComponent : IGraphicComponent {

        public static EmptyGraphicComponent Create() {
            return new EmptyGraphicComponent {
                Tag = ElementTag.Empty,
                EntityTag = ElementTag.Empty,
            };
        }

        public ElementTag Tag { get; private set; }
        public ElementTag EntityTag { get; set; }
        public bool IsModified { get; set; }
        public bool IsValid { get; }
        public bool IsDisposed { get; }

        public void Dispose() {

        }
    }
}
