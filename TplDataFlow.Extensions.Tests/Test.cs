namespace TplDataFlow.Extensions.UnitTests
{
    using System.Collections.Generic;
    using System.Linq;
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

            var previous = new BufferBlock<int>();
            var next = new BufferBlock<int>();

            var combined = previous.Combine(next);

            previous.LinkTo(next, new DataflowLinkOptions { PropagateCompletion = true });

            items.ToList().ForEach(item => combined.Post(item));
            combined.Complete();

            IList<int> outputItems = new List<int>();

            while (await combined.OutputAvailableAsync())
            {
                outputItems.Add(combined.Receive());
            }
            outputItems.Should().BeEquivalentTo(items);
        }
    }
}