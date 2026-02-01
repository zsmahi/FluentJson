using System;
using System.Collections.Generic;

namespace FluentJson.Internal;

internal static class TypeSafety
{
    private static readonly HashSet<Type> _unsupportedTypes =
    [
        typeof(IntPtr),
        typeof(UIntPtr),
        typeof(void)
    ];

    public static bool IsUnsupported(Type type)
        => _unsupportedTypes.Contains(type) || type.IsPointer;
}
