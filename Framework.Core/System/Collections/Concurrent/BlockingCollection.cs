﻿#if LESSTHAN_NET40 || NETSTANDARD1_0

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;
using Theraot.Collections.ThreadSafe;
using Theraot.Threading;

namespace System.Collections.Concurrent
{
    [ComVisible(false)]
    [DebuggerDisplay("Count = {Count}")]
    [HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
    public class BlockingCollection<T> : ICollection, IDisposable, IReadOnlyCollection<T>
    {
        private PrivateData _data;

        public BlockingCollection()
        {
            _data = new PrivateData(new SafeQueue<T>(), int.MaxValue);
        }

        public BlockingCollection(int boundedCapacity)
        {
            if (boundedCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(boundedCapacity));
            }

            _data = new PrivateData(new FixedSizeQueue<T>(boundedCapacity), boundedCapacity);
        }

        public BlockingCollection(IProducerConsumerCollection<T> collection, int boundedCapacity)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (boundedCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(boundedCapacity));
            }

            if (boundedCapacity < collection.Count)
            {
                throw new ArgumentException("The collection argument contains more items than are allowed by the boundedCapacity.");
            }

            _data = new PrivateData(collection, boundedCapacity);
        }

        public BlockingCollection(IProducerConsumerCollection<T> collection)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            _data = new PrivateData(collection, int.MaxValue);
        }

        public int BoundedCapacity => Data.Capacity;

        public bool IsAddingCompleted => !Data.CanAdd;

        public bool IsCompleted
        {
            get
            {
                var data = Data;
                return !data.CanAdd && data.Count == 0;
            }
        }

        private PrivateData Data
        {
            get
            {
                var data = Volatile.Read(ref _data);
                if (data == null)
                {
                    throw new ObjectDisposedException(nameof(BlockingCollection<T>));
                }

                return data;
            }
        }

        public int Count => Data.Count;

        bool ICollection.IsSynchronized
        {
            get
            {
                GC.KeepAlive(Data);
                return false;
            }
        }

        object ICollection.SyncRoot => throw new NotSupportedException();

        void ICollection.CopyTo(Array array, int index)
        {
            Data.CopyTo(array, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        public static int AddToAny(BlockingCollection<T>[] collections, T item)
        {
            return AddToAny(collections, item, CancellationToken.None);
        }

        public static int AddToAny(BlockingCollection<T>[] collections, T item, CancellationToken cancellationToken)
        {
            if (collections == null)
            {
                throw new ArgumentNullException(nameof(collections));
            }

            if (collections.Length == 0)
            {
                throw new ArgumentException("The collections argument is a 0-length array", nameof(collections));
            }

            var waitHandles = new WaitHandle[collections.Length + 1];
            for (var index = 0; index < collections.Length; index++)
            {
                var collection = collections[index];
                if (collection == null)
                {
                    throw new ArgumentException("The collections argument contains a null element", nameof(collections));
                }

                waitHandles[index] = collection.Data.WaitHandle;
                if (collection.IsAddingCompleted)
                {
                    throw new ArgumentException("At least one of collections has been marked as complete for adding.", nameof(collections));
                }

                if (collection.TryAdd(item, 0, cancellationToken))
                {
                    return index;
                }
            }

            waitHandles[collections.Length] = cancellationToken.WaitHandle;
            while (true)
            {
                WaitHandle.WaitAny(waitHandles);
                cancellationToken.ThrowIfCancellationRequested();
                for (var index = 0; index < collections.Length; index++)
                {
                    var collection = collections[index];
                    if (collection.IsAddingCompleted)
                    {
                        throw new ArgumentException("At least one of collections has been marked as complete for adding.", nameof(collections));
                    }

                    if (collection.TryAdd(item, 0, cancellationToken))
                    {
                        return index;
                    }
                }
            }
        }

        public static int TakeFromAny(BlockingCollection<T>[] collections, out T item)
        {
            return TakeFromAny(collections, out item, CancellationToken.None);
        }

        public static int TakeFromAny(BlockingCollection<T>[] collections, out T item, CancellationToken cancellationToken)
        {
            if (collections == null)
            {
                throw new ArgumentNullException(nameof(collections));
            }

            if (collections.Length == 0)
            {
                throw new ArgumentException("The collections argument is a 0-length array", nameof(collections));
            }

            var waitHandles = new WaitHandle[collections.Length + 1];
            for (var index = 0; index < collections.Length; index++)
            {
                var collection = collections[index];
                if (collection == null)
                {
                    throw new ArgumentException("The collections argument contains a null element", nameof(collections));
                }

                waitHandles[index] = collection.Data.WaitHandle;
                if (collection.IsAddingCompleted)
                {
                    throw new ArgumentException("At least one of collections has been marked as complete for adding.", nameof(collections));
                }

                if (collection.TryTake(out item, 0, cancellationToken))
                {
                    return index;
                }
            }

            waitHandles[collections.Length] = cancellationToken.WaitHandle;
            while (true)
            {
                WaitHandle.WaitAny(waitHandles);
                cancellationToken.ThrowIfCancellationRequested();
                for (var index = 0; index < collections.Length; index++)
                {
                    var collection = collections[index];
                    if (collection.IsAddingCompleted)
                    {
                        throw new ArgumentException("At least one of collections has been marked as complete for adding.", nameof(collections));
                    }

                    if (collection.TryTake(out item, 0, cancellationToken))
                    {
                        return index;
                    }
                }
            }
        }

        public static int TryAddToAny(BlockingCollection<T>[] collections, T item)
        {
            return TryAddToAny(collections, item, 0, CancellationToken.None);
        }

        public static int TryAddToAny(BlockingCollection<T>[] collections, T item, TimeSpan timeout)
        {
            return TryAddToAny(collections, item, (int)timeout.TotalMilliseconds, CancellationToken.None);
        }

        public static int TryAddToAny(BlockingCollection<T>[] collections, T item, int millisecondsTimeout)
        {
            return TryAddToAny(collections, item, millisecondsTimeout, CancellationToken.None);
        }

        public static int TryAddToAny(BlockingCollection<T>[] collections, T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (collections == null)
            {
                throw new ArgumentNullException(nameof(collections));
            }

            if (collections.Length == 0)
            {
                throw new ArgumentException("The collections argument is a 0-length array", nameof(collections));
            }

            var waitHandles = new WaitHandle[collections.Length + 1];
            for (var index = 0; index < collections.Length; index++)
            {
                var collection = collections[index];
                if (collection == null)
                {
                    throw new ArgumentException("The collections argument contains a null element", nameof(collections));
                }

                waitHandles[index] = collection.Data.WaitHandle;
                if (collection.IsAddingCompleted)
                {
                    throw new ArgumentException("At least one of collections has been marked as complete for adding.", nameof(collections));
                }

                if (collection.TryAdd(item, 0, cancellationToken))
                {
                    return index;
                }
            }

            waitHandles[collections.Length] = cancellationToken.WaitHandle;
            if (millisecondsTimeout == -1)
            {
                while (true)
                {
                    WaitHandle.WaitAny(waitHandles);
                    cancellationToken.ThrowIfCancellationRequested();
                    for (var index = 0; index < collections.Length; index++)
                    {
                        var collection = collections[index];
                        if (collection.IsAddingCompleted)
                        {
                            throw new ArgumentException("At least one of collections has been marked as complete for adding.", nameof(collections));
                        }

                        if (collection.TryAdd(item, 0, cancellationToken))
                        {
                            return index;
                        }
                    }
                }
            }

            var start = ThreadingHelper.TicksNow();
            var remaining = millisecondsTimeout;
            while (true)
            {
                WaitHandle.WaitAny(waitHandles, remaining);
                cancellationToken.ThrowIfCancellationRequested();
                for (var index = 0; index < collections.Length; index++)
                {
                    var collection = collections[index];
                    if (collection.IsAddingCompleted)
                    {
                        throw new ArgumentException("At least one of collections has been marked as complete for adding.", nameof(collections));
                    }

                    if (collection.TryAdd(item, 0, cancellationToken))
                    {
                        return index;
                    }

                    remaining = (int)(millisecondsTimeout - ThreadingHelper.Milliseconds(ThreadingHelper.TicksNow() - start));
                    if (remaining <= 0)
                    {
                        return -1;
                    }
                }
            }
        }

        public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T item)
        {
            return TryTakeFromAny(collections, out item, 0, CancellationToken.None);
        }

        public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T item, TimeSpan timeout)
        {
            return TryTakeFromAny(collections, out item, (int)timeout.TotalMilliseconds, CancellationToken.None);
        }

        public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T item, int millisecondsTimeout)
        {
            return TryTakeFromAny(collections, out item, millisecondsTimeout, CancellationToken.None);
        }

        public static int TryTakeFromAny(BlockingCollection<T>[] collections, out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            if (collections == null)
            {
                throw new ArgumentNullException(nameof(collections));
            }

            if (collections.Length == 0)
            {
                throw new ArgumentException("The collections argument is a 0-length array", nameof(collections));
            }

            var waitHandles = new WaitHandle[collections.Length + 1];
            for (var index = 0; index < collections.Length; index++)
            {
                var collection = collections[index];
                if (collection == null)
                {
                    throw new ArgumentException("The collections argument contains a null element", nameof(collections));
                }

                waitHandles[index] = collection.Data.WaitHandle;
                if (collection.IsAddingCompleted)
                {
                    throw new ArgumentException("At least one of collections has been marked as complete for adding.", nameof(collections));
                }

                if (collection.TryTake(out item, 0, cancellationToken))
                {
                    return index;
                }
            }

            waitHandles[collections.Length] = cancellationToken.WaitHandle;
            if (millisecondsTimeout == -1)
            {
                while (true)
                {
                    WaitHandle.WaitAny(waitHandles);
                    cancellationToken.ThrowIfCancellationRequested();
                    for (var index = 0; index < collections.Length; index++)
                    {
                        var collection = collections[index];
                        if (collection.IsAddingCompleted)
                        {
                            throw new ArgumentException("At least one of collections has been marked as complete for adding.", nameof(collections));
                        }

                        if (collection.TryTake(out item, 0, cancellationToken))
                        {
                            return index;
                        }
                    }
                }
            }

            var start = ThreadingHelper.TicksNow();
            var remaining = millisecondsTimeout;
            while (true)
            {
                WaitHandle.WaitAny(waitHandles, remaining);
                cancellationToken.ThrowIfCancellationRequested();
                for (var index = 0; index < collections.Length; index++)
                {
                    var collection = collections[index];
                    if (collection.IsAddingCompleted)
                    {
                        throw new ArgumentException("At least one of collections has been marked as complete for adding.", nameof(collections));
                    }

                    if (collection.TryTake(out item, 0, cancellationToken))
                    {
                        return index;
                    }

                    remaining = (int)(millisecondsTimeout - ThreadingHelper.Milliseconds(ThreadingHelper.TicksNow() - start));
                    if (remaining <= 0)
                    {
                        return -1;
                    }
                }
            }
        }

        public void Add(T item)
        {
            Data.TryAdd(item, Timeout.Infinite, CancellationToken.None);
        }

        public void Add(T item, CancellationToken cancellationToken)
        {
            Data.TryAdd(item, Timeout.Infinite, cancellationToken);
        }

        public void CompleteAdding()
        {
            Data.CanAdd = false;
        }

        public void CopyTo(T[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        public IEnumerable<T> GetConsumingEnumerable()
        {
            while (TryTake(out var item, -1, CancellationToken.None))
            {
                yield return item;
            }
        }

        public IEnumerable<T> GetConsumingEnumerable(CancellationToken cancellationToken)
        {
            while (!IsCompleted)
            {
                if (TryTake(out var item, Timeout.Infinite, cancellationToken))
                {
                    yield return item;
                }
            }
        }

        public T Take()
        {
            Data.TryTake(out var item, Timeout.Infinite, CancellationToken.None);
            return item;
        }

        public T Take(CancellationToken cancellationToken)
        {
            Data.TryTake(out var item, Timeout.Infinite, cancellationToken);
            return item;
        }

        public T[] ToArray()
        {
            return Data.ToArray();
        }

        public bool TryAdd(T item)
        {
            return Data.TryAdd(item, Timeout.Infinite, CancellationToken.None);
        }

        public bool TryAdd(T item, TimeSpan timeout)
        {
            return Data.TryAdd(item, (int)timeout.TotalMilliseconds, CancellationToken.None);
        }

        public bool TryAdd(T item, int millisecondsTimeout)
        {
            return Data.TryAdd(item, millisecondsTimeout, CancellationToken.None);
        }

        public bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Data.TryAdd(item, millisecondsTimeout, cancellationToken);
        }

        public bool TryTake(out T item)
        {
            return Data.TryTake(out item);
        }

        public bool TryTake(out T item, TimeSpan timeout)
        {
            return Data.TryTake(out item, (int)timeout.TotalMilliseconds, CancellationToken.None);
        }

        public bool TryTake(out T item, int millisecondsTimeout)
        {
            return Data.TryTake(out item, millisecondsTimeout, CancellationToken.None);
        }

        public bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            return Data.TryTake(out item, millisecondsTimeout, cancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Interlocked.Exchange(ref _data, null)?.Dispose();
            }
        }

        private sealed class PrivateData : IDisposable, IReadOnlyCollection<T>
        {
            private readonly SemaphoreSlim _addSemaphore;
            private readonly IProducerConsumerCollection<T> _collection;
            private readonly SemaphoreSlim _takeSemaphore;
            public readonly int Capacity;
            private int _addWaiters;
            private int _canAdd;

            public PrivateData(IProducerConsumerCollection<T> collection, int capacity)
            {
                _collection = collection;
                Capacity = capacity;
                _addSemaphore = new SemaphoreSlim(capacity, capacity);
                _takeSemaphore = new SemaphoreSlim(0, capacity);
            }

            public bool CanAdd
            {
                get => Volatile.Read(ref _canAdd) == 0;
                set => Volatile.Write(ref _canAdd, value ? 1 : 0);
            }

            public WaitHandle WaitHandle => _addSemaphore.AvailableWaitHandle;

            public void Dispose()
            {
                _addSemaphore?.Dispose();
                _takeSemaphore?.Dispose();
            }

            public int Count => _collection.Count;

            public IEnumerator<T> GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public void CopyTo(Array array, int index)
            {
                _collection.CopyTo(array, index);
            }

            public T[] ToArray()
            {
                return _collection.ToArray();
            }

            public bool TryAdd(T item, int millisecondsTimeout, CancellationToken cancellationToken)
            {
                if (!CanAdd)
                {
                    throw new InvalidOperationException("The BlockingCollection<T> has been marked as complete with regards to additions.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                Interlocked.Increment(ref _addWaiters);
                try
                {
                    if (!_addSemaphore.Wait(millisecondsTimeout, cancellationToken))
                    {
                        return false;
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                    if (!_collection.TryAdd(item))
                    {
                        throw new InvalidOperationException("The underlying collection didn't accept the item.");
                    }

                    _takeSemaphore.Release();
                    return true;
                }
                finally
                {
                    Interlocked.Decrement(ref _addWaiters);
                }
            }

            public bool TryTake(out T item)
            {
                item = default;
                if (_collection.Count == 0)
                {
                    return false;
                }

                return TryTake(out item, -1, CancellationToken.None);
            }

            public bool TryTake(out T item, int millisecondsTimeout, CancellationToken cancellationToken)
            {
                item = default;
                cancellationToken.ThrowIfCancellationRequested();
                if (!_takeSemaphore.Wait(millisecondsTimeout, cancellationToken))
                {
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!_collection.TryTake(out item))
                {
                    throw new InvalidOperationException("The underlying collection was modified outside this BlockingCollection<T> instance.");
                }

                _addSemaphore.Release();
                return true;
            }
        }
    }
}

#endif