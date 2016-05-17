using System;

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
    }
}