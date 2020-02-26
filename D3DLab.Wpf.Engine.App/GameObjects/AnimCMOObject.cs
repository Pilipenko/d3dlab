using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using D3DLab.SDX.Engine.Animation;
using D3DLab.SDX.Engine.Components;
using D3DLab.Std.Engine.Core;
using D3DLab.Std.Engine.Core.Ext;
using D3DLab.Std.Engine.Core.Animation.Formats;
using D3DLab.Std.Engine.Core.Common;
using D3DLab.Std.Engine.Core.Components;
using SharpDX.Direct3D11;
using D3DLab.Std.Engine.Core.Systems;
using D3DLab.Std.Engine.Core.Utilities;
using D3DLab.Physics.Engine;
using D3DLab.ECS;
using D3DLab.ECS.Ext;
using D3DLab.ECS.Components;

namespace D3DLab.Wpf.Engine.App.GameObjects {
    
    public class AnimCMOObject : SingleGameObject {//SingleGameObject
        public ElementTag Tag { get; }

        public AnimCMOObject(ElementTag tag) : base(tag, "AnimCMOObject") {
            Tag = tag;
        }

        public static SingleGameObject Create(IEntityManager manager) {

            var tag = new ElementTag("Anim CMO file");

			var fileName = Path.Combine($"{Directory.GetCurrentDirectory()}", $"Resources", $"animation", $"Chick.cmo");
			//var fileName = @"C:\Storage\projects\sv\3d\d3dlab\D3DLab.Wpf.Engine.App\Resources\animation\Chick.cmo";
			var meshes = CMOAnimationLoader.LoadFromFile(fileName);

            var mesh = meshes.First();

            var animesh = new MeshAnimationComponent("Armature|ArmatureAction.002");
            var render = new D3DAnimRenderComponent();
            var texture = new D3DTexturedMaterialSamplerComponent(new SamplerStateDescription() {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                BorderColor = new SharpDX.Color4(0, 0, 0, 0),
                ComparisonFunction = Comparison.Never,
                Filter = Filter.MinMagMipLinear,
                MaximumAnisotropy = 16,
                MaximumLod = float.MaxValue,
                MinimumLod = 0,
                MipLodBias = 0.0f
            });

            var moveToZ = Matrix4x4.CreateTranslation(Vector3.UnitY * 20);
            var rotateToY = Matrix4x4.CreateRotationX(-90f.ToRad());

            manager.CreateEntity(tag)
                .AddComponents(new IGraphicComponent[] {
                    mesh,
                    render,
                    animesh,
                    texture,
                    TransformComponent.Create(rotateToY * moveToZ),
                   // PhysicalComponentFactory.CreateAABB(),
                });

           // new SingleGameObject( EntityBuilders.BuildMeshElement(manager, pos, indx, V4Colors.Red),"test");
           
            return new AnimCMOObject(tag);
        }

        public override void Hide(IEntityManager manager) {
           
        }

        public override void Show(IEntityManager manager) {

        }
    }
}
