using System.Collections.Generic;

namespace D3DLab.ECS {
    public abstract class GameObject {
        public string Description { get; }

        protected GameObject(string desc) {
            Description = desc;
        }

        public abstract void Hide(IEntityManager manager);
        public abstract void Show(IEntityManager manager);

        public virtual void Cleanup(IEntityManager manager) {

        }
        
        public virtual IEnumerable<GraphicEntity> GetEntities(IEntityManager manager) {
            return new GraphicEntity[0];
        }
    }
}
