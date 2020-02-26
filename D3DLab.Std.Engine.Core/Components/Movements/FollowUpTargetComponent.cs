using D3DLab.ECS;
using D3DLab.ECS.Components;
using System.Numerics;

namespace D3DLab.Std.Engine.Core.Components.Movements {
    public abstract class FollowUpTargetComponent : MovementComponent {
        public override void Execute(IMovementComponentHandler handler) {
            handler.Execute(this);
        }

        public abstract void Follow(GraphicEntity follower, GraphicEntity target);
        public abstract bool IsTarget(GraphicEntity target);
    }

    public class FollowUpCameraPositionComponent : FollowUpTargetComponent {     
        public override void Follow(GraphicEntity follower, GraphicEntity target) {

            var cameraPostion = target.GetComponent<GeneralCameraComponent>().Position;

            var worldMatrix = Matrix4x4.CreateTranslation(cameraPostion.X, cameraPostion.Y, cameraPostion.Z);

            follower.UpdateComponent(TransformComponent.Create(worldMatrix));
        }

        public override bool IsTarget(GraphicEntity target) {
            return target.Has<GeneralCameraComponent>();
        }
    }

}
