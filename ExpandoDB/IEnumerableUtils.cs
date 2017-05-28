using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB
{
    /// <summary>
    /// 
    /// </summary>
    public static class IEnumerableUtils
    {
        /// <summary>
        /// Ins the sets of.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="size">The size.</param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> InSetsOf<T>(this IEnumerable<T> source, int size)
        {
            var toReturn = new List<T>(size);
            foreach (var item in source)
            {
                toReturn.Add(item);
                if (toReturn.Count == size)
                {
                    yield return toReturn;
                    toReturn = new List<T>(size);
                }
            }
            if (toReturn.Any())
            {
                yield return toReturn;
            }
        }
    }
}
