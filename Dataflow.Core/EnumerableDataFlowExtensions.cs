using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Collection.Extensions;

namespace Dataflow.Core
{
    public static class EnumerableDataFlowExtensions
    {
        private static readonly ConcurrentDictionary<Tuple<Type, Type>, MethodInfo> _transformDataflowHelperMethods
            = new ConcurrentDictionary<Tuple<Type, Type>, MethodInfo>();

        private static readonly ConcurrentDictionary<Type, object> _typeCache = new ConcurrentDictionary<Type, object>();

        public static IEnumerable<TOutput> BindDataflow<TInput, TOutput>(this IEnumerable<TInput> input,
            Func<IDataflowFactory, TInput, IDataflow<TOutput>> bindFunc)
        {
            var factory = new DataflowFactory(new DataflowTypeFactory());
            return input.Select(item => bindFunc(factory, item)).TransformDataflows();
        }

        private static IEnumerable<TOutput> TransformDataflows<TOutput>(this IEnumerable<IDataflow<TOutput>> dataflows)
        {
            return dataflows
                .GroupBy(dataflow => dataflow.Type)
                .SelectMany(group => TransformDataflowsByType(group.Key, group));
        }

        private static IEnumerable<TOutput> TransformDataflowsByType<TOutput>(IDataflowType<TOutput> dataflowType, IEnumerable<IDataflow<TOutput>> dataflows)
        {
            var transformDataflowHelperMethod = _transformDataflowHelperMethods.GetOrAdd(
                new Tuple<Type, Type>(typeof(TOutput), dataflowType.TypeOfDataflow),
                types => GetTransformDataflowHelperMethodInfo(types.Item1, types.Item2));
            return (IEnumerable<TOutput>)transformDataflowHelperMethod.Invoke(null, new object[] { dataflowType, dataflows });
        }

        private static MethodInfo GetTransformDataflowHelperMethodInfo(Type outputType, Type typeOfDataflow)
        {
            return typeof(EnumerableDataFlowExtensions)
                .GetMethod(nameof(TransformDataflowsHelper), BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(outputType, typeOfDataflow);
        }

        private static IEnumerable<TOutput> TransformDataflowsHelper<TOutput, TDataflow>(DataflowType<TOutput, TDataflow> dataflowType, IEnumerable<IDataflow<TOutput>> dataflows)
            where TDataflow : IDataflow<TOutput>
        {
            return dataflowType.TransformDataFlows(dataflows.Cast<TDataflow>());
        }

        // TODO: Refactor
        private static IGroupedDataflow<TKey, TElement> CreateGroupedDataflow<TKey, TElement>(IDataflowFactory factory, TKey key,
            IEnumerable<TElement> items)
        {
            var type = (GroupedDataflowType<TKey, TElement>)_typeCache.GetOrAdd(
                typeof(GroupedDataflow<TKey, TElement>),
                _ => new GroupedDataflowType<TKey, TElement>());
            return new GroupedDataflow<TKey, TElement>(factory, type, key, items);
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

            IDataflowType<T> IDataflowTypeFactory.CreateReturnManyType<T>()
            {
                return new ReturnManyType<T>();
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
            public abstract IEnumerable<T> TransformDataFlows(IEnumerable<TDataflow> dataflows);

            public Type TypeOfDataflow => typeof(TDataflow);
        }

        private abstract class DataflowOperatorType<T, TDataflow> : DataflowType<T, TDataflow> where TDataflow : DataflowOperator<T, TDataflow>
        {
            public abstract IEnumerable<IDataflow<TOutput>> PerformOperator<TOutput>(
                IEnumerable<DataflowCalculation<T, TOutput, TDataflow>> calculationDataflows);
        }

        private class DataflowCalculationType<TInput, TOutput, TDataflowOperator> : DataflowType<TOutput, DataflowCalculation<TInput, TOutput, TDataflowOperator>>
            where TDataflowOperator : DataflowOperator<TInput, TDataflowOperator>
        {
            public override IEnumerable<TOutput> TransformDataFlows(IEnumerable<DataflowCalculation<TInput, TOutput, TDataflowOperator>> dataflows)
            {
                return dataflows
                    .GroupBy(dataflow => dataflow.Operator.Type)
                    .SelectMany(group => PerformOperatorTyped(group.Key, group))
                    .TransformDataflows();
            }

            private static IEnumerable<IDataflow<TOutput>> PerformOperatorTyped(IDataflowType<TInput> dataflowType, IEnumerable<DataflowCalculation<TInput, TOutput, TDataflowOperator>> group)
            {
                return ((DataflowOperatorType<TInput, TDataflowOperator>)dataflowType).PerformOperator(group);
            }
        }

        private class ReturnType<T> : DataflowOperatorType<T, Return<T>>
        {
            public override IEnumerable<T> TransformDataFlows(IEnumerable<Return<T>> dataflows)
            {
                return dataflows.Select(dataflow => dataflow.Result);
            }

            public override IEnumerable<IDataflow<TOutput>> PerformOperator<TOutput>(
                IEnumerable<DataflowCalculation<T, TOutput, Return<T>>> calculationDataflows)
            {
                return calculationDataflows.Select(dataflow =>
                    dataflow.Continuation(dataflow.Operator.Result));
            }
        }

        private class ReturnManyType<T> : DataflowOperatorType<T, ReturnMany<T>>
        {
            public override IEnumerable<T> TransformDataFlows(IEnumerable<ReturnMany<T>> dataflows)
            {
                return dataflows.SelectMany(dataflow => dataflow.Result);
            }

            public override IEnumerable<IDataflow<TOutput>> PerformOperator<TOutput>(
                IEnumerable<DataflowCalculation<T, TOutput, ReturnMany<T>>> calculationDataflows)
            {
                return calculationDataflows.SelectMany(dataflow =>
                    dataflow.Operator.Result.Select(dataflow.Continuation));
            }
        }

        private class BufferType<T> : DataflowOperatorType<IList<T>, Buffer<T>>
        {
            public override IEnumerable<IList<T>> TransformDataFlows(IEnumerable<Buffer<T>> dataflows)
            {
                return dataflows
                    .GroupBy(item => new { item.BatchMaxSize, item.BatchTimeout })
                    .SelectMany(group => group
                        .Select(item => item.Item)
                        .Buffer(group.Key.BatchTimeout, group.Key.BatchMaxSize));
            }

            public override IEnumerable<IDataflow<TOutput>> PerformOperator<TOutput>(
                IEnumerable<DataflowCalculation<IList<T>, TOutput, Buffer<T>>> calculationDataflows)
            {
                return calculationDataflows
                    .GroupBy(dataflow => new
                    {
                        dataflow.Operator.BatchMaxSize,
                        dataflow.Operator.BatchTimeout
                    })
                    .SelectMany(group => group.Buffer(group.Key.BatchTimeout, group.Key.BatchMaxSize))
                    .Where(batch => batch.Count > 0)
                    .Select(batch => new
                    {
                        Items = batch.Select(item => item.Operator.Item).ToList(),
                        batch.First().Continuation
                    })
                    .Select(batch => batch.Continuation(batch.Items));
            }
        }

        private class GroupType<TKey, TElement> : DataflowOperatorType<IGroupedDataflow<TKey, TElement>, Group<TKey, TElement>>
        {
            public override IEnumerable<IGroupedDataflow<TKey, TElement>> TransformDataFlows(IEnumerable<Group<TKey, TElement>> dataflows)
            {
                // TODO: Fix
                return dataflows
                    .GroupBy(item => item.KeySelector)
                    .SelectMany(group => group
                        .Select(item => item)
                        .GroupBy(item => new { Key = group.Key(item.Item), Factory = item.Factory })
                        .Select(innerGroup => CreateGroupedDataflow(
                            innerGroup.Key.Factory, innerGroup.Key.Key, 
                            innerGroup.Select(item => item.Item))));
            }

            public override IEnumerable<IDataflow<TOutput>> PerformOperator<TOutput>(IEnumerable<DataflowCalculation<IGroupedDataflow<TKey, TElement>, TOutput, Group<TKey, TElement>>> calculationDataflows)
            {
                return calculationDataflows
                    .GroupBy(item => item.Operator.KeySelector)
                    .SelectMany(group => group
                        .Select(item => item)
                        .GroupBy(item => new
                        {
                            Key = group.Key(item.Operator.Item),
                            Factory = item.Factory
                        })
                        .Select(innerGroup => new
                        {
                            Group = CreateGroupedDataflow(
                                innerGroup.Key.Factory, innerGroup.Key.Key,
                                innerGroup.Select(item => item.Operator.Item)),
                            Continuation = innerGroup.First().Continuation
                        })
                        .Select(groupAndCont => groupAndCont.Continuation(groupAndCont.Group)));
            }
        }

        private class ToListType<T> : DataflowOperatorType<IList<T>, ToList<T>>
        {
            public override IEnumerable<IList<T>> TransformDataFlows(IEnumerable<ToList<T>> dataflows)
            {
                return dataflows
                    .Select(dataflow => dataflow.Item)
                    .ToListEnumerable();
            }

            public override IEnumerable<IDataflow<TOutput>> PerformOperator<TOutput>(IEnumerable<DataflowCalculation<IList<T>, TOutput, ToList<T>>> calculationDataflows)
            {
                var calculationDataflowList = calculationDataflows.ToList();
                return calculationDataflowList
                    .Select(dataflow => dataflow.Operator.Item)
                    .ToListEnumerable()
                    .Select(list => calculationDataflowList[0].Continuation(list));
            }
        }

        private class GroupedDataflowType<TKey, TElement> : DataflowOperatorType<TElement, GroupedDataflow<TKey, TElement>>
        {
            public override IEnumerable<TElement> TransformDataFlows(IEnumerable<GroupedDataflow<TKey, TElement>> dataflows)
            {
                return dataflows.SelectMany(dataflow => dataflow.Items);
            }

            public override IEnumerable<IDataflow<TOutput>> PerformOperator<TOutput>(IEnumerable<DataflowCalculation<TElement, TOutput, GroupedDataflow<TKey, TElement>>> calculationDataflows)
            {
                return calculationDataflows
                    .Select(dataflow => 
                        dataflow.Factory.ReturnMany(dataflow.Operator.Items.BindDataflow((factory, item) => 
                            dataflow.Continuation(item))));
            }
        }

        public class GroupedDataflow<TKey, TElement> : DataflowOperator<TElement, GroupedDataflow<TKey, TElement>>, IGroupedDataflow<TKey, TElement>
        {
            public TKey Key { get; }

            public IEnumerable<TElement> Items { get; }

            public GroupedDataflow(IDataflowFactory factory, IDataflowType<TElement> type, TKey key, IEnumerable<TElement> items)
                : base(factory, type)
            {
                Key = key;
                Items = items;
            }
        }
    }
}