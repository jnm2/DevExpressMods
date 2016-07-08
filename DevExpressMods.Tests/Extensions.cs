using System;
using System.Collections.Generic;

namespace DevExpressMods.Tests
{
    internal static class Extensions
    {
        public static IEnumerable<T> SelectManyRecursive<T>(this IEnumerable<T> startingCollection, Func<T, IEnumerable<T>> recurseSelector)
        {
            var stack = new Stack<IEnumerator<T>>();
            var currentEnumerator = startingCollection.GetEnumerator();

            while (true)
            {
                if (currentEnumerator.MoveNext())
                {
                    yield return currentEnumerator.Current;
                    var innerEnumerator = recurseSelector.Invoke(currentEnumerator.Current);
                    if (innerEnumerator != null)
                    {
                        stack.Push(currentEnumerator);
                        currentEnumerator = innerEnumerator.GetEnumerator();
                    }
                }
                else
                {
                    currentEnumerator.Dispose();
                    if (stack.Count == 0) yield break;
                    currentEnumerator = stack.Pop();
                }
            }
        }
    }
}
