using System;
using System.Collections.Generic;
using System.Text;

namespace D3DLab.Std.Engine.Core.Ext {
    public class NullVerificationLock<T> where T : class {
        readonly object loker;

        public NullVerificationLock() {
            this.loker = new object();
        }
        public T Create(ref T obj, Func<T> create) {
            if (obj == null) {
                lock (loker) {
                    if (obj == null) {
                        obj = create();
                    }
                }
            }
            return obj;
        }
        public void Destroy(ref T obj, Func<T> destroy) {
            if (obj != null) {
                lock (loker) {
                    if (obj != null) {
                        obj = destroy();
                    }
                }
            }
        }
    }
}
