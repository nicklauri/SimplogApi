using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Extensions
{
    public static class Extension
    {
        public static bool IsNullOrEmpty(this string obj)
        {
            return obj == null || obj.Length == 0;
        }

        public static bool IsNullOrEmpty(this IList list)
        {
            return list == null || list.Count == 0;
        }

        public static bool IsNullOrEmpty(this object obj)
        {
            return obj == null;
        }
    }
}
