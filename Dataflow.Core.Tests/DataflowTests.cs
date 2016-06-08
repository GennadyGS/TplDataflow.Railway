using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Dataflow.Core.Tests
{
    public class DataflowTests
    {
        [Fact]
        public void Return_ShouldReturnDataflow()
        {
            int input = 1;

            var result = Dataflow.Return<int, int>(input);

            result.Should().NotBeNull();
        }

        [Fact]
        public void ReturnDataflowProjectedToEnumerable_ShouldReturnValue()
        {
            int[] input = { 1 };

            IEnumerable<int> result = input.BindDataflow(Dataflow.Return<int, int>);

            result.ShouldAllBeEquivalentTo(input);
        }
    }
}
