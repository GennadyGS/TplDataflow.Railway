using System;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using Telerik.JustMock;

namespace TplDataflow.Extensions.Example.Test
{
    public static class ReactiveMockUtils
    {
        public static IObserver<T> MockObservable<T>(Expression<Func<IObservable<T>>> expression)
        {
            var subject = new Subject<T>();
            Mock.Arrange(expression).Returns(subject);
            return subject;
        }

        public static IObservable<T> MockObserver<T>(Expression<Func<IObserver<T>>> expression)
        {
            var subject = new Subject<T>();
            Mock.Arrange(expression).Returns(subject);
            return subject;
        }
    }
}