using System;
using FluentAssertions;
using Xunit;

namespace TplDataFlow.Extensions.UnitTests
{
    public class ResultTests
    {
        // TODO: Split unit tests
        [Fact]
        public void Select_ShouldReturnSuccess()
        {
            const int input = 1;

            var result = Result.Success<int, string>(input)
                .Select(i => i + 1);

            result.IsSuccess
                .Should().BeTrue();
            result.Success
                .Should().Be(input + 1);
            Assert.Throws<InvalidOperationException>(() => result.Failure);
        }

        [Fact]
        public void SelectSafe_ShouldReturnFailure()
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

        [Fact]
        public void Select_ShouldWorkWithAnonymousTypes()
        {
            const int input = 1;

            var result = Result.Success<int, string>(input)
                .Select(item => new {Item = input, Next = input + 1});

            result.Success.Next.Should().Be(input + 1);
        }
    }
}