using D3DLab.ECS;
using D3DLab.ECS.Systems;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Components;
using D3DLab.Wpf.Engine.App.D3D;
using System.Threading;

namespace D3DLab.Wpf.Engine.App.GameObjects {
    public class CameraGameObject : SingleGameObject {
        static int cameras = 0;

        public CameraGameObject(ElementTag tag, string descr) : base(tag, descr) { }

        public static CameraGameObject Create(IContextState context) {
            IEntityManager manager = context.GetEntityManager();
            var cameraTag = new ElementTag("CameraEntity_" + Interlocked.Increment(ref cameras));

            var obj = new CameraGameObject(cameraTag, "PerspectiveCamera");

            manager.CreateEntity(cameraTag)
                   //.AddComponent(new OrthographicCameraComponent(Window.Width, Window.Height));
                   .AddComponent(new PerspectiveCameraComponent());

            {//entities ordering 
                context.EntityOrder
                       .RegisterOrder<RenderSystem>(cameraTag, 0)
                       .RegisterOrder<DefaultInputSystem>(cameraTag, 0);
            }

            return obj;
        }

    }
}
