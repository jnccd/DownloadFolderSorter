﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace DownloadFolderSorter
{
    public static class LinqExtensions
    {
        public static b Foldl<a, b>(this IEnumerable<a> xs, b y, Func<b, a, b> f)
        {
            foreach (a x in xs)
                y = f(y, x);
            return y;
        }
        public static b Foldl<a, b>(this IEnumerable<a> xs, Func<b, a, b> f)
        {
            return xs.Foldl(default, f);
        }
        public static a MaxElement<a>(this IEnumerable<a> xs, Func<a, double> f) { return xs.MaxElement(f, out double max); }
        public static a MaxElement<a>(this IEnumerable<a> xs, Func<a, double> f, out double max)
        {
            max = 0; a maxE = default;
            foreach (a x in xs)
            {
                double res = f(x);
                if (res > max)
                {
                    max = res;
                    maxE = x;
                }
            }
            return maxE;
        }
        public static a MinElement<a>(this IEnumerable<a> xs, Func<a, double> f) { return xs.MinElement(f, out double min); }
        public static a MinElement<a>(this IEnumerable<a> xs, Func<a, double> f, out double min)
        {
            min = 0; a minE = default;
            foreach (a x in xs)
            {
                double res = f(x);
                if (res < min)
                {
                    min = res;
                    minE = x;
                }
            }
            return minE;
        }
        public static bool ContainsAny<a>(this IEnumerable<a> xs, IEnumerable<a> ys)
        {
            foreach (a y in ys)
                if (xs.Contains(y))
                    return true;
            return false;
        }
        public static string RemoveLastGroup(this string s, char seperator)
        {
            string[] split = s.Split(seperator);
            return split.Take(split.Length - 1).Foldl("", (a, b) => a + seperator + b).Remove(0, 1);
        } 
    }
}
