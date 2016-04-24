namespace TplDataFlow.Extensions.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    using FluentAssertions;

    using Xunit;

    public class Test
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

            IList<int> outputItems = combined
                .AsObservable()
                .ToEnumerable()
                .ToList();

            outputItems.Should().BeEquivalentTo(items);
        }
    }
}