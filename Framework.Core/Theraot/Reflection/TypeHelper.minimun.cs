﻿// Needed for NET40

using System;
using System.Reflection;
using System.Threading;

namespace Theraot.Reflection
{
    public static partial class TypeHelper
    {
        public static TTarget As<TTarget>(object source)
            where TTarget : class
        {
            return As
            (
                source,
                new Func<TTarget>
                (
                    () => throw new InvalidOperationException("Cannot convert to " + typeof(TTarget).Name))
            );
        }

        public static TTarget As<TTarget>(object source, TTarget def)
            where TTarget : class
        {
            return As(source, () => def);
        }

        public static TTarget As<TTarget>(object source, Func<TTarget> alternative)
            where TTarget : class
        {
            if (alternative == null)
            {
                throw new ArgumentNullException(nameof(alternative));
            }
            if (!(source is TTarget sourceAsTarget))
            {
                return alternative();
            }
            return sourceAsTarget;
        }

        public static TTarget Cast<TTarget>(object source)
        {
            return Cast
            (
                source,
                new Func<TTarget>
                (
                    () => throw new InvalidOperationException("Cannot convert to " + typeof(TTarget).Name))
            );
        }

        public static TTarget Cast<TTarget>(object source, TTarget def)
        {
            return Cast(source, () => def);
        }

        public static TTarget Cast<TTarget>(object source, Func<TTarget> alternative)
        {
            if (alternative == null)
            {
                throw new ArgumentNullException(nameof(alternative));
            }
            try
            {
                var sourceAsTarget = (TTarget)source;
                return sourceAsTarget;
            }
            catch (Exception exception)
            {
                No.Op(exception);
                return alternative();
            }
        }

        public static MethodInfo FindConversionOperator(MethodInfo[] methods, Type typeFrom, Type typeTo, bool implicitOnly)
        {
            foreach (var method in methods)
            {
                if (!string.Equals(method.Name, "op_Implicit", StringComparison.Ordinal) && (implicitOnly || !string.Equals(method.Name, "op_Explicit", StringComparison.Ordinal)))
                {
                    continue;
                }
                if (method.ReturnType != typeTo)
                {
                    continue;
                }
                var parameters = method.GetParameters();
                if (parameters[0].ParameterType != typeFrom)
                {
                    continue;
                }
                return method;
            }
            return null;
        }

        public static bool IsImplicitBoxingConversion(Type source, Type target)
        {
            var info = source.GetTypeInfo();
            if (info.IsValueType && (target == typeof(object) || target == typeof(ValueType)))
            {
                return true;
            }
            if (info.IsEnum && target == typeof(Enum))
            {
                return true;
            }
            return false;
        }

        public static bool IsImplicitNumericConversion(Type source, Type target)
        {
            if (source == typeof(sbyte))
            {
                if (
                    target == typeof(short)
                    || target == typeof(int)
                    || target == typeof(long)
                    || target == typeof(float)
                    || target == typeof(double)
                    || target == typeof(decimal)
                    )
                {
                    return true;
                }
            }
            else if (source == typeof(byte))
            {
                if (
                    target == typeof(short)
                    || target == typeof(ushort)
                    || target == typeof(int)
                    || target == typeof(uint)
                    || target == typeof(long)
                    || target == typeof(ulong)
                    || target == typeof(float)
                    || target == typeof(double)
                    || target == typeof(decimal)
                    )
                {
                    return true;
                }
            }
            else if (source == typeof(short))
            {
                if (
                    target == typeof(int)
                    || target == typeof(long)
                    || target == typeof(float)
                    || target == typeof(double)
                    || target == typeof(decimal)
                    )
                {
                    return true;
                }
            }
            else if (source == typeof(ushort))
            {
                if (
                    target == typeof(int)
                    || target == typeof(uint)
                    || target == typeof(long)
                    || target == typeof(ulong)
                    || target == typeof(float)
                    || target == typeof(double)
                    || target == typeof(decimal)
                    )
                {
                    return true;
                }
            }
            else if (source == typeof(int))
            {
                if (
                    target == typeof(long)
                    || target == typeof(float)
                    || target == typeof(double)
                    || target == typeof(decimal)
                    )
                {
                    return true;
                }
            }
            else if (source == typeof(uint))
            {
                if (
                    target == typeof(ulong)
                    || target == typeof(float)
                    || target == typeof(double)
                    || target == typeof(decimal)
                    )
                {
                    return true;
                }
            }
            else if (source == typeof(long) || target == typeof(ulong))
            {
                if (
                    target == typeof(float)
                    || target == typeof(double)
                    || target == typeof(decimal)
                    )
                {
                    return true;
                }
            }
            else if (source == typeof(char))
            {
                if (
                    target == typeof(ushort)
                    || target == typeof(int)
                    || target == typeof(uint)
                    || target == typeof(long)
                    || target == typeof(ulong)
                    || target == typeof(float)
                    || target == typeof(double)
                    || target == typeof(decimal)
                    )
                {
                    return true;
                }
            }
            else if (source == typeof(float))
            {
                return target == typeof(double);
            }
            return false;
        }

        public static T LazyCreate<T>(ref T target)
            where T : class
        {
            var found = target;
            if (found != null)
            {
                return found;
            }
            found = Volatile.Read(ref target);
            if (found != null)
            {
                return found;
            }
            T created;
            try
            {
                created = Activator.CreateInstance<T>();
            }
            catch
            {
                throw new MissingMemberException("The type being lazily initialized does not have a public, parameterless constructor.");
            }
            found = Interlocked.CompareExchange(ref target, created, null);
            return found ?? created;
        }

        public static T LazyCreate<T>(ref T target, object syncRoot)
            where T : class
        {
            var found = target;
            if (found != null)
            {
                return found;
            }
            found = Volatile.Read(ref target);
            if (found != null)
            {
                return found;
            }
            lock (syncRoot)
            {
                return LazyCreate(ref target);
            }
        }

        public static T LazyCreate<T>(ref T target, Func<T> valueFactory)
            where T : class
        {
            var found = target;
            if (found != null)
            {
                return found;
            }
            found = Volatile.Read(ref target);
            if (found != null)
            {
                return found;
            }
            if (valueFactory == null)
            {
                throw new ArgumentNullException(nameof(valueFactory));
            }
            var created = valueFactory();
            if (created == null)
            {
                throw new InvalidOperationException("valueFactory returned null");
            }
            found = Interlocked.CompareExchange(ref target, created, null);
            return found ?? created;
        }

        public static T LazyCreate<T>(ref T target, Func<T> valueFactory, object syncRoot)
            where T : class
        {
            var found = target;
            if (found != null)
            {
                return found;
            }
            found = Volatile.Read(ref target);
            if (found != null)
            {
                return found;
            }
            lock (syncRoot)
            {
                return LazyCreate(ref target, valueFactory);
            }
        }
    }
}