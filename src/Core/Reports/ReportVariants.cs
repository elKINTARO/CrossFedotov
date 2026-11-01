using Core.Domain;

namespace Core.Reports;

// [дод 4] Однаковий за результатом звіт, записаний по-різному, — для заміру Stopwatch.
public static class ReportVariants
{
    // A. Один ланцюжок без проміжного ToList: Where віддає елементи далі по одному.
    public static IReadOnlyList<OrderSummary> TopExpensiveStreaming(IEnumerable<Order> source, decimal threshold, int n) =>
        source.Where(o => o.Total > threshold)
              .OrderByDescending(o => o.Total)
              .ThenBy(o => o.Id, StringComparer.Ordinal)
              .Take(n)
              .Select(ReportService.ToSummary)
              .ToList();

    // B. Те саме, але з ToList посередині: створюється проміжний список у пам'яті.
    public static IReadOnlyList<OrderSummary> TopExpensiveWithMiddleToList(IEnumerable<Order> source, decimal threshold, int n) =>
        source.Where(o => o.Total > threshold)
              .ToList()
              .OrderByDescending(o => o.Total)
              .ThenBy(o => o.Id, StringComparer.Ordinal)
              .Take(n)
              .Select(ReportService.ToSummary)
              .ToList();

    // C. Відкладений запит використано двічі: Where виконується двічі.
    public static int CountAndListDeferred(IEnumerable<Order> source, decimal threshold)
    {
        IEnumerable<Order> query = source.Where(o => o.Total > threshold);
        int count = query.Count();
        List<Order> list = query.ToList();
        return count + list.Count;
    }

    // D. Те саме, але результат матеріалізовано один раз.
    public static int CountAndListMaterialized(IEnumerable<Order> source, decimal threshold)
    {
        List<Order> list = source.Where(o => o.Total > threshold).ToList();
        return list.Count + list.Count;
    }
}