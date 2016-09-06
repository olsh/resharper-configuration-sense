using System.Collections.Generic;

namespace Resharper.ConfigurationSense.Extensions
{
    internal static class CollectionExtensions
    {
        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> range)
        {
            foreach (var item in range)
            {
                queue.Enqueue(item);
            }
        }
    }
}
