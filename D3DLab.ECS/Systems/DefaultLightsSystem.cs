using D3DLab.ECS;
using D3DLab.ECS.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace D3DLab.ECS.Systems {  
    public class DefaultLightsSystem : BaseEntitySystem, IGraphicSystem, IGraphicSystemContextDependent {
        public IContextState ContextState { get; set; }

        protected override void Executing(ISceneSnapshot snapshot) {
            var emanager = ContextState.GetEntityManager();
            var camera = snapshot.Camera;

            try {
                foreach (var entity in emanager.GetEntities()) {
                    if (!entity.Has<LightComponent>()) {
                        continue;
                    }
                    var light = entity.GetComponent<LightComponent>();
                    var color = entity.GetComponent<ColorComponent>();

                    snapshot.UpdateLight(light.Index, new LightState {
                        Intensity = light.Intensity,
                        Position = light.Position,
                        Direction = light.Direction,
                        Color = color.Color,
                        Type = light.Type
                    });

                }
            } catch (Exception ex) {
                ex.ToString();
                throw ex;
            }
        }
    }
}
