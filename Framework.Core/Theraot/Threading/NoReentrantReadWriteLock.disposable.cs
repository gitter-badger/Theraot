#if FAT

using System;
using System.Threading;

namespace Theraot.Threading
{
    internal sealed partial class NoReentrantReadWriteLock : IExtendedDisposable
    {
        private int _disposeStatus;

        [System.Diagnostics.DebuggerNonUserCode]
        ~NoReentrantReadWriteLock()
        {
            try
            {
                // Empty
            }
            finally
            {
                Dispose(false);
            }
        }

        public bool IsDisposed => _disposeStatus == -1;

        [System.Diagnostics.DebuggerNonUserCode]
        public void Dispose()
        {
            try
            {
                Dispose(true);
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }

        [System.Diagnostics.DebuggerNonUserCode]
        public void DisposedConditional(Action whenDisposed, Action whenNotDisposed)
        {
            if (_disposeStatus == -1)
            {
                whenDisposed?.Invoke();
            }
            else
            {
                if (whenNotDisposed != null)
                {
                    if (ThreadingHelper.SpinWaitRelativeSet(ref _disposeStatus, 1, -1))
                    {
                        try
                        {
                            whenNotDisposed.Invoke();
                        }
                        finally
                        {
                            Interlocked.Decrement(ref _disposeStatus);
                        }
                    }
                    else
                    {
                        whenDisposed?.Invoke();
                    }
                }
            }
        }

        [System.Diagnostics.DebuggerNonUserCode]
        public TReturn DisposedConditional<TReturn>(Func<TReturn> whenDisposed, Func<TReturn> whenNotDisposed)
        {
            if (_disposeStatus == -1)
            {
                if (whenDisposed == null)
                {
                    return default;
                }
                return whenDisposed.Invoke();
            }
            if (whenNotDisposed == null)
            {
                return default;
            }
            if (ThreadingHelper.SpinWaitRelativeSet(ref _disposeStatus, 1, -1))
            {
                try
                {
                    return whenNotDisposed.Invoke();
                }
                finally
                {
                    Interlocked.Decrement(ref _disposeStatus);
                }
            }
            if (whenDisposed == null)
            {
                return default;
            }
            return whenDisposed.Invoke();
        }

        [System.Diagnostics.DebuggerNonUserCode]
        private void Dispose(bool disposeManagedResources)
        {
            if (TakeDisposalExecution())
            {
                try
                {
                    if (disposeManagedResources)
                    {
                        _freeToRead.Dispose();
                        _freeToWrite.Dispose();
                    }
                }
                finally
                {
                    _freeToRead = null;
                    _freeToWrite = null;
                }
            }
        }

        private bool TakeDisposalExecution()
        {
            if (_disposeStatus == -1)
            {
                return false;
            }
            return ThreadingHelper.SpinWaitSetUnless(ref _disposeStatus, -1, 0, -1);
        }
    }
}

#endif