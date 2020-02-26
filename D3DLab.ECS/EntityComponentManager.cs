using D3DLab.ECS;
using System;
using System.Collections.Generic;
using System.Linq;

namespace D3DLab.ECS {
    public sealed class EntityComponentManager : IEntityManager, IComponentManager {

        #region IEntityManager

        readonly HashSet<ElementTag> entities = new HashSet<ElementTag>();
        Func<ElementTag, bool> predicate = x => true;
        readonly IManagerChangeNotify notify;

        public GraphicEntity CreateEntity(ElementTag tag) {
            var en = _CreateEntity(tag);

            entitySynchronizer.Add((owner, input) => {
                owner.entities.Add(tag);
                owner.notify.NotifyChange(input);
                owner.components.Add(input.Tag, new Dictionary<ElementTag, IGraphicComponent>());
                entityHas.Add(input.Tag, new HashSet<Type>());
            }, en);

            return en;
        }

        public void RemoveEntity(ElementTag elementTag) {
            entitySynchronizer.Add((owner, input) => {
                if (owner.entities.Contains(elementTag)) {
                    var entity = _CreateEntity(elementTag);

                    foreach (var component in owner.GetComponents(entity.Tag)) {
                        owner._RemoveComponent(entity.Tag, component);
                    }
                    owner.entities.Remove(entity.Tag);
                    owner.components.Remove(entity.Tag);
                    entityHas.Remove(entity.Tag);

                    notify.NotifyChange(entity);
                }
            }, null);
        }

        public IEnumerable<GraphicEntity> GetEntities() {
            return entities.Where(predicate).Select(_CreateEntity);
        }
        public GraphicEntity GetEntity(ElementTag tag) {
            if (!entities.Contains(tag)) {
                return GraphicEntity.Empty();
                //throw new Exception($"There is no {tag} ");
            }
            return new GraphicEntity(tag, this, this, orderContainer);
        }
        public IEnumerable<GraphicEntity> GetEntity(Func<GraphicEntity, bool> predicate) {
            var res = new List<GraphicEntity>();
            foreach (var tag in entities) {
                var en = _CreateEntity(tag);
                if (predicate(en)) {
                    res.Add(en);
                }
            }
            return res;
        }

        public void SetFilter(Func<ElementTag, bool> predicate) {
            this.predicate = predicate;
        }

        public bool IsExisted(ElementTag tag) {
            return entities.Contains(tag);
        }

        GraphicEntity _CreateEntity(ElementTag tag) {
            return new GraphicEntity(tag, this, this, orderContainer);
        }

        #endregion

        #region IComponentManager

        //TODO:
        readonly Dictionary<ElementTag, HashSet<Type>> entityHas = new Dictionary<ElementTag, HashSet<Type>>();
        readonly Dictionary<ElementTag, Dictionary<ElementTag,IGraphicComponent>> components = new Dictionary<ElementTag, Dictionary<ElementTag, IGraphicComponent>>();
        readonly Dictionary<IFlyweightGraphicComponent, HashSet<ElementTag>> flyweightComponents = new Dictionary<IFlyweightGraphicComponent, HashSet<ElementTag>>();
        readonly EntityOrderContainer orderContainer;

        public void AddComponents(ElementTag tagEntity, IEnumerable<IGraphicComponent> com) {
            comSynchronizer.AddRange((owner, inp) => {
                owner._AddComponent(tagEntity, inp);
            }, com);
        }
        public IGraphicComponent AddComponent(ElementTag tagEntity, IGraphicComponent com) {
            comSynchronizer.Add((owner, inp) => {
                owner._AddComponent(tagEntity, inp);
            }, com);
            return com;
        }
        public void RemoveComponent(ElementTag tagEntity, IGraphicComponent com) {
            comSynchronizer.Add((owner, inp) => {
                owner._RemoveComponent(tagEntity, inp);
            }, com);
        }
        public void RemoveComponents<T>(ElementTag tagEntity) where T : IGraphicComponent {
            foreach (var com in GetComponents<T>(tagEntity).ToList()) {
                comSynchronizer.Add((owner, c) => {
                    owner._RemoveComponent(tagEntity, c);
                }, com);
            }
        }

