using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace D3DLab.ECS.Common {
    public abstract class Disposable {
        /// <summary>
		/// SharpDX 1.3 requires explicit dispose of all its ComObject.
		/// This method makes it easy.
		/// (Remark: I attempted to hack a correct Dispose implementation but it crashed the app on first GC!)
		/// </summary>
		protected void DisposeSetter<T>(ref T field, T newValue)
            where T : IDisposable {
            if (field != null) {
                field.Dispose();
            }
            field = newValue;
        }
    }
    /// <summary>
	/// SharpDX 1.3 requires explicit dispose of all its ComObject.
	/// This method makes it easier.
	/// (Remark: I attempted to hack a correct Dispose implementation but it crashed the app on first GC!)
	/// </summary>
	public class DisposeGroup : IDisposable {
        private readonly List<IDisposable> list;

        public DisposeGroup() {
            this.list = new List<IDisposable>();
        }

        public void Add(params IDisposable[] objects) {
            list.AddRange(objects.Where(o => o != null));
        }

        public T Add<T>(T ob)
            where T : IDisposable {
            if (ob != null)
                list.Add(ob);
            return ob;
        }

        public void Dispose() {
            for (int i = list.Count - 1; i >= 0; i--) {
                var d = list[i];
                list.RemoveAt(i);
                d.Dispose();
            }
        }
    }
}
