﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Collection.Extensions;
using Dataflow.Core;
using Xunit;

namespace Dataflow.Tests
{
    public abstract class DataflowTests
    {
        private readonly IDataflowTestEngine testEngine;

        public DataflowTests(IDataflowTestEngine testEngine)
        {
            this.testEngine = testEngine;
        }

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

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) => 
                dataflowFactory.Return(i * 2));
        }

        [Fact]
        public void BindReturnAsyncDataflow_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => i * 2);

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) => 
                dataflowFactory.ReturnAsync(Task.FromResult(i * 2)));
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
        public void BindSelectAsyncDataflow_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => i * 2);

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .SelectAsync(item => Task.FromResult(item * 2)));
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
        public void BindMultipleSelectAsyncDataflow_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.Select(i => (i * 2 + 1) * 2);

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .SelectAsync(item => Task.FromResult(item * 2))
                    .SelectAsync(item => Task.FromResult(item + 1))
                    .SelectAsync(item => Task.FromResult(item * 2)));
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
        public void BindSelectManyAsyncDataflow_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            var expectedOutput = input.SelectMany(item => Enumerable.Repeat(item * 2 + 1, 2));

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.Return(i)
                    .SelectManyAsync(item => Task.FromResult(Enumerable.Repeat(item * 2, 2)))
                    .SelectAsync(item => Task.FromResult(item + 1)));

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
        public void BindToListDataflow_ShouldReturnCorrectResult()
        {
            var input = Enumerable.Range(0, 100).ToList();

            var expectedOutput = input.ToListEnumerable();

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.ToList(i));
        }

        [Fact]
        public void BindToListAndSelectManyDataflow_ShouldReturnCorrectResult()
        {
            var input = Enumerable.Range(0, 100).ToList();

            var expectedOutput = input.ToListEnumerable();

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) =>
                dataflowFactory.ToList(i));
        }

        [Fact]
        public void BindGroupByDataflow_ShouldReturnCorrectResult()
        {
            const int itemCount = 50;
            var input = Enumerable.Range(0, itemCount * 2).ToList();

            var expectedOutput = input
                .GroupBy(i => i % 2)
                .SelectMany(group => group.ToListEnumerable());

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) => dataflowFactory
                .Return(i)
                .GroupBy(item => item % 2)
                .SelectMany(group => group.ToList()));
        }

        [Fact]
        public void BindGroupByDataflowWithInnerDataflow_ShouldReturnCorrectResult()
        {
            const int itemCount = 50;
            var input = Enumerable.Range(0, itemCount * 2).ToList();

            var expectedOutput = input
                .GroupBy(i => i % 2)
                .SelectMany(group =>
                    group
                        .Select(item => item + 1)
                        .ToListEnumerable()
                        .SelectMany(item => item));

            TestBindDataflow(expectedOutput, input, (dataflowFactory, i) => dataflowFactory
                .Return(i)
                .GroupBy(item => item % 2)
                .SelectMany(group =>
                    group
                        .Select(item => item + 1)
                        .ToList()
                        .SelectMany(item => item)));
        }

        private void TestBindDataflow<TInput, TOutput>(IEnumerable<TOutput> expectedOutput, IEnumerable<TInput> input, Func<IDataflowFactory, TInput, IDataflow<TOutput>> dataflowBindFunc)
        {
            var inputList = input.ToList();
            var expectedOutputList = expectedOutput.ToList();
            testEngine.TestBindDataflow(expectedOutputList, inputList, dataflowBindFunc);
        }
    }

    public class EnumerableDataflowTestsImpl : DataflowTests
    {
        public EnumerableDataflowTestsImpl() : base(new EnumerableDataflowTestEngine())
        {
        }
    }

    public class ObservableDataflowTestsImpl : DataflowTests
    {
        public ObservableDataflowTestsImpl() : base(new ObservableDataflowTestEngine())
        {
        }
    }

    public class TplDataflowDataflowTestsImpl : DataflowTests
    {
        public TplDataflowDataflowTestsImpl() : base(new TplDataflowDataflowTestEngine())
        {
        }
    }
}
