using System;
using System.Collections.Generic;

namespace D3DLab.ECS {
    public sealed class SystemManager : ISystemManager {
        readonly List<IGraphicSystem> systems = new List<IGraphicSystem>();
        public TSystem CreateSystem<TSystem>() where TSystem : class, IGraphicSystem {
            var sys = Activator.CreateInstance<TSystem>();
            
            if(sys is IComponentSystemIncrementId incrementId) {
                incrementId.ID = systems.Count;
            }            

            if(sys is IGraphicSystemContextDependent dependent) {
                dependent.ContextState = context;
            }

            systems.Add(sys);            
            notify.NotifyChange(sys);
            return sys;
        }
        public IEnumerable<IGraphicSystem> GetSystems() {
            return systems;
        }

        public void Dispose() {
            systems.Clear();
        }

        readonly IManagerChangeNotify notify;
        readonly IContextState context;

        public SystemManager(IManagerChangeNotify notify, IContextState context) {
            this.notify = notify;
            this.context = context;
        }
    }
}
