using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AsyncProcessing.Core
{
    public static class ObservableExtensions
    {
        public static Task<IList<T>> ToListAsync<T>(this IObservable<T> observable)
        {
            var taskSource = new TaskCompletionSource<IList<T>>();
            var result = new List<T>();
            observable
                .Subscribe(
                    onNext: result.Add,
                    onError: taskSource.SetException,
                    onCompleted: () => taskSource.SetResult(result));
            return taskSource.Task;
        }
    }
}