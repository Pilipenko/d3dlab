using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace D3DLab.ECS.Ext {
    public class ReadLockSlim : IDisposable {
        readonly ReaderWriterLockSlim loker;
        public ReadLockSlim(ReaderWriterLockSlim loker) {
            this.loker = loker;
            loker.EnterReadLock();
        }
        public void Dispose() {
            loker.ExitReadLock();
        }
    }
    public class WriteLockSlim : IDisposable {
        readonly ReaderWriterLockSlim loker;
        public WriteLockSlim(ReaderWriterLockSlim loker) {
            this.loker = loker;
            loker.EnterWriteLock();
        }
        public void Dispose() {
            loker.ExitWriteLock();
        }
    }
    public class UpgradeableReadLockSlim : IDisposable {
        readonly ReaderWriterLockSlim loker;
        public UpgradeableReadLockSlim(ReaderWriterLockSlim loker) {
            this.loker = loker;
            loker.EnterUpgradeableReadLock();
        }
        public void Dispose() {
            loker.ExitUpgradeableReadLock();
        }
    }
}
