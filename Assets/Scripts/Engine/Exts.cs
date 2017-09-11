using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Engine
{
    public static class Exts
    {
        public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k)
            where V : new()
        {
            return d.GetOrAdd(k, _ => new V());
        }

        public static V GetOrAdd<K, V>(this Dictionary<K, V> d, K k, Func<K, V> createFn)
        {
            V ret;
            if (!d.TryGetValue(k, out ret))
                d.Add(k, ret = createFn(k));
            return ret;
        }

        public static bool HasAttribute<T>(this MemberInfo mi)
        {
            return mi.GetCustomAttributes(typeof(T), true).Length != 0;
        }
    }
}
