using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;
using FluentAssertions;
using Xunit;

namespace TplDataFlow.Extensions.UnitTests
{
    public class DataFlowBlockExtensionsTests
    {
        //[Fact]
        //public void TestCombine()
        //{
        //    var items = new[] { 1, 2, 3 };

        //    var sut = new TransformBlock<int, int>(i => i)
        //        .CombineWith(new BufferBlock<int>());

        //    items.ToObservable()
        //        .Subscribe(sut.AsObserver());

        //    IList<int> output = sut
        //        .AsObservable()
        //        .ToEnumerable()
        //        .ToList();

        //    output.Should().BeEquivalentTo(items);
        //}

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

        //[Fact]
        //public void TestSafeTransformSuccess()
        //{
        //    var input = new[] { 1, 2, 3, 4, 5 };

        //    var target = new BufferBlock<int>();
        //    var targetOnException = new BufferBlock<Tuple<Exception, int>>();

        //    var sut = new TransformSafeBlock<int, int>(i => i)
        //                .HandleExceptionWith(targetOnException)
        //                .LinkWith(target);

        //    input.ToObservable()
        //        .Subscribe(sut.AsObserver());

        //    IList<int> output = target
        //        .AsObservable()
        //        .ToEnumerable()
        //        .ToList();

        //    IList<Tuple<Exception, int>> outputExceptions = targetOnException
        //        .AsObservable()
        //        .ToEnumerable()
        //        .ToList();

        //    output.Should()
        //        .BeEquivalentTo(input);
        //    outputExceptions.Should()
        //        .BeEmpty();
        //}

        //[Fact]
        //public void TestSafeTransformWithErrors()
        //{
        //    var input = new[] { -2, -1, 0, 1, 2 };

        //    var target = new BufferBlock<int>();
        //    var targetException = new BufferBlock<Tuple<Exception, int>>();

        //    var sut = new TransformSafeBlock<int, int>(i =>
        //                    {
        //                        if (i < 0)
        //                        {
        //                            throw new ArgumentException();
        //                        }
        //                        return i;
        //                    })
        //                .HandleExceptionWith(targetException)
        //                .LinkWith(target);

        //    input.ToObservable()
        //        .Subscribe(sut.AsObserver());

        //    IList<int> output = target
        //        .AsObservable()
        //        .ToEnumerable()
        //        .ToList();

        //    IList<Tuple<Exception, int>> outputExceptions = targetException
        //        .AsObservable()
        //        .ToEnumerable()
        //        .ToList();

        //    output.Should()
        //        .BeEquivalentTo(input.Where(i => i >= 0));

        //    outputExceptions
        //        .Select(item => item.Item2)
        //        .Should()
        //        .BeEquivalentTo(input.Where(i => i < 0));
        //}

        //[Fact]
        //public void TestSafeTransformManySuccess()
        //{
        //    var input = new[] { 1, 2, 3, 4, 5 };

        //    var target = new BufferBlock<int>();
        //    var targetOnException = new BufferBlock<Tuple<Exception, int>>();

        //    var sut = new TransformManySafeBlock<int, int>(i => new[] { i })
        //                .HandleExceptionWith(targetOnException)
        //                .LinkWith(target);

        //    input.ToObservable()
        //        .Subscribe(sut.AsObserver());

        //    IList<int> output = target
        //        .AsObservable()
        //        .ToEnumerable()
        //        .ToList();

        //    IList<Tuple<Exception, int>> outputExceptions = targetOnException
        //        .AsObservable()
        //        .ToEnumerable()
        //        .ToList();

        //    output.Should()
        //        .BeEquivalentTo(input);
        //    outputExceptions.Should()
        //        .BeEmpty();
        //}

        //[Fact]
        //public void TestSafeTransformManyWithErrors()
        //{
        //    var input = new[] { -2, -1, 0, 1, 2 };

        //    var target = new BufferBlock<int>();
        //    var targetException = new BufferBlock<Tuple<Exception, int>>();

        //    var sut = new TransformManySafeBlock<int, int>(i =>
        //    {
        //        if (i < 0)
        //        {
        //            throw new ArgumentException();
        //        }
        //        return new[] { i };
        //    })
        //                .HandleExceptionWith(targetException)
        //                .LinkWith(target);

        //    input.ToObservable()
        //        .Subscribe(sut.AsObserver());

        //    IList<int> output = target
        //        .AsObservable()
        //        .ToEnumerable()
        //        .ToList();

        //    IList<Tuple<Exception, int>> outputExceptions = targetException
        //        .AsObservable()
        //        .ToEnumerable()
        //        .ToList();

        //    output.Should()
        //        .BeEquivalentTo(input.Where(i => i >= 0));

        //    outputExceptions
        //        .Select(item => item.Item2)
        //        .Should()
        //        .BeEquivalentTo(input.Where(i => i < 0));
        //}

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