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

            IEnumerable<int> result = input.BindDataflow(i =>
                Dataflow.Return(i)
                    .Select(item => item * 2)
                    .Select(item => item + 1));

            result.ShouldAllBeEquivalentTo(input.Select(i => i * 2 + 1));
        }
    }
}
