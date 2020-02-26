using D3DLab.ECS;
using D3DLab.ECS.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace D3DLab.ECS {

    public sealed class GraphicEntity  {

        struct EmptyManager : IComponentManager, IEntityManager {
            public bool HasChanges => false;
            public IGraphicComponent AddComponent(ElementTag tagEntity, IGraphicComponent com) => EmptyGraphicComponent.Create();
            public void AddComponents(ElementTag tagEntity, IEnumerable<IGraphicComponent> com) { }
            public GraphicEntity CreateEntity(ElementTag tag) => GraphicEntity.Empty();
            public void Dispose() {            }
            public void FrameSynchronize(int theadId) {            }
            public IEnumerable<IGraphicComponent> GetComponents(ElementTag tagEntity) => Enumerable.Empty<IGraphicComponent>();
            public IEnumerable<IGraphicComponent> GetComponents(ElementTag tag, params Type[] types) => Enumerable.Empty<IGraphicComponent>();
            public IEnumerable<GraphicEntity> GetEntities() => Enumerable.Empty<GraphicEntity>();
            public GraphicEntity GetEntity(ElementTag tag) => GraphicEntity.Empty();
            public IEnumerable<GraphicEntity> GetEntity(Func<GraphicEntity, bool> predicate) => Enumerable.Empty<GraphicEntity>();
            public IEnumerable<T> GetComponents<T>(ElementTag tagEntity) where T : IGraphicComponent => Enumerable.Empty<T>();
            public bool Has<T>(ElementTag tag) where T : IGraphicComponent => false;
            public bool Has(ElementTag tag, params Type[] types) => false;
            public bool IsExisted(ElementTag tag) => false;
            public void PushSynchronization() {}
            public void RemoveComponent(ElementTag tagEntity, IGraphicComponent com) {            }
            public void RemoveComponents<T>(ElementTag tagEntity) where T : IGraphicComponent {            }
            public void RemoveEntity(ElementTag elementTag) {           }
            public void SetFilter(Func<ElementTag, bool> predicate) {           }
            public void Synchronize(int theadId) { }
            public void UpdateComponents<T>(ElementTag tagEntity, T com) where T : IGraphicComponent {}


            //TODO refactor! should not be methods with no results
            public T GetComponent<T>(ElementTag tagEntity) where T : IGraphicComponent {
                throw new Exception("Empty graphic Entity does not have any components.");
            }
            public T GetOrCreateComponent<T>(ElementTag tagEntity, T newone) where T : IGraphicComponent {
                throw new Exception("Empty graphic Entity does not have any components.");
            }
        }


        public static GraphicEntity Empty() {
            return new GraphicEntity(ElementTag.Empty, new EmptyManager(), new EmptyManager(), new EntityOrderContainer());
        }


        public ElementTag Tag { get; }
        readonly IComponentManager manager;
        readonly IEntityManager emanager;
        readonly EntityOrderContainer order;

        public GraphicEntity(ElementTag tag, IComponentManager manager, IEntityManager emanager,EntityOrderContainer order) {
            this.order = order;
            this.manager = manager;
            this.emanager = emanager;
            Tag =tag;
           
        }

        public T GetComponent<T>() where T : IGraphicComponent {
            return manager.GetComponent<T>(Tag);
        }
        public IEnumerable<T> GetComponents<T>() where T : IGraphicComponent {
            return manager.GetComponents<T>(Tag);
        }
        public IEnumerable<IGraphicComponent> GetComponents(params Type[] types) {
            return manager.GetComponents(Tag, types);
        }

        public T GetOrCreateComponent<T>(T newone) where T : IGraphicComponent {
            return manager.GetOrCreateComponent<T>(Tag, newone);
        }

        public GraphicEntity AddComponent<T>(T component) where T : IGraphicComponent {
            manager.AddComponent(Tag, component);
            return this;
        }
        public GraphicEntity AddComponents(params IGraphicComponent[] components){
            manager.AddComponents(Tag, components);
            return this;
        }
        public GraphicEntity AddComponents(IEnumerable<IGraphicComponent> components) {
            manager.AddComponents(Tag, components);
            return this;
        }
        public void RemoveComponent(IGraphicComponent component) {
            manager.RemoveComponent(Tag, component);            
        }

        public void Remove() {
            emanager.RemoveEntity(Tag);
        }

        public void RemoveComponents<T>() where T : IGraphicComponent {
            manager.RemoveComponents<T>(Tag);
        }
        public void RemoveComponentsOfType<TCom>() where TCom : IGraphicComponent {
            foreach (var component in manager.GetComponents<TCom>(Tag)) {
                manager.RemoveComponent(Tag, component);
            }
        }

        public bool Has(params Type[] types) {
            return manager.Has(Tag, types);
        }
        public bool Has<T>() where T : IGraphicComponent {
            return manager.Has<T>(Tag);
        }
        public IEnumerable<IGraphicComponent> GetComponents() {
            return manager.GetComponents(Tag);
        }

        public int GetOrderIndex<TSys>()
            where TSys : IGraphicSystem {
            return order.Get<TSys>(Tag);
        }

        public void UpdateComponent<T>(T com) where T : IGraphicComponent {
            manager.UpdateComponents(Tag, com);
        }

        public bool IsDestroyed => !emanager.IsExisted(Tag);

        public override string ToString() {
            return $"Entity[{Tag}]";
        }
    }
    public class OrderSystemContainer : Dictionary<Type, int> {

    }
    public class EntityOrderContainer {
        readonly Dictionary<ElementTag, OrderSystemContainer> componentOrderIndex;
        readonly Dictionary<Type, int> systemsOrder;

        public EntityOrderContainer() {
            componentOrderIndex = new Dictionary<ElementTag, OrderSystemContainer>();
            systemsOrder = new Dictionary<Type, int>();
        }
        public EntityOrderContainer RegisterOrder<TSys>(ElementTag tag,int index) {
            OrderSystemContainer ordering;
            if (!componentOrderIndex.TryGetValue(tag, out ordering)) {
                ordering = new OrderSystemContainer();
                componentOrderIndex.Add(tag, ordering);
            }
            var t = typeof(TSys);

            ordering.Add(t, index);
            IncrementSystemOrderIndex(t);

            return this;
        }

        public EntityOrderContainer RegisterOrder<TSys>(ElementTag tag) {
            OrderSystemContainer ordering;
            if (!componentOrderIndex.TryGetValue(tag, out ordering)) {
                ordering = new OrderSystemContainer();
                componentOrderIndex.Add(tag, ordering);
            }
            var t = typeof(TSys);

            ordering.Add(t, IncrementSystemOrderIndex(t));

            return this;
        }

        public int Get<TSys>(ElementTag tag)
            where TSys : IGraphicSystem {
            if (!componentOrderIndex.ContainsKey(tag)) {
                return int.MaxValue;
            }
            return componentOrderIndex[tag][typeof(TSys)];
        }

        int IncrementSystemOrderIndex(Type t) {
            if (!systemsOrder.ContainsKey(t)) {
                systemsOrder.Add(t, 0);
            } else {
                systemsOrder[t] = systemsOrder[t] + 1;
            }
            return systemsOrder[t];
        }
    }
}
