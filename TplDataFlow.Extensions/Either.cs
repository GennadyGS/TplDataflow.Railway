using System;
using System.Collections.Generic;

namespace TplDataFlow.Extensions
{
    /// <summary>
    /// Either monad
    /// </summary>
    public delegate EitherPair<L, R> Either<L, R>();

    public struct EitherPair<L, R>
    {
        public readonly R Right;
        public readonly L Left;
        public readonly bool IsRight;
        public readonly bool IsLeft;

        public EitherPair(R r)
        {
            Right = r;
            Left = default(L);
            IsRight = true;
            IsLeft = false;
        }

        public EitherPair(L l)
        {
            Left = l;
            Right = default(R);
            IsLeft = true;
            IsRight = false;
        }

        public static implicit operator EitherPair<L, R>(L value)
        {
            return new EitherPair<L, R>(value);
        }

        public static implicit operator EitherPair<L, R>(R value)
        {
            return new EitherPair<L, R>(value);
        }
    }

    /// <summary>
    /// Either constructor methods
    /// </summary>
    public class Either
    {
        /// <summary>
        /// Construct an Either Left monad
        /// </summary>
        public static Either<L, R> Left<L, R>(Func<L> left)
        {
            if (left == null) throw new ArgumentNullException("left");
            return () => new EitherPair<L, R>(left());
        }

        /// <summary>
        /// Construct an Either Right monad
        /// </summary>
        public static Either<L, R> Right<L, R>(Func<R> right)
        {
            if (right == null) throw new ArgumentNullException("right");
            return () => new EitherPair<L, R>(right());
        }

        /// <summary>
        /// Construct an either Left or Right
        /// </summary>
        public static Either<L, R> Return<L, R>(Func<EitherPair<L, R>> either)
        {
            if (either == null) throw new ArgumentNullException("either");
            return () => either();
        }

        /// <summary>
        /// Monadic zero
        /// </summary>
        public static Either<L, R> Mempty<L, R>()
        {
            return () => new EitherPair<L, R>(default(R));
        }
    }

    /// <summary>
    /// The Either monad represents values with two possibilities: a value of Left or Right
    /// Either is sometimes used to represent a value which is either correct or an error, 
    /// by convention, 'Left' is used to hold an error value 'Right' is used to hold a 
    /// correct value.
    /// So you can see that Either has a very close relationship to the Error monad.  However,
    /// the Either monad won't capture exceptions.  Either would primarily be used for 
    /// known error values rather than exceptional ones.
    /// Once the Either monad is in the Left state it cancels the monad bind function and 
    /// returns immediately.
    /// </summary>
    public static class EitherExt
    {
        /// <summary>
        /// Returns true if the monad object is in the Right state
        /// </summary>
        public static bool IsRight<L, R>(this Either<L, R> m)
        {
            return m().IsRight;
        }

        /// <summary>
        /// Get the Left value
        /// NOTE: This throws an InvalidOperationException if the object is in the 
        /// Right state
        /// </summary>
        public static bool IsLeft<L, R>(this Either<L, R> m)
        {
            return m().IsLeft;
        }

        /// <summary>
        /// Get the Right value
        /// NOTE: This throws an InvalidOperationException if the object is in the 
        /// Left state
        /// </summary>
        public static R Right<L, R>(this Either<L, R> m)
        {
            var res = m();
            if (res.IsLeft)
                throw new InvalidOperationException("Not in the Right state");
            return res.Right;
        }

        /// <summary>
        /// Get the Left value
        /// NOTE: This throws an InvalidOperationException if the object is in the 
        /// Right state
        /// </summary>
        public static L Left<L, R>(this Either<L, R> m)
        {
            var res = m();
            if (res.IsRight)
                throw new InvalidOperationException("Not in the Left state");
            return res.Left;
        }

        /// <summary>
        /// Pattern matching method for a branching expression
        /// </summary>
        /// <param name="Right">Action to perform if the monad is in the Right state</param>
        /// <param name="Left">Action to perform if the monad is in the Left state</param>
        /// <returns>T</returns>
        public static Func<T> Match<R, L, T>(this Either<L, R> m, Func<R, T> Right, Func<L, T> Left)
        {
            if (Right == null) throw new ArgumentNullException("Right");
            if (Left == null) throw new ArgumentNullException("Left");
            return () =>
            {
                var res = m();
                return res.IsLeft
                    ? Left(res.Left)
                    : Right(res.Right);
            };
        }

        /// <summary>
        /// Pattern matching method for a branching expression
        /// NOTE: This throws an InvalidOperationException if the object is in the 
        /// Left state
        /// </summary>
        /// <param name="right">Action to perform if the monad is in the Right state</param>
        /// <returns>T</returns>
        public static Func<T> MatchRight<R, L, T>(this Either<L, R> m, Func<R, T> right)
        {
            if (right == null) throw new ArgumentNullException("right");
            return () =>
            {
                return right(m.Right());
            };
        }

        /// <summary>
        /// Pattern matching method for a branching expression
        /// NOTE: This throws an InvalidOperationException if the object is in the 
        /// Right state
        /// </summary>
        /// <param name="left">Action to perform if the monad is in the Left state</param>
        /// <returns>T</returns>
        public static Func<T> MatchLeft<R, L, T>(this Either<L, R> m, Func<L, T> left)
        {
            if (left == null) throw new ArgumentNullException("left");

            return () =>
            {
                return left(m.Left());
            };
        }

        /// <summary>
        /// Pattern matching method for a branching expression
        /// Returns the defaultValue if the monad is in the Left state
        /// </summary>
        /// <param name="right">Action to perform if the monad is in the Right state</param>
        /// <returns>T</returns>
        public static Func<T> MatchRight<R, L, T>(this Either<L, R> m, Func<R, T> right, T defaultValue)
        {
            if (right == null) throw new ArgumentNullException("right");

            return () =>
            {
                var res = m();
                if (res.IsLeft)
                    return defaultValue;
                return right(res.Right);
            };
        }

        /// <summary>
        /// Pattern matching method for a branching expression
        /// Returns the defaultValue if the monad is in the Right state
        /// </summary>
        /// <param name="left">Action to perform if the monad is in the Left state</param>
        /// <returns>T</returns>
        public static Func<T> MatchLeft<R, L, T>(this Either<L, R> m, Func<L, T> left, T defaultValue)
        {
            if (left == null) throw new ArgumentNullException("left");

            return () =>
            {
                var res = m();
                if (res.IsRight)
                    return defaultValue;
                return left(res.Left);
            };
        }

        /// <summary>
        /// Pattern matching method for a branching expression
        /// </summary>
        /// <param name="Right">Action to perform if the monad is in the Right state</param>
        /// <param name="Left">Action to perform if the monad is in the Left state</param>
        /// <returns>Unit</returns>
        public static void Match<L, R>(this Either<L, R> m, Action<R> Right, Action<L> Left)
        {
            if (Left == null) throw new ArgumentNullException("Left");
            if (Right == null) throw new ArgumentNullException("Right");

            var res = m();
            if (res.IsLeft)
                Left(res.Left);
            else
                Right(res.Right);
        }

        /// <summary>
        /// Pattern matching method for a branching expression
        /// NOTE: This throws an InvalidOperationException if the object is in the 
        /// Left state
        /// </summary>
        /// <param name="right">Action to perform if the monad is in the Right state</param>
        /// <returns>Unit</returns>
        public static void MatchRight<L, R>(this Either<L, R> m, Action<R> right)
        {
            if (right == null) throw new ArgumentNullException("right");

            right(m.Right());
        }

        /// <summary>
        /// Pattern matching method for a branching expression
        /// NOTE: This throws an InvalidOperationException if the object is in the 
        /// Right state
        /// </summary>
        /// <param name="left">Action to perform if the monad is in the Left state</param>
        /// <returns>Unit</returns>
        public static void MatchLeft<L, R>(this Either<L, R> m, Action<L> left)
        {
            if (left == null) throw new ArgumentNullException("left");

            left(m.Left());
        }

        /// <summary>
        /// Converts the Either to an enumerable of R
        /// </summary>
        /// <returns>
        /// Right: A list with one R in
        /// Left: An empty list
        /// </returns>
        public static IEnumerable<R> AsEnumerable<L, R>(this Either<L, R> self)
        {
            var res = self();
            if (res.IsRight)
                yield return res.Right;
            else
                yield break;
        }

        /// <summary>
        /// Converts the Either to an infinite enumerable
        /// </summary>
        /// <returns>
        /// Just: An infinite list of R
        /// Nothing: An empty list
        /// </returns>
        public static IEnumerable<R> AsEnumerableInfinite<L, R>(this Either<L, R> self)
        {
            var res = self();
            if (res.IsRight)
                while (true) yield return res.Right;
            else
                yield break;
        }

        /// <summary>
        /// Select
        /// </summary>
        public static Either<L, UR> Select<L, TR, UR>(
            this Either<L, TR> self,
            Func<TR, UR> selector)
        {
            if (selector == null) throw new ArgumentNullException("selector");

            return () =>
            {
                var resT = self();
                if (resT.IsLeft)
                    return new EitherPair<L, UR>(resT.Left);

                return new EitherPair<L, UR>(selector(resT.Right));
            };
        }

        /// <summary>
        /// SelectMany
        /// </summary>
        public static Either<L, VR> SelectMany<L, TR, UR, VR>(
            this Either<L, TR> self,
            Func<TR, Either<L, UR>> selector,
            Func<TR, UR, VR> projector)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            if (projector == null) throw new ArgumentNullException("projector");

            return () =>
            {
                var resT = self();

                if (resT.IsLeft)
                    return new EitherPair<L, VR>(resT.Left);

                var resU = selector(resT.Right)();
                if (resU.IsLeft)
                    return new EitherPair<L, VR>(resU.Left);

                return new EitherPair<L, VR>(projector(resT.Right, resU.Right));
            };
        }

        /// <summary>
        /// Memoize the result 
        /// </summary>
        public static Func<EitherPair<L, R>> Memo<L, R>(this Either<L, R> self)
        {
            var res = self();
            return () => res;
        }
    }
}