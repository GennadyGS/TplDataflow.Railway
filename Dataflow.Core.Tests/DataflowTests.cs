using Collection.Extensions;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Dataflow.Rx;
using Xunit;

namespace Dataflow.Core.Tests
{
    public class DataflowTests
    {
        [Fact]
        public void BindReturnDataflow_ShouldReturnTheSameList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input;
            
            TestBindDataflow(input, expectedOutput, (dataflowFactory, i) => dataflowFactory.Return(i));
        }

        [Fact]
        public void BindReturnDataflow_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => i * 2);

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) => dataflowFactory.Return(i * 2));
        }

        [Fact]
        public void BindSelectDataflow_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => i * 2);

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Select(item => item * 2));
        }

        [Fact]
        public void BindMultipleSelectDataflow_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => (i * 2 + 1) * 2);

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Select(item => item * 2)
                    .Select(item => item + 1)
                    .Select(item => item * 2));
        }

        [Fact]
        public void BindMultipleSelectDataflow_ShouldReturnTheProjectedListCrossType()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => $"{2000 + i}-01-01T00:00:00.0000000");

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Select(item => new DateTime(2000 + item, 1, 1))
                    .Select(item => item.ToString("O")));
        }

        [Fact]
        public void BindMultipleNestedDataflows_ShouldReturnTheProjectedListCrossType()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => $"{2000 + i}-01-01T00:00:00.0000000");

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Bind(item =>
                        dataflowFactory.Return(new DateTime(2000 + item, 1, 1))
                            .Bind(item2 =>
                                dataflowFactory.Return(item2.ToString("O")))));
        }

        [Fact]
        public void BindSelectManyDataflow_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.SelectMany(item => Enumerable.Repeat(item * 2 + 1, 2));

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .SelectMany(item => Enumerable.Repeat(item * 2, 2))
                    .Select(item => item + 1));

        }

        [Fact]
        public void BinNestedSelectManyDataflow_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.SelectMany(item => Enumerable.Repeat(item * 2 + 1, 2));

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Bind(j =>
                        dataflowFactory.ReturnMany(Enumerable.Repeat(j * 2, 2))
                            .Bind(k =>
                                dataflowFactory.Return(k + 1))));
        }

        [Fact]
        public void BindDataflow_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => i + 1);

            TestBindDataflow(expectedOutput, input, (dataflowFactory, x) =>
                dataflowFactory.Return(1)
                    .Bind(y => dataflowFactory.Return(x + y)));
        }

        [Fact]
        public void BindComplexDataflow_ShouldReturnTheProjectedList2()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => i + 1);

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Bind(x =>
                        dataflowFactory.Return(1)
                            .Bind(y => dataflowFactory.Return(x + y))));

        }

        [Fact]
        public void BindLinqDataflow_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(item => item + 1);

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                from x in dataflowFactory.Return(i)
                from y in dataflowFactory.Return(1)
                select x + y);
        }

        [Fact]
        public void BindBufferDataflow_ShouldReturnTheProjectedList()
        {
            const int batchSize = 3;

            var input = Enumerable.Range(0, 100).ToList();

            var expectedOutput = input.Select(item => item + 1).Buffer(TimeSpan.FromDays(1), batchSize);

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) => dataflowFactory.Return(i)
                .Select(item => item + 1)
                .Buffer(TimeSpan.FromDays(1), batchSize));
        }

        [Fact]
        public void BindBufferAndSelectDataflow_ShouldReturnTheProjectedList()
        {
            const int batchSize = 3;

            var input = Enumerable.Range(0, 100).ToList();

            var expectedOutput = input.Buffer(TimeSpan.FromDays(1), batchSize).Select(i => i.Count);

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) => dataflowFactory.Return(i)
                .Buffer(TimeSpan.FromDays(1), batchSize)
                .Select(item => item.Count));

        }

        [Fact]
        public void BindGroupByDataflow_ShouldReturnCorrectResult()
        {
            const int itemCount = 50;
            var input = Enumerable.Range(0, itemCount * 2).ToList();

            var expectedOutput = input
                .GroupBy(i => i % 2)
                .Select(group => group.ToList());

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
            {
                return dataflowFactory
                    .Return(i)
                    .GroupBy(item => item % 2)
                    .SelectMany(group => group.ToList());
            });

        }

        private static void TestBindDataflow<TInput, TOutput>(IEnumerable<TOutput> expectedOutput, IEnumerable<TInput> input, Func<IDataflowFactory, TInput, IDataflow<TOutput>> dataflow)
        {
            var inputList = input.ToList();
            var expectedOutputList = expectedOutput.ToList();

            inputList
                .BindDataflow(dataflow)
                .ShouldAllBeEquivalentTo(expectedOutputList, "Enumerable result should be correct");

            inputList
                .ToObservable()
                .BindDataflow(dataflow)
                .ToEnumerable()
                .ShouldAllBeEquivalentTo(expectedOutputList, "Observable result should be correct");
        }
    }
}
