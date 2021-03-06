﻿using System;
using System.Diagnostics;
using System.Threading;

namespace Theraot.Threading
{
    public sealed partial class Disposable : IDisposable
    {
        private int _disposeStatus;

        [DebuggerNonUserCode]
        ~Disposable()
        {
            try
            {
                // Empty
            }
            finally
            {
                try
                {
                    Dispose(false);
                }
                catch (Exception exception)
                {
                    // Catch them all - fields may be partially collected.
                    No.Op(exception);
                }
            }
        }

        public bool IsDisposed
        {
            [DebuggerNonUserCode]
            get => _disposeStatus == -1;
        }

        [DebuggerNonUserCode]
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

        [DebuggerNonUserCode]
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

        [DebuggerNonUserCode]
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

        [DebuggerNonUserCode]
        private void Dispose(bool disposeManagedResources)
        {
            No.Op(disposeManagedResources);
            if (TakeDisposalExecution())
            {
                try
                {
                    _release.Invoke();
                }
                finally
                {
                    _release = null;
                }
            }
        }

        private bool TakeDisposalExecution()
        {
            return _disposeStatus != -1 && ThreadingHelper.SpinWaitSetUnless(ref _disposeStatus, -1, 0, -1);
        }
    }
}