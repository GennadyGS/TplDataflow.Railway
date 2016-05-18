using System;
using System.Collections.Generic;
using System.Linq;

namespace TplDataFlow.Extensions
{
    public static class ActionExtensions
    {
        public static Func<T, Unit> ToFunc<T>(this Action<T> action)
        {
            return arg =>
            {
                action(arg);
                return Unit.Default;
            };
        }

        public static Func<IEnumerable<T>, IEnumerable<Unit>> ToFunc<T>(this Action<IEnumerable<T>> action)
        {
            return arg =>
            {
                action(arg);
                return Enumerable.Repeat(Unit.Default, 1);
            };
        }
    }
}