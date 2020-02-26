using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Linq;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Systems;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Animation.Formats;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Animation;
using System.Collections.Generic;
using D3DLab.ECS;

namespace D3DLab.SDX.Engine.Animation {
    public class MeshAnimationComponent : GraphicComponent {
        public const int Slot = 4;

        public Matrix4x4[] Bones;

        public readonly string AnimationName;

        public MeshAnimationComponent(string animationName) {            
            this.AnimationName = animationName;
        }        
    }
}
