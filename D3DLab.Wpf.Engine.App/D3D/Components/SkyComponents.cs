using D3DLab.ECS;
using D3DLab.ECS.Common;
using D3DLab.SDX.Engine.Components;
using D3DLab.SDX.Engine.Rendering;
using D3DLab.SDX.Engine.Shader;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Systems;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace D3DLab.Wpf.Engine.App.D3D.Components {
    public class SkyPlaneParallaxAnimationComponent : GraphicComponent, IAnimationComponent {
        public Vector2 translate1;
        public Vector2 translate2;

        Vector2 vectorSpeed1;
        Vector2 vectorSpeed2;

        public SkyPlaneParallaxAnimationComponent() {
            // texture translation speed.
            vectorSpeed1 = new Vector2(0.0003f, 0.0f);
            vectorSpeed2 = new Vector2(0.00015f, 0.0f);
            IsModified = true;
        }

        public void Animate(GraphicEntity owner, TimeSpan frameRateTime) {
            // Increment the translation values to simulate the moving clouds.
            // implementation for comparing FPS, when not using VSync.
            translate1 += vectorSpeed1;
            translate2 += vectorSpeed2;

            // Keep the values in the zero to one range.
            if (translate1.X > 1.0f)
                translate1.X -= 1.0f;
            if (translate1.Y > 1.0f)
                translate1.Y -= 1.0f;
            if (translate2.X > 1.0f)
                translate2.X -= 1.0f;
            if (translate2.Y > 1.0f)
                translate2.Y -= 1.0f;

            IsModified = true;
        }
    }


    public class D3DSkyPlaneRenderComponent : D3DRenderComponent {
        [IgnoreDebuging]
        public SharpDX.Direct3D11.Buffer ParallaxAnimation { get; set; }
        [IgnoreDebuging]
        internal EnumerableDisposableSetter<ShaderResourceView[]> TextureResources { get; set; }
        [IgnoreDebuging]
        internal DisposableSetter<SamplerState> SampleState { get; set; }

        public D3DSkyPlaneRenderComponent() {
            SampleState = new DisposableSetter<SamplerState>();
            TextureResources = new EnumerableDisposableSetter<ShaderResourceView[]>();
        }

        public override void Dispose() {
            base.Dispose();
            Disposer.DisposeAll(ParallaxAnimation, SampleState, TextureResources);
        }
    }

    /// <summary>
    
    /// </summary>
    public class D3DSkyRenderComponent : D3DRenderComponent {
        public SharpDX.Direct3D11.Buffer GradientBuffer { get; set; }


        public override void Dispose() {
            base.Dispose();
            Disposer.DisposeAll(GradientBuffer);
        }
    }

}
