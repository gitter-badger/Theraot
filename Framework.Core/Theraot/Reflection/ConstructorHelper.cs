﻿// Needed for NET40

using System;
using System.Reflection;
using Theraot.Collections.ThreadSafe;

namespace Theraot.Reflection
{
    public static class ConstructorHelper
    {
        private static readonly CacheDict<Type, object> _constructorCache = new CacheDict<Type, object>(256);

        public static TReturn Create<TReturn>()
        {
            if (TryGetCreate<TReturn>(out var result))
            {
                return result();
            }
            throw new MissingMemberException($"There is no constructor for {typeof(TReturn)} with no type arguments.");
        }

        public static TReturn CreateOrDefault<TReturn>()
        {
            if (TryGetCreate<TReturn>(out var result))
            {
                return result();
            }
            return default;
        }

        public static bool TryGetCreate<TReturn>(out Func<TReturn> create)
        {
            var type = typeof(TReturn);
            var info = type.GetTypeInfo();
            if (info.IsValueType)
            {
                create = () => default;
                return true;
            }
            if (_constructorCache.TryGetValue(type, out var result))
            {
                if (result == null)
                {
                    create = null;
                    return false;
                }
                create = (Func<TReturn>)result;
                return true;
            }
            var typeArguments = ArrayReservoir<Type>.EmptyArray;
            var constructorInfo = typeof(TReturn).GetTypeInfo().GetConstructor(typeArguments);
            if (constructorInfo == null)
            {
                _constructorCache[type] = null;
                create = null;
                return false;
            }
            TReturn Create() => (TReturn)constructorInfo.Invoke(ArrayReservoir<object>.EmptyArray);
            _constructorCache[type] = (Func<TReturn>)Create;
            create = Create;
            return true;
        }
    }
}