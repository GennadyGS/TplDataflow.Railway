using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;
using TplDataflow.Linq;
using Xunit;

namespace TplDataFlow.Extensions.UnitTests
{
    public class DataFlowBlockExtensionsTests
    {
        [Fact]
        public void TestSwitch()
        {
            var input = new[] { 1, 2, 3, 4, 5 };
            Predicate<int> predicate = i => i % 2 == 0;

            var target1 = new BufferBlock<int>();
            var target2 = new BufferBlock<int>();

            var inputBlock = new TransformBlock<int, int>(i => i);

            inputBlock
                .LinkWhen(predicate, target1)
                .LinkOtherwise(target2);

            input.ToObservable()
                .Subscribe(inputBlock.AsObserver());

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
        public void TestSelect()
        {
            var items = new[] { 1, 2, 3 };

            var source = new BufferBlock<int>();

            Func<int, int> transformFunc = i => i + 1;

            var sut =
                from i in source
                select transformFunc(i);

            items.ToObservable()
                .Subscribe(source.AsObserver());

            IList<int> output = sut
                .AsObservable()
                .ToEnumerable()
                .ToList();

            output.Should().BeEquivalentTo(items.Select(i => transformFunc(i)));
        }
    }
}