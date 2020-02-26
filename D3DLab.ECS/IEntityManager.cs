using System;
using System.Collections.Generic;

namespace D3DLab.ECS {
    public interface IEntityManager : ISynchronizationContext {
        bool HasChanges { get; }

        GraphicEntity CreateEntity(ElementTag tag);
        IEnumerable<GraphicEntity> GetEntities();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>
        /// If etity by TAG was not found, returns empty GraphicEntity without components.
        /// 
        /// </returns>
        GraphicEntity GetEntity(ElementTag tag);
        IEnumerable<GraphicEntity> GetEntity(Func<GraphicEntity, bool> predicate);
        bool IsExisted(ElementTag tag);

        void SetFilter(Func<ElementTag, bool> predicate);
        void Dispose();

        void RemoveEntity(ElementTag elementTag);

        void FrameSynchronize(int theadId);
        void PushSynchronization();
    }

}
