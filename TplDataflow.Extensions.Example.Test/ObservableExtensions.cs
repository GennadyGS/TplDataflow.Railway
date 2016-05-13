using System;
using System.Collections.Generic;
using System.Reactive;

namespace TplDataflow.Extensions.Example.Test
{
    public static class ObservableExtensions
    {
        public static IObserver<T> CreateObserver<T>(this ICollection<T> collection)
        {
            return Observer.Create<T>(collection.Add);
        }

        public static IList<T> CreateList<T>(this IObservable<T> observable)
        {
            var result = new List<T>();
            observable.Subscribe(CreateObserver(result));
            return result;
        }
    }
}