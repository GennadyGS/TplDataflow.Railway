using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks.Dataflow;
using Dataflow.Core;
using TplDataflow.Linq;

namespace Dataflow.TplDataflow
{
    public static class SourceBlockDataFlowExtensions
    {
        private static readonly ConcurrentDictionary<Tuple<Type, Type>, MethodInfo> _transformDataflowHelperMethods
            = new ConcurrentDictionary<Tuple<Type, Type>, MethodInfo>();

        public static ISourceBlock<TOutput> BindDataflow<TInput, TOutput>(this ISourceBlock<TInput> input,
            Func<IDataflowFactory, TInput, IDataflow<TOutput>> bindFunc)
        {
            var factory = new DataflowFactory(new DataflowTypeFactory());
            return input.Select(item => bindFunc(factory, item)).TransformDataflows();
        }

        private static ISourceBlock<TOutput> TransformDataflows<TOutput>(this ISourceBlock<IDataflow<TOutput>> dataflows)
        {
            return dataflows
                .GroupBy(dataflow => dataflow.Type)
                .SelectMany(group => TransformDataflowsByType(group.Key, group));
        }

        private static ISourceBlock<TOutput> TransformDataflowsByType<TOutput>(IDataflowType<TOutput> dataflowType, ISourceBlock<IDataflow<TOutput>> dataflows)
        {
            var transformDataflowHelperMethod = _transformDataflowHelperMethods.GetOrAdd(
                new Tuple<Type, Type>(typeof(TOutput), dataflowType.TypeOfDataflow),
                types => GetTransformDataflowHelperMethodInfo(types.Item1, types.Item2));
            return (ISourceBlock<TOutput>)transformDataflowHelperMethod.Invoke(null, new object[] { dataflowType, dataflows });
        }

