using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.ECS.Common { 

    public static class Disposer {
        public static void DisposeAll(params IDisposable[] source) {
            DisposeAll((IEnumerable<IDisposable>)source);
        }
        public static void DisposeAll(IEnumerable<IDisposable> source) {
            if (source == null) return;
            foreach (var d in source) {
                d?.Dispose();
            }
        }
    }
}
