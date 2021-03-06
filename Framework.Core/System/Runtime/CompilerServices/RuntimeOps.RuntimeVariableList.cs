﻿#if LESSTHAN_NET35

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions.Compiler;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.
    /// Contains helper methods called from dynamically generated methods.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never), DebuggerStepThrough]
    public static partial class RuntimeOps
    {
        /// <summary>
        /// Creates an interface that can be used to modify closed over variables at runtime.
        /// </summary>
        /// <param name="data">The closure array.</param>
        /// <param name="indexes">An array of indexes into the closure array where variables are found.</param>
        /// <returns>An interface to access variables.</returns>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static IRuntimeVariables CreateRuntimeVariables(object[] data, long[] indexes)
        {
            return new RuntimeVariableList(data, indexes);
        }

        /// <summary>
        /// Creates an interface that can be used to modify closed over variables at runtime.
        /// </summary>
        /// <returns>An interface to access variables.</returns>
        [Obsolete("do not use this method", true), EditorBrowsable(EditorBrowsableState.Never)]
        public static IRuntimeVariables CreateRuntimeVariables()
        {
            return new EmptyRuntimeVariables();
        }

        private sealed class EmptyRuntimeVariables : IRuntimeVariables
        {
            int IRuntimeVariables.Count => 0;

            object IRuntimeVariables.this[int index]
            {
#pragma warning disable CA2201 // Do not raise reserved exception types
                get => throw new IndexOutOfRangeException();
                set => throw new IndexOutOfRangeException();
#pragma warning restore CA2201 // Do not raise reserved exception types
            }
        }

        /// <summary>
        /// Provides a list of variables, supporting read/write of the values
        /// Exposed via RuntimeVariablesExpression
        /// </summary>
        private sealed class RuntimeVariableList : IRuntimeVariables
        {
            // The top level environment. It contains pointers to parent
            // environments, which are always in the first element
            private readonly object[] _data;

            // An array of (int, int) pairs, each representing how to find a
            // variable in the environment data structure.
            //
            // The first integer indicates the number of times to go up in the
            // closure chain, the second integer indicates the index into that
            // closure chain.
            private readonly long[] _indexes;

            internal RuntimeVariableList(object[] data, long[] indexes)
            {
                Debug.Assert(data != null);
                Debug.Assert(indexes != null);

                _data = data;
                _indexes = indexes;
            }

            public int Count => _indexes.Length;

            public object this[int index]
            {
                get => GetStrongBox(index).Value;
                set => GetStrongBox(index).Value = value;
            }

            private IStrongBox GetStrongBox(int index)
            {
                // We lookup the closure using two ints:
                // 1. The high dword is the number of parents to go up
                // 2. The low dword is the index into that array
                var closureKey = _indexes[index];

                // walk up the parent chain to find the real environment
                var result = _data;
                for (var parents = (int)(closureKey >> 32); parents > 0; parents--)
                {
                    result = HoistedLocals.GetParent(result);
                }

                // Return the variable storage
                return (IStrongBox)result[unchecked((int)closureKey)];
            }
        }
    }
}

#endif