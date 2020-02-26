using System.Collections.Generic;
using System.Linq;

namespace D3DLab.ECS {
    public interface IEngineSubscriber { }
    public interface IManagerChangeSubscriber<in T> : IEngineSubscriber{
        void Change(T visual);
    }

    public interface IEntityRenderSubscriber : IEngineSubscriber {
        void Render(IEnumerable<GraphicEntity> entities);
    }

    public interface IEngineSubscribe {
        void Subscribe(IEngineSubscriber s);
    }

    public interface IManagerChangeNotify {
        void NotifyChange<T>(T _object);// where T : class
    }
    public interface IEntityRenderNotify {
        void NotifyRender(IEnumerable<GraphicEntity> entities);
    }

    public sealed class EngineNotificator : IEngineSubscribe, IManagerChangeNotify , IEntityRenderNotify {
        private readonly List<IEngineSubscriber> subscribers;
        public EngineNotificator() {
            this.subscribers = new List<IEngineSubscriber>();
        }

        public void Subscribe(IEngineSubscriber s) {
            subscribers.Add(s);
        }

        public void NotifyChange<T>(T _object) {// where T : class
            var handlers = subscribers.OfType<IManagerChangeSubscriber<T>>();
            foreach (var handler in handlers) {
                handler.Change(_object);
            }
        }
        public void NotifyRender(IEnumerable<GraphicEntity> entities) {
            var handlers = subscribers.OfType<IEntityRenderSubscriber>();
            foreach (var handler in handlers) {
                handler.Render(entities);
            }
        }
    }
}