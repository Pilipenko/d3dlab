using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace D3DLab.ECS.Components {
    public struct LightComponent : IGraphicComponent {

        public static LightComponent Create(float intensity, int index, Vector3 direction, Vector3 position, LightTypes type) {
            return new LightComponent {
                Tag = new ElementTag(Guid.NewGuid().ToString()),
                Intensity = intensity,
                Index = index,
                Direction = direction,
                Position = position,
                Type = type
            };
        }
        public static LightComponent CreateDirectional(float intensity, int index, Vector3 direction) {
            return new LightComponent {
                Tag = new ElementTag(Guid.NewGuid().ToString()),
                Intensity = intensity,
                Index = index,
                Direction = direction,
                Type = LightTypes.Directional,
            };
        }
        public static LightComponent CreatePoint(float intensity, int index, Vector3 position) {
            return new LightComponent {
                Tag = new ElementTag(Guid.NewGuid().ToString()),
                Intensity = intensity,
                Index = index,
                Position = position,
                Type = LightTypes.Point,
            };
        }
        public static LightComponent CreateAmbient(float intensity, int index) {
            return new LightComponent {
                Tag = new ElementTag(Guid.NewGuid().ToString()),
                Intensity = intensity,
                Index = index,
                Type = LightTypes.Ambient,
            };
        }

        /// <summary>
        /// 0 - 1 range
        /// </summary>
        public float Intensity;
        public int Index;
        public Vector3 Position;
        public Vector3 Direction;
        public LightTypes Type;

        public ElementTag Tag { get; private set; }
        public ElementTag EntityTag { get; set; }
        public bool IsModified { get; set; }
        public bool IsValid { get; private set; }
        public bool IsDisposed { get; private set; }


        public void Dispose() {
            IsDisposed = true;
        }
    }
}