        public T GetComponent<T>(ElementTag tagEntity) where T : IGraphicComponent {
            return components[tagEntity].Values.OfType<T>().Single();
        }
        public IEnumerable<T> GetComponents<T>(ElementTag tagEntity) where T : IGraphicComponent {
            return components[tagEntity].Values.OfType<T>();
        }
        public IEnumerable<IGraphicComponent> GetComponents(ElementTag tagEntity) {
            if (!components.ContainsKey(tagEntity)) {
                return new IGraphicComponent[0];
            }
            return components[tagEntity].Values.ToArray();
        }
        public bool Has<T>(ElementTag tag) where T : IGraphicComponent {
            return components[tag].Any(x => x.Value is T);
        }
        public bool Has(ElementTag tag, params Type[] types) {
            return types.All(type => entityHas[tag].Contains(type));
        }
        public IEnumerable<IGraphicComponent> GetComponents(ElementTag tag, params Type[] types) {
            //TODO: temporary decision
            return components[tag].Values.Where(x => types.Any(t => t == x.GetType()));
        }
        public T GetOrCreateComponent<T>(ElementTag tagEntity, T newone) where T : IGraphicComponent {
            var any = GetComponents<T>(tagEntity);
            if (any.Any()) {
                return any.Single();
            }
            AddComponent(tagEntity, newone);
            return newone;
        }

        public void UpdateComponents<T>(ElementTag tagEntity, T com) where T : IGraphicComponent {
            comSynchronizer.Add((owner, inp) => {
                var any = GetComponents<T>(tagEntity);
                if (any.Any()) {
                    var old = any.Single();
                    var removed = components[tagEntity].Remove(old.Tag);
                    old.Dispose();
                }

                com.EntityTag = tagEntity;
                components[tagEntity].Add(com.Tag, com);
                
                notify.NotifyChange(com);
            }, com);
        }

        public IFlyweightGraphicComponent AddComponent(ElementTag tagEntity, IFlyweightGraphicComponent com) {
            flyweightComSynchronizer.Add((owner, inp) => {
                owner._AddComponent(tagEntity, inp);
            }, com);
            return com;
        }
        public void RemoveComponent(ElementTag tagEntity, IFlyweightGraphicComponent com) {
            flyweightComSynchronizer.Add((owner, inp) => {
                owner._RemoveComponent(tagEntity, inp);
            }, com);
        }

        void _AddComponent(ElementTag tagEntity, IFlyweightGraphicComponent com) {
            if (flyweightComponents.ContainsKey(com)) {
                flyweightComponents[com].Add(tagEntity);
            } else {
                flyweightComponents.Add(com, new HashSet<ElementTag> { tagEntity });
            }

            entityHas[tagEntity].Add(com.GetType());
            notify.NotifyChange(com);
        }
        void _RemoveComponent(ElementTag tagEntity, IFlyweightGraphicComponent com) {
            if (!flyweightComponents[com].Remove(tagEntity)) {
                //no data to remove - dispose comp
                com.Dispose();
            }
            entityHas[tagEntity].Remove(com.GetType());
            notify.NotifyChange(com);
        }

        void _AddComponent(ElementTag tagEntity, IGraphicComponent com) {
            com.EntityTag = tagEntity;
            components[tagEntity].Add(com.Tag, com);
            entityHas[tagEntity].Add(com.GetType());
            notify.NotifyChange(com);
        }
        void _RemoveComponent(ElementTag tagEntity, IGraphicComponent com) {
            var removed = components[tagEntity].Remove(com.Tag);
            removed = entityHas[tagEntity].Remove(com.GetType());
            com.Dispose();
            notify.NotifyChange(com);
        }

        #endregion

        public bool HasChanges {
            get {
                return entitySynchronizer.IsChanged || comSynchronizer.IsChanged || frameChanges;
            }
        }

        SynchronizationContext<EntityComponentManager, GraphicEntity> entitySynchronizer;
        SynchronizationContext<EntityComponentManager, IGraphicComponent> comSynchronizer;
        SynchronizationContext<EntityComponentManager, IFlyweightGraphicComponent> flyweightComSynchronizer;

        public EntityComponentManager(IManagerChangeNotify notify, EntityOrderContainer orderContainer) {
            this.orderContainer = orderContainer;
            this.notify = notify;
            entitySynchronizer = new SynchronizationContext<EntityComponentManager, GraphicEntity>(this);
            comSynchronizer = new SynchronizationContext<EntityComponentManager, IGraphicComponent>(this);
            flyweightComSynchronizer = new SynchronizationContext<EntityComponentManager, IFlyweightGraphicComponent>(this);
        }

        public void Synchronize(int theadId) {
            frameChanges = false;
            entitySynchronizer.Synchronize(theadId);
            comSynchronizer.Synchronize(theadId);
            flyweightComSynchronizer.Synchronize(theadId);
        }

        bool frameChanges;
        //not a good decision :(
        public void FrameSynchronize(int theadId) {
            if (!frameChanges) {
                frameChanges = HasChanges;
            }
            entitySynchronizer.Synchronize(theadId);
            comSynchronizer.Synchronize(theadId);
            flyweightComSynchronizer.Synchronize(theadId);
        }

        public void PushSynchronization() {
            frameChanges = true;
        }

        public void Dispose() {
            foreach (var coms in components) {
                foreach (var com in coms.Value) {
                    com.Value.Dispose();
                }
            }
            entitySynchronizer.Dispose();
            comSynchronizer.Dispose();
        }
    }
}
