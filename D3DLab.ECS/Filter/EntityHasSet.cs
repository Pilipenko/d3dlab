using D3DLab.ECS;
using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS.Filter {
    /// <summary>
    /// NOTE: not support interface types
    /// </summary>
    public class EntityHasSet {
        readonly Type[] types;
        public EntityHasSet(params Type[] types) {
            this.types = types;
        }

        public bool HasComponents(GraphicEntity entity) {
            return entity.Has(types);
        }
    }
}
