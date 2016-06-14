using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Railway.Linq;

namespace Dataflow.Core
{
    public static class EnumerableDataFlowExtensions
    {
        public static IEnumerable<TOutput> BindDataflow<TInput, TOutput>(this IEnumerable<TInput> input, Func<TInput, Dataflow<TOutput>> bindFunc)
        {
            var enumerable = input.Select(bindFunc);
            return TransformDataflow(enumerable);
        }

        private static IEnumerable<TOutput> TransformDataflow<TOutput>(IEnumerable<Dataflow<TOutput>> dataflows)
        {
            return dataflows
                .GroupBy(dataflow => dataflow.GetType())
                .SelectMany(TransformDataflowGroup);

        }

        private static IEnumerable<TOutput> TransformDataflowGroup<TOutput>(IGrouping<Type, Dataflow<TOutput>> group)
        {
            if (group.Key == typeof(Return<TOutput>))
            {
                return group.Select(dataflow => ((Return<TOutput>)dataflow).Result);
            }
            if (group.Key == typeof(ReturnMany<TOutput>))
            {
                return group.SelectMany(dataflow => ((ReturnMany<TOutput>)dataflow).Result);
            }
            if (group.Key == typeof(Continuation<TOutput>))
            {
                return TransformDataflow(group.Select(dataflow => ((Continuation<TOutput>)dataflow).Func()));
            }
            if (group.Key == typeof(ContinuationMany<TOutput>))
            {
                return TransformDataflow(group.SelectMany(dataflow => ((ContinuationMany<TOutput>)dataflow).Func()));
            }
            if (group.Key == typeof(Buffer<TOutput>))
            {
                Debug.Assert(typeof(TOutput).IsGenericType && typeof(TOutput).GetGenericTypeDefinition() == typeof(IList<>));
                var baseType = typeof(TOutput).GenericTypeArguments[0];
                var buffer = (Buffer<TOutput>)group.First();
                var items = group
                    .Buffer(buffer.BatchMaxSize)
                    .SelectMany(item => item.Cast<Buffer<TOutput>>().Select(bf => bf.Item));
                var res = typeof(EnumerableDataFlowExtensions)
                    .GetMethod("ConcatLists")
                    .MakeGenericMethod(baseType)
                    .Invoke(null, new object[] {items});
            }
            throw new InvalidOperationException();
        }

        private static IList<T> ConcatLists<T>(IEnumerable<IList<T>> lists)
        {
            return lists.SelectMany(item => item).ToList();
        }
    }
}