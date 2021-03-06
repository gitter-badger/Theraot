// Needed for Workaround

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Theraot.Collections.ThreadSafe;
using Theraot.Threading.Needles;

namespace Theraot.Threading
{
    /// <summary>
    ///     Represents a context to execute operation without reentry.
    /// </summary>
    [DebuggerNonUserCode]
    public sealed class ReentryGuard
    {
        [ThreadStatic]
        private static HashSet<UniqueId> _guard;

        private readonly SafeQueue<Action> _workQueue;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReentryGuard" /> class.
        /// </summary>
        public ReentryGuard()
        {
            _workQueue = new SafeQueue<Action>();
            Id = RuntimeUniqueIdProvider.GetNextId();
        }

        internal UniqueId Id { get; }

        /// <summary>
        ///     Gets a value indicating whether or not the current thread did enter.
        /// </summary>
        public bool IsTaken => _guard?.Contains(Id) == true;

        public IDisposable TryEnter(out bool didEnter)
        {
            didEnter = Enter(Id);
            return didEnter ? DisposableAkin.Create(() => Leave(Id)) : NoOpDisposable.Instance;
        }

        private static IPromise AddExecution(Action action, SafeQueue<Action> queue)
        {
            var promised = new Promise(false);
            var result = new ReadOnlyPromise(promised, false);
            queue.Add
            (
                () =>
                {
                    try
                    {
                        action.Invoke();
                        promised.SetCompleted();
                    }
                    catch (Exception exception)
                    {
                        promised.SetError(exception);
                    }
                }
            );
            return result;
        }

        private static IPromise<T> AddExecution<T>(Func<T> action, SafeQueue<Action> queue)
        {
            var promised = new PromiseNeedle<T>(false);
            var result = new ReadOnlyPromiseNeedle<T>(promised, false);
            queue.Add
            (
                () =>
                {
                    try
                    {
                        promised.Value = action.Invoke();
                    }
                    catch (Exception exception)
                    {
                        promised.SetError(exception);
                    }
                }
            );
            return result;
        }

        private static void ExecutePending(SafeQueue<Action> queue, UniqueId id)
        {
            var didEnter = false;
            try
            {
                didEnter = Enter(id);
                if (!didEnter)
                {
                    return;
                }

                while (queue.TryTake(out var action))
                {
                    action.Invoke();
                }
            }
            finally
            {
                if (didEnter)
                {
                    Leave(id);
                }
            }
        }

        /// <summary>
        ///     Executes an operation-
        /// </summary>
        /// <typeparam name="T">The return value of the operation.</typeparam>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>Returns a promise to finish the execution.</returns>
        public IPromise<T> Execute<T>(Func<T> operation)
        {
            var result = AddExecution(operation, _workQueue);
            ExecutePending(_workQueue, Id);
            return result;
        }

        /// <summary>
        ///     Executes an operation-
        /// </summary>
        /// <param name="operation">The operation to execute.</param>
        /// <returns>Returns a promise to finish the execution.</returns>
        public IPromise Execute(Action operation)
        {
            var result = AddExecution(operation, _workQueue);
            ExecutePending(_workQueue, Id);
            return result;
        }

        internal static bool Enter(UniqueId id)
        {
            // Assume anything could have been set to null, start no sync operation, this could be running during DomainUnload
            if (GCMonitor.FinalizingForUnload)
            {
                return false;
            }

            var guard = _guard;
            if (guard == null)
            {
                _guard = new HashSet<UniqueId> {id};
                return true;
            }

            if (!guard.Contains(id))
            {
                guard.Add(id);
                return true;
            }

            return false;
        }

        internal static void Leave(UniqueId id)
        {
            // Assume anything could have been set to null, start no sync operation, this could be running during DomainUnload
            var guard = _guard;
            guard?.Remove(id);
        }
    }
}
