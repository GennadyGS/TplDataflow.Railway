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
        public void TestCombine()
        {
            var items = new[] { 1, 2, 3 };

            var combined = new TransformBlock<int, int>(i => i)
                .Combine(new BufferBlock<int>());

            items.ToObservable()
                .Subscribe(combined.AsObserver());

            IList<int> output = combined
                .AsObservable()
                .ToEnumerable()
                .ToList();

            output.Should().BeEquivalentTo(items);
        }

        [Fact]
        public void TestSwitch()
        {
            var input = new[] { 1, 2, 3, 4, 5 };
            Predicate<int> predicate = i => i % 2 == 0;

            var target1 = new BufferBlock<int>();
            var target2 = new BufferBlock<int>();

            var combined = new TransformBlock<int, int>(i => i)
                .LinkWhen(predicate, target1)
                .LinkOtherwise(target2);

            input.ToObservable()
                .Subscribe(combined.AsObserver());

            IList<int> output1 = target1
                .AsObservable()
                .ToEnumerable()
                .ToList();
            IList<int> output2 = target2
                .AsObservable()
                .ToEnumerable()
                .ToList();

            output1.Should()
                .BeEquivalentTo(input.Where(i => predicate(i)));
            output2.Should()
                .BeEquivalentTo(input.Where(i => !predicate(i)));
        }

        [Fact]
        public void TestFork()
        {
            var input = new[] { 1, 2, 3, 4, 5 };

            var target1 = new BufferBlock<int>();
            var target2 = new BufferBlock<string>();

            var combined =
                new TransformBlock<int, int>(i => i)
                    .LinkWith(new ForkBlock<int, int, string>(async i => new Tuple<int, string>(i, i.ToString()))
                        .ForkTo(
                            target1, 
                            target2)
                    );

            input.ToObservable()
                .Subscribe(combined.AsObserver());

            IList<int> output1 = target1
                .AsObservable()
                .ToEnumerable()
                .ToList();
            IList<string> output2 = target2
                .AsObservable()
                .ToEnumerable()
                .ToList();

            output1.Should()
                .BeEquivalentTo(input);
            output2.Should()
                .BeEquivalentTo(input.Select(i => i.ToString()));
        }
    }
}