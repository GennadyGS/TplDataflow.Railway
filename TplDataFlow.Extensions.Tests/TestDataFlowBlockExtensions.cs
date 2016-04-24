namespace TplDataFlow.Extensions.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    using FluentAssertions;

    using Xunit;

    public class TestDataFlowBlockExtensions
    {
        [Fact]
        public async Task TestCombine()
        {
            var items = new[] { 1, 2, 3 };

            var previous = new TransformBlock<int, int>(i => i);
            var next = new BufferBlock<int>();

            var combined = previous.Combine(next);

            items.ToObservable()
                .Subscribe(combined.AsObserver());

            IList<int> output = combined
                .AsObservable()
                .ToEnumerable()
                .ToList();

            output.Should().BeEquivalentTo(items);
        }

        [Fact]
        public async Task TestSwitch()
        {
            var input = new[] { 1, 2, 3, 4, 5 };
            Predicate<int> predicate = i => i % 2 == 0;

            var previous = new TransformBlock<int, int>(i => i);
            var next1 = new BufferBlock<int>();
            var next2 = new BufferBlock<int>();

            var combined = previous
                .LinkWhen(predicate, next1)
                .LinkOtherwise(next2);

            input.ToObservable()
                .Subscribe(combined.AsObserver());

            IList<int> output1 = next1
                .AsObservable()
                .ToEnumerable()
                .ToList();

            IList<int> output2 = next2
                .AsObservable()
                .ToEnumerable()
                .ToList();

            output1.Should()
                .BeEquivalentTo(input.Where(i => predicate(i)));
            output2.Should()
                .BeEquivalentTo(input.Where(i => !predicate(i)));
        }
    }
}