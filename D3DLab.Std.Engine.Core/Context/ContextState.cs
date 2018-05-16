﻿using System;
using System.Collections.Generic;

namespace D3DLab.Std.Engine.Core {
    public interface IContextState {
        void BeginState();
        void EndState();
        IComponentManager GetComponentManager();
        IEntityManager GetEntityManager();
        ISystemManager GetSystemManager();
        void SwitchTo(int stateTo);
        EntityOrderContainer EntityOrder { get; }

        void Dispose();
    }

    public sealed class ManagerContainer {
        public ISystemManager SystemManager { get; }
        public IComponentManager ComponentManager { get; }
        public IEntityManager EntityManager { get; }
        public EntityOrderContainer EntityOrder { get; }

        public ManagerContainer(IManagerChangeNotify notify) {
            EntityOrder = new EntityOrderContainer();
            this.SystemManager = new SystemManager(notify);
            var encom = new EntityComponentManager(notify, EntityOrder);
            this.ComponentManager = encom;
            this.EntityManager = encom;
        }

        public void Dispose() {
            SystemManager.Dispose();
            ComponentManager.Dispose();
            EntityManager.Dispose();
        }
    }

    public abstract class BaseContextState : IContextState {
        readonly ContextStateProcessor processor;

        public BaseContextState(ContextStateProcessor processor) {
            this.processor = processor;
        }

        public virtual void SwitchTo(int stateTo) {
            processor.SwitchTo(stateTo);
        }
        public virtual void EndState() { }
        public virtual void BeginState() { }

        public virtual IComponentManager GetComponentManager() { return processor.Managers.ComponentManager; }
        public virtual IEntityManager GetEntityManager() { return processor.Managers.EntityManager; }
        public virtual ISystemManager GetSystemManager() { return processor.Managers.SystemManager; }
        public EntityOrderContainer EntityOrder { get { return processor.Managers.EntityOrder; } }

        public void Dispose() {
            processor.Managers.Dispose();
        }
    }

    public sealed class ContextStateProcessor : IContextState {
        private sealed class EmptyContextState : IContextState {
            public EntityOrderContainer EntityOrder => throw new NotImplementedException();
            public void BeginState() { }
            public void EndState() { }
            public IComponentManager GetComponentManager() { throw new NotImplementedException(); }
            public IEntityManager GetEntityManager() { throw new NotImplementedException(); }
            public ISystemManager GetSystemManager() { throw new NotImplementedException(); }
            public void SwitchTo(int stateTo) { throw new NotImplementedException(); }
            public void Dispose() {}
        }

        IContextState currentState;
        private readonly Dictionary<int, Func<ContextStateProcessor, IContextState>> states;

        internal ManagerContainer Managers { get; private set; }

        public ContextStateProcessor(ManagerContainer managers) {
            states = new Dictionary<int, Func<ContextStateProcessor, IContextState>>();
            states.Add(-1, x => new EmptyContextState());
            currentState = new EmptyContextState();

            Managers = managers;
        }

        public void AddState(int stateTo, Func<ContextStateProcessor, IContextState> func) {
            states.Add(stateTo, func);
        }

        public void SwitchTo(int stateTo) {
            currentState.EndState();
            currentState = states[stateTo](this);
            currentState.BeginState();
        }

        public ISystemManager GetSystemManager() {
            return currentState.GetSystemManager();
        }

        public IComponentManager GetComponentManager() {
            return currentState.GetComponentManager();
        }

        public IEntityManager GetEntityManager() {
            return currentState.GetEntityManager();
        }

        public EntityOrderContainer EntityOrder {
            get { return currentState.EntityOrder; }
        }

        public void BeginState() {
            currentState.BeginState();
        }

        public void EndState() {
            currentState.EndState();
        }

        public void Dispose() {
            Managers.Dispose();
        }
    }
}
