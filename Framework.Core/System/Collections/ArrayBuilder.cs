﻿#if LESSTHAN_NET45

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Theraot.Collections.ThreadSafe;

namespace System.Collections.Generic
{
    /// <summary>
    ///     Helper type for avoiding allocations while building arrays.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    internal struct ArrayBuilder<T>
    {
        private const int _defaultCapacity = 4;
        private const int _maxCoreClrArrayLength = 0x7fefffff; // For byte arrays the limit is slightly larger

        /// <summary>
        ///     Initializes the <see cref="ArrayBuilder{T}" /> with a specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity of the array to allocate.</param>
        public ArrayBuilder(int capacity) : this()
        {
            Debug.Assert(capacity >= 0);
            if (capacity > 0)
            {
                Buffer = new T[capacity];
            }
        }

        /// <summary>Gets the current underlying array.</summary>
        public T[] Buffer { get; private set; }

        /// <summary>
        ///     Gets the number of items this instance can store without re-allocating,
        ///     or 0 if the backing array is <c>null</c>.
        /// </summary>
        public int Capacity => Buffer?.Length ?? 0;

        /// <summary>
        ///     Gets the number of items in the array currently in use.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        ///     Gets or sets the item at a certain index in the array.
        /// </summary>
        /// <param name="index">The index into the array.</param>
        public T this[int index]
        {
            get
            {
                Debug.Assert(index >= 0 && index < Count);
                return Buffer[index];
            }
        }

        /// <summary>
        ///     Adds an item to the backing array, resizing it if necessary.
        /// </summary>
        /// <param name="item">The item to add.</param>
        public void Add(T item)
        {
            if (Count == Capacity)
            {
                EnsureCapacity(Count + 1);
            }

            UncheckedAdd(item);
        }

        /// <summary>
        ///     Gets the first item in this builder.
        /// </summary>
        public T First()
        {
            Debug.Assert(Count > 0);
            return Buffer[0];
        }

        /// <summary>
        ///     Gets the last item in this builder.
        /// </summary>
        public T Last()
        {
            Debug.Assert(Count > 0);
            return Buffer[Count - 1];
        }

        /// <summary>
        ///     Creates an array from the contents of this builder.
        /// </summary>
        /// <remarks>
        ///     Do not call this method twice on the same builder.
        /// </remarks>
        public T[] ToArray()
        {
            if (Count == 0)
            {
                return ArrayReservoir<T>.EmptyArray;
            }

            Debug.Assert(Buffer != null); // Nonzero _count should imply this

            var result = Buffer;
            if (Count < result.Length)
            {
                // Avoid a bit of overhead (method call, some branches, extra code-gen)
                // which would be incurred by using Array.Resize
                result = new T[Count];
                Array.Copy(Buffer, 0, result, 0, Count);
            }

#if DEBUG
            // Try to prevent callers from using the ArrayBuilder after ToArray, if _count != 0.
            Count = -1;
            Buffer = null;
#endif

            return result;
        }

        /// <summary>
        ///     Adds an item to the backing array, without checking if there is room.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <remarks>
        ///     Use this method if you know there is enough space in the <see cref="ArrayBuilder{T}" />
        ///     for another item, and you are writing performance-sensitive code.
        /// </remarks>
        public void UncheckedAdd(T item)
        {
            Debug.Assert(Count < Capacity);

            Buffer[Count++] = item;
        }

        private void EnsureCapacity(int minimum)
        {
            Debug.Assert(minimum > Capacity);

            var capacity = Capacity;
            var nextCapacity = capacity == 0 ? _defaultCapacity : 2 * capacity;

            if ((uint)nextCapacity > _maxCoreClrArrayLength)
            {
                nextCapacity = Math.Max(capacity + 1, _maxCoreClrArrayLength);
            }

            nextCapacity = Math.Max(nextCapacity, minimum);

            var next = new T[nextCapacity];
            if (Count > 0)
            {
                Array.Copy(Buffer, 0, next, 0, Count);
            }

            Buffer = next;
        }
    }
}

#endif