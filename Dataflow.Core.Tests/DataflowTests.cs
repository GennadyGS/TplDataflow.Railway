using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Railway.Linq;
using Xunit;

namespace Dataflow.Core.Tests
{
    public class DataflowTests
    {
        [Fact]
        public void Return_ShouldReturnDataflow()
        {
            int input = 1;

            var result = Dataflow.Return(input);

            result.Should().NotBeNull();
        }

        [Fact]
        public void BindReturnDataflowToEnumerable_ShouldReturnTheSameList()
        {
            int[] input = { 1, 2, 3 };

            IEnumerable<int> result = input.BindDataflow(Dataflow.Return);

            result.ShouldAllBeEquivalentTo(input);
        }

        [Fact]
        public void BindReturnDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            IEnumerable<int> result = input.BindDataflow(i => Dataflow.Return(i * 2));

            result.ShouldAllBeEquivalentTo(input.Select(i => i * 2));
        }

        [Fact]
        public void BindSelectDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            IEnumerable<int> result = input.BindDataflow(i =>
                Dataflow.Return(i)
                    .Select(item => item * 2));

            result.ShouldAllBeEquivalentTo(input.Select(i => i * 2));
        }

        [Fact]
        public void BindMultipleSelectDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            Func<int, Dataflow<int>> bindFunc = i =>
                Dataflow.Return(i)
                    .Select(item => item * 2)
                    .Select(item => item + 1)
                    .Select(item => item * 2);

            IEnumerable<int> result = input.BindDataflow(bindFunc);

            result.ShouldAllBeEquivalentTo(input.Select(i => (i * 2 + 1) * 2));
        }

        [Fact]
        public void BindMultipleSelectDataflowToEnumerable_ShouldReturnTheProjectedListCrossType()
        {
            int[] input = { 1, 2, 3 };

            IEnumerable<string> result = input.BindDataflow(i =>
                Dataflow.Return(i)
                    .Select(item => new DateTime(2000 + item, 1, 1))
                    .Select(item => item.ToString("O")));

            result.ShouldAllBeEquivalentTo(input.Select(i => $"{2000 + i}-01-01T00:00:00.0000000"));
        }

        [Fact]
        public void BindSelectManyDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            IEnumerable<int> result = input.BindDataflow(i =>
                Dataflow.Return(i)
                    .SelectMany(item => Enumerable.Repeat(item * 2, 2))
                    .Select(item => item + 1));

            result.ShouldAllBeEquivalentTo(input.SelectMany(item => Enumerable.Repeat(item * 2 + 1, 2)));
        }

        [Fact]
        public void BindBindDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            IEnumerable<int> result = input.BindDataflow(i =>
                Dataflow.Return(i)
                    .Bind(x =>
                    {
                        return Dataflow.Return(1)
                            .Bind(y => Dataflow.Return(x + y));
                    }));

            result.ShouldAllBeEquivalentTo(input.Select(item => item + 1));
        }

        [Fact]
        public void BindLinqDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            IEnumerable<int> result = input.BindDataflow(i =>
                from x in Dataflow.Return(i)
                from y in Dataflow.Return(1)
                select x + y);

            result.ShouldAllBeEquivalentTo(input.Select(item => item + 1));
        }

        [Fact]
        public void BindBufferDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            const int batchSize = 3;

            var input = Enumerable.Range(0, 100).ToList();

            IEnumerable<IList<int>> result = input.BindDataflow(i => Dataflow.Return(i)
                .Buffer(TimeSpan.MaxValue, batchSize));

            result.ShouldAllBeEquivalentTo(input.Buffer(batchSize));
        }

        [Fact]
        public void BindBufferAndSelectDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            const int batchSize = 3;

            var input = Enumerable.Range(0, 100).ToList();

            IEnumerable<int> result = input.BindDataflow(i => Dataflow.Return(i)
                .Buffer(TimeSpan.MaxValue, batchSize)
                .Select(item => item.Count));

            result.ShouldAllBeEquivalentTo(input.Buffer(batchSize).Select(i => i.Count));
        }
    }
}
