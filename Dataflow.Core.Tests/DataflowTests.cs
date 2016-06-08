using System;
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
        public void BindReturnDataflowToEnumerable_ShouldReturnTheSameList()
        {
            int[] input = { 1, 2, 3 };

            IEnumerable<int> result = input.BindDataflow(Dataflow.Return<int, int>);

            result.ShouldAllBeEquivalentTo(input);
        }

        [Fact]
        public void BindReturnDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            IEnumerable<int> result = input.BindDataflow(i => Dataflow.Return<int, int>(i * 2));

            result.ShouldAllBeEquivalentTo(input.Select(i => i * 2));
        }

        [Fact]
        public void BindSelectDataflowToEnumerable_ShouldReturnTheProjectedList()
        {
            int[] input = { 1, 2, 3 };

            IEnumerable<int> result = input.BindDataflow(i => 
                Dataflow.Return<int, int>(i)
                    .Select(item => item * 2));

            result.ShouldAllBeEquivalentTo(input.Select(i => i * 2));
        }
    }
}
