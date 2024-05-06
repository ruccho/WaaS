﻿using System.Collections.Generic;
using System.Linq;

namespace WaaS.Generators;

internal static class LinqPlus
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
    {
        return source.Where(element => element != null)!;
    }
}