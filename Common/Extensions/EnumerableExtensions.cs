namespace MES.Common.Extensions
{
    public static class EnumerableExtensions
    {
        public static bool HasDuplicates<T>(this IEnumerable<T> list, out IEnumerable<T> duplicates)
        {
            duplicates = list.GroupBy(x => x)
                             .Where(g => g.Count() > 1)
                             .Select(g => g.Key);

            return duplicates.Any();
        }
    }
}
