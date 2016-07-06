using Collection.Extensions;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Xunit;

namespace Dataflow.Core.Tests
{
    public class DataflowTests
    {
        [Fact]
        public void BindReturnDataflowToEnumerable_ShouldReturnTheSameList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input;
            
            TestDataflow(input, expectedOutput, (dataflowFactory, i) => dataflowFactory.Return(i));
        }

        [Fact]
        public void BindReturnDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => i * 2);

            TestDataflow(expectedOutput, input, (dataflowFactory, i) => dataflowFactory.Return(i * 2));
        }

        [Fact]
        public void BindSelectDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => i * 2);

            TestDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Select(item => item * 2));
        }

        [Fact]
        public void BindMultipleSelectDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => (i * 2 + 1) * 2);

            TestDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Select(item => item * 2)
                    .Select(item => item + 1)
                    .Select(item => item * 2));
        }

        [Fact]
        public void BindMultipleSelectDataflowToEnumerable_ShouldReturnTheProjectedListCrossType()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => $"{2000 + i}-01-01T00:00:00.0000000");

            TestDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Select(item => new DateTime(2000 + item, 1, 1))
                    .Select(item => item.ToString("O")));
        }

        [Fact]
        public void BindMultipleNestedDataflowsToEnumerable_ShouldReturnTheProjectedListCrossType()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => $"{2000 + i}-01-01T00:00:00.0000000");

            TestDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Bind(item =>
                        dataflowFactory.Return(new DateTime(2000 + item, 1, 1))
                            .Bind(item2 =>
                                dataflowFactory.Return(item2.ToString("O")))));
        }

        [Fact]
        public void BindSelectManyDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.SelectMany(item => Enumerable.Repeat(item * 2 + 1, 2));

            TestDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .SelectMany(item => Enumerable.Repeat(item * 2, 2))
                    .Select(item => item + 1));

        }

        [Fact]
        public void BinNestedSelectManyDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.SelectMany(item => Enumerable.Repeat(item * 2 + 1, 2));

            TestDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Bind(j =>
                        dataflowFactory.ReturnMany(Enumerable.Repeat(j * 2, 2))
                            .Bind(k =>
                                dataflowFactory.Return(k + 1))));
        }

        [Fact]
        public void BindDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => i + 1);

            TestDataflow(expectedOutput, input, (dataflowFactory, x) =>
                dataflowFactory.Return(1)
                    .Bind(y => dataflowFactory.Return(x + y)));
        }

        [Fact]
        public void BindComplexDataflowToEnumerable_ShouldReturnTheProjectedList2()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => i + 1);

            TestDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .Bind(x =>
                        dataflowFactory.Return(1)
                            .Bind(y => dataflowFactory.Return(x + y))));

        }

        [Fact]
        public void BindLinqDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(item => item + 1);

            TestDataflow(expectedOutput, input, (dataflowFactory, i) =>
                from x in dataflowFactory.Return(i)
                from y in dataflowFactory.Return(1)
                select x + y);
        }

        [Fact]
        public void BindBufferDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            const int batchSize = 3;

            var input = Enumerable.Range(0, 100).ToList();

            var expectedOutput = input.Select(item => item + 1).Buffer(TimeSpan.FromDays(1), batchSize);

            TestDataflow(expectedOutput, input, (dataflowFactory, i) => dataflowFactory.Return(i)
                .Select(item => item + 1)
                .Buffer(TimeSpan.FromDays(1), batchSize));
        }

        [Fact]
        public void BindBufferAndSelectDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            const int batchSize = 3;

            var input = Enumerable.Range(0, 100).ToList();

            var expectedOutput = input.Buffer(TimeSpan.FromDays(1), batchSize).Select(i => i.Count);

            TestDataflow(expectedOutput, input, (dataflowFactory, i) => dataflowFactory.Return(i)
                .Buffer(TimeSpan.FromDays(1), batchSize)
                .Select(item => item.Count));

        }

        private static void TestDataflow<TInput, TOutput>(IEnumerable<TOutput> expectedOutput, IEnumerable<TInput> input, Func<IDataflowFactory, TInput, Dataflow<TOutput>> dataflow)
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
