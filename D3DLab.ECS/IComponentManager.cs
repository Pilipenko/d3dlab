using System;
using System.Collections.Generic;

namespace D3DLab.ECS {

    public interface IComponentManager : ISynchronizationContext {
        void AddComponents(ElementTag tagEntity, IEnumerable<IGraphicComponent> com);
        IGraphicComponent AddComponent(ElementTag tagEntity, IGraphicComponent com);
        void RemoveComponent(ElementTag tagEntity, IGraphicComponent com);
        void RemoveComponents<T>(ElementTag tagEntity) where T : IGraphicComponent;


        T GetComponent<T>(ElementTag tagEntity) where T : IGraphicComponent;
        IEnumerable<T> GetComponents<T>(ElementTag tagEntity) where T : IGraphicComponent;
        IEnumerable<IGraphicComponent> GetComponents(ElementTag tagEntity);
        T GetOrCreateComponent<T>(ElementTag tagEntity, T newone) where T : IGraphicComponent;

        bool Has<T>(ElementTag tag) where T : IGraphicComponent;
        bool Has(ElementTag tag, params Type[] types);


        IEnumerable<IGraphicComponent> GetComponents(ElementTag tag, params Type[] types);

        void UpdateComponents<T>(ElementTag tagEntity, T com) where T : IGraphicComponent;

        void Dispose();
    }

}
