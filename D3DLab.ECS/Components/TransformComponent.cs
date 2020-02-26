using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace D3DLab.ECS.Components {
    public struct TransformComponent : IGraphicComponent {

        public static TransformComponent Create(Matrix4x4 matrixWorld) {
            return new TransformComponent {
                MatrixWorld = matrixWorld,
                IsModified = true,
                IsValid = true,
                Tag = new ElementTag(Guid.NewGuid().ToString()),
            };
        }

        public static TransformComponent Identity() {
            return new TransformComponent {
                MatrixWorld = Matrix4x4.Identity,
                IsModified = true,
                IsValid = true,
                Tag = new ElementTag(Guid.NewGuid().ToString()),
            };
        }

        public void Dispose() {
            IsDisposed = true;
        }

        public Matrix4x4 MatrixWorld { get; private set; }
        public ElementTag Tag { get; private set; }
        public ElementTag EntityTag { get; set; }
        public bool IsModified { get; set; }
        public bool IsValid { get; private set; }
        public bool IsDisposed { get; private set; }
    }
}
