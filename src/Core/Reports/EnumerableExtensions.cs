namespace Core.Reports;

// [дод 2] Власний метод-розширення. Використовується у двох звітах: TopCustomers і TopProducts.
public static class EnumerableExtensions
{
    // Перші n за спаданням ключа. Другий ключ (унікальний рядок) робить порядок стабільним
    // між запусками, коли основні ключі рівні.
    public static IEnumerable<T> TopBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> key,
        Func<T, string> tieBreaker,
        int n)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(n);

        return source.OrderByDescending(key)
                     .ThenBy(tieBreaker, StringComparer.Ordinal)
                     .Take(n);
    }
}