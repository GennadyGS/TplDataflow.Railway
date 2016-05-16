using System;
using FluentAssertions;
using Xunit;

namespace TplDataFlow.Extensions.UnitTests
{
    public class ResultTests
    {
        [Fact]
        public void Result_ShouldBeSuccess()
        {
            const int input = 1;

            var result = 
                from i in Result.Success<int, string>(input)
                select i + 1;

            result.IsSuccess
                .Should().BeTrue();
            result.Success
                .Should().Be(input + 1);
            Assert.Throws<InvalidOperationException>(() => result.Failure);
        }
        [Fact]

        public void Result_ShouldBeFailure()
        {
            const int input = 1;
            const string failure = "failure";

            var result = Result.Success<int, string>(input)
                .SelectSafe(i => Result.Failure<int, string>(failure));

            result.IsSuccess
                .Should().BeFalse();
            result.Failure
                .Should().Be(failure);
            Assert.Throws<InvalidOperationException>(() => result.Success);
        }
    }
}