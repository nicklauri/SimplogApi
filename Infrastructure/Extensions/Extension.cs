using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Domain;

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

        public static bool IsNull(this User user)
        {
            return user == null;
        }

        public static bool IsNull(this Employee employee)
        {
            return employee == null;
        }
    }
}