        private static MethodInfo GetTransformDataflowHelperMethodInfo(Type outputType, Type typeOfDataflow)
        {
            return typeof(SourceBlockDataFlowExtensions)
                .GetMethod(nameof(TransformDataflowsHelper), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(outputType, typeOfDataflow);
        }

        private static ISourceBlock<TOutput> TransformDataflowsHelper<TOutput, TDataflow>(DataflowType<TOutput, TDataflow> dataflowType, ISourceBlock<IDataflow<TOutput>> dataflows)
            where TDataflow : IDataflow<TOutput>
        {
            return dataflowType.TransformDataFlows(dataflows.Cast<TDataflow>());
        }

        private class DataflowFactory : Core.DataflowFactory
        {
            public DataflowFactory(IDataflowTypeFactory typeFactory) : base(typeFactory)
            {
            }

            public IGroupedDataflow<TKey, TElement> CreateGroupedDataflow<TKey, TElement>(TKey key,
                ISourceBlock<TElement> items)
            {
                var type = GetOrCreateType(typeof(GroupedDataflow<TKey, TElement>),
                    () => new GroupedDataflowType<TKey, TElement>());
                return new GroupedDataflow<TKey, TElement>(this, type, key, items);
            }

            public IDataflow<T> CreateResultDataflow<T>(ISourceBlock<T> results)
            {
                var type = GetOrCreateType(typeof(ResultDataflow<T>),
                    () => new ResultDataflowType<T>());
                return new ResultDataflow<T>(this, type, results);
            }
        }

        private class DataflowTypeFactory : IDataflowTypeFactory
        {
            IDataflowType<TOutput> IDataflowTypeFactory.CreateCalculationType<TInput, TOutput, TDataflowOperator>()
            {
                return new DataflowCalculationType<TInput, TOutput, TDataflowOperator>();
            }

            IDataflowType<T> IDataflowTypeFactory.CreateReturnType<T>()
            {
                return new ReturnType<T>();
            }

            IDataflowType<T> IDataflowTypeFactory.CreateReturnAsyncType<T>()
            {
                return new ReturnAsyncType<T>();
            }

            IDataflowType<T> IDataflowTypeFactory.CreateReturnManyType<T>()
            {
                return new ReturnManyType<T>();
            }

            public IDataflowType<T> CreateReturnManyAsyncType<T>()
            {
                throw new NotImplementedException();
            }

            public IDataflowType<IList<T>> CreateBufferType<T>()
            {
                return new BufferType<T>();
            }

            public IDataflowType<IGroupedDataflow<TKey, TElement>> CreateGroupType<TKey, TElement>()
            {
                return new GroupType<TKey, TElement>();
            }

            public IDataflowType<IList<T>> CreateToListType<T>()
            {
                return new ToListType<T>();
            }
        }

        private abstract class DataflowType<T, TDataflow> : IDataflowType<T> where TDataflow : IDataflow<T>
        {
            public abstract ISourceBlock<T> TransformDataFlows(ISourceBlock<TDataflow> dataflows);

            public Type TypeOfDataflow => typeof(TDataflow);
        }

        private abstract class DataflowOperatorType<T, TDataflow> : DataflowType<T, TDataflow> where TDataflow : DataflowOperator<T, TDataflow>
        {
            public abstract ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(
                ISourceBlock<DataflowCalculation<T, TOutput, TDataflow>> calculationDataflows);
        }

        private class DataflowCalculationType<TInput, TOutput, TDataflowOperator> : DataflowType<TOutput, DataflowCalculation<TInput, TOutput, TDataflowOperator>>
            where TDataflowOperator : DataflowOperator<TInput, TDataflowOperator>
        {
            public override ISourceBlock<TOutput> TransformDataFlows(ISourceBlock<DataflowCalculation<TInput, TOutput, TDataflowOperator>> dataflows)
            {
                return dataflows
                    .GroupBy(dataflow => dataflow.Operator.Type)
                    .SelectMany(group => PerformOperatorTyped(group.Key, group))
                    .TransformDataflows();
            }

            private static ISourceBlock<IDataflow<TOutput>> PerformOperatorTyped(IDataflowType<TInput> dataflowType, ISourceBlock<DataflowCalculation<TInput, TOutput, TDataflowOperator>> group)
            {
                return ((DataflowOperatorType<TInput, TDataflowOperator>)dataflowType).PerformOperator(group);
            }
        }

        private class ReturnType<T> : DataflowOperatorType<T, Return<T>>
        {
            public override ISourceBlock<T> TransformDataFlows(ISourceBlock<Return<T>> dataflows)
            {
                return dataflows.Select(dataflow => dataflow.Result);
            }

            public override ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(
                ISourceBlock<DataflowCalculation<T, TOutput, Return<T>>> calculationDataflows)
            {
                return calculationDataflows.Select(dataflow =>
                    dataflow.Continuation(dataflow.Operator.Result));
            }
        }

        private class ReturnAsyncType<T> : DataflowOperatorType<T, ReturnAsync<T>>
        {
            public override ISourceBlock<T> TransformDataFlows(ISourceBlock<ReturnAsync<T>> dataflows)
            {
                return dataflows.SelectAsync(dataflow => dataflow.Result);
            }

            public override ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(
                ISourceBlock<DataflowCalculation<T, TOutput, ReturnAsync<T>>> calculationDataflows)
            {
                return calculationDataflows.SelectAsync(async dataflow =>
                    dataflow.Continuation(await dataflow.Operator.Result));
            }
        }

        private class ReturnManyType<T> : DataflowOperatorType<T, ReturnMany<T>>
        {
            public override ISourceBlock<T> TransformDataFlows(ISourceBlock<ReturnMany<T>> dataflows)
            {
                return dataflows.SelectMany(dataflow => dataflow.Result);
            }

            public override ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(
                ISourceBlock<DataflowCalculation<T, TOutput, ReturnMany<T>>> calculationDataflows)
            {
                return calculationDataflows.SelectMany(dataflow =>
                    dataflow.Operator.Result.Select(dataflow.Continuation));
            }
        }

        private class BufferType<T> : DataflowOperatorType<IList<T>, Buffer<T>>
        {
            public override ISourceBlock<IList<T>> TransformDataFlows(ISourceBlock<Buffer<T>> dataflows)
            {
                return dataflows
                    .GroupBy(item => new { item.BatchMaxSize, item.BatchTimeout })
                    .SelectMany(group => group
                        .Select(item => item.Item)
                        .Buffer(group.Key.BatchTimeout, group.Key.BatchMaxSize));
            }

            public override ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(
                ISourceBlock<DataflowCalculation<IList<T>, TOutput, Buffer<T>>> calculationDataflows)
            {
                return calculationDataflows
                    .GroupBy(dataflow => new
                    {
                        dataflow.Operator.BatchMaxSize,
                        dataflow.Operator.BatchTimeout
                    })
                    .SelectMany(group => group.Buffer(group.Key.BatchTimeout, group.Key.BatchMaxSize))
                    .Where(batch => batch.Count > 0)
                    .Select(batch =>
                        batch[0].Continuation(batch.Select(item => item.Operator.Item).ToList()));
            }
        }

        private class GroupType<TKey, TElement> : DataflowOperatorType<IGroupedDataflow<TKey, TElement>, Group<TKey, TElement>>
        {
            public override ISourceBlock<IGroupedDataflow<TKey, TElement>> TransformDataFlows(ISourceBlock<Group<TKey, TElement>> dataflows)
            {
                return dataflows
                    .GroupBy(item => item.KeySelector)
                    .SelectMany(group => group
                        .GroupBy(item => new { Key = group.Key(item.Item), Factory = item.Factory })
                        .Select(innerGroup =>
                            ((DataflowFactory)innerGroup.Key.Factory).CreateGroupedDataflow(
                                innerGroup.Key.Key, innerGroup.Select(item => item.Item))));
            }

            public override ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(ISourceBlock<DataflowCalculation<IGroupedDataflow<TKey, TElement>, TOutput, Group<TKey, TElement>>> calculationDataflows)
            {
                return calculationDataflows
                    .GroupBy(item => item.Operator.KeySelector)
                    .SelectMany(group => group
                        .GroupBy(item => new
                        {
                            Key = group.Key(item.Operator.Item),
                            Continuation = item.Continuation,
                            Factory = item.Factory
                        })
                        .Select(innerGroup => innerGroup.Key.Continuation(
                            ((DataflowFactory)innerGroup.Key.Factory).CreateGroupedDataflow(
                                innerGroup.Key.Key, innerGroup.Select(item => item.Operator.Item)))));
            }
        }

        private class ToListType<T> : DataflowOperatorType<IList<T>, ToList<T>>
        {
            public override ISourceBlock<IList<T>> TransformDataFlows(ISourceBlock<ToList<T>> dataflows)
            {
                return dataflows
                    .Select(dataflow => dataflow.Item)
                    .ToList();
            }

            public override ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(ISourceBlock<DataflowCalculation<IList<T>, TOutput, ToList<T>>> calculationDataflows)
            {
                return calculationDataflows
                    .ToList()
                    .Where(list => list.Count > 0)
                    .Select(list => list[0].Continuation(list.Select(item => item.Operator.Item).ToList()));
            }
        }

        private class GroupedDataflowType<TKey, TElement> : DataflowOperatorType<TElement, GroupedDataflow<TKey, TElement>>
        {
            public override ISourceBlock<TElement> TransformDataFlows(ISourceBlock<GroupedDataflow<TKey, TElement>> dataflows)
            {
                return dataflows.SelectMany(dataflow => dataflow.Items);
            }

            public override ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(ISourceBlock<DataflowCalculation<TElement, TOutput, GroupedDataflow<TKey, TElement>>> calculationDataflows)
            {
                return calculationDataflows
                    .Select(dataflow =>
                        ((DataflowFactory)dataflow.Factory).CreateResultDataflow(
                            dataflow.Operator.Items.BindDataflow((factory, item) =>
                                dataflow.Continuation(item))));
            }
        }

        public class GroupedDataflow<TKey, TElement> : DataflowOperator<TElement, GroupedDataflow<TKey, TElement>>, IGroupedDataflow<TKey, TElement>
        {
            public TKey Key { get; }

            public ISourceBlock<TElement> Items { get; }

            public GroupedDataflow(IDataflowFactory factory, IDataflowType<TElement> type, TKey key, ISourceBlock<TElement> items)
                : base(factory, type)
            {
                Key = key;
                Items = items;
            }
        }

        private class ResultDataflowType<T> : DataflowOperatorType<T, ResultDataflow<T>>
        {
            public override ISourceBlock<T> TransformDataFlows(ISourceBlock<ResultDataflow<T>> dataflows)
            {
                return dataflows.SelectMany(dataflow => dataflow.Results);
            }

            public override ISourceBlock<IDataflow<TOutput>> PerformOperator<TOutput>(ISourceBlock<DataflowCalculation<T, TOutput, ResultDataflow<T>>> calculationDataflows)
            {
                return calculationDataflows
                    .Select(dataflow =>
                        ((DataflowFactory)dataflow.Factory).CreateResultDataflow(
                            dataflow.Operator.Results.BindDataflow((factory, item) => dataflow.Continuation(item))));
            }
        }

        public class ResultDataflow<T> : DataflowOperator<T, ResultDataflow<T>>
        {
            public ISourceBlock<T> Results { get; }

            public ResultDataflow(IDataflowFactory factory, IDataflowType<T> type, ISourceBlock<T> results)
                : base(factory, type)
            {
                Results = results;
            }
        }
    }
}