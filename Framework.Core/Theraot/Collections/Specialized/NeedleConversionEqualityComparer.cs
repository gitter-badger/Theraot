// Needed for NET35 (ConditionalWeakTable)

using System.Collections.Generic;
using System.Diagnostics;
using Theraot.Threading.Needles;

namespace Theraot.Collections.Specialized
{
    [DebuggerNonUserCode]
    public sealed class NeedleConversionEqualityComparer<TNeedle, T> : ConversionEqualityComparer<TNeedle, T>
        where TNeedle : INeedle<T>
    {
        public NeedleConversionEqualityComparer(IEqualityComparer<T> comparer)
            : base(comparer, Conversion)
        {
            // Empty
        }

        private static T Conversion(TNeedle needle)
        {
            if (needle == null)
            {
                return default;
            }
            if (needle is ICacheNeedle<T> cacheNeedle && cacheNeedle.TryGetValue(out var value))
            {
                return value;
            }
            return needle.Value;
        }
    }
}