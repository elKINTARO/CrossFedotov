using System.Diagnostics;
using Core;
using Core.Domain;
using Core.Reports;

namespace Cli;

// [дод 4] Замір часу звітів на великому згенерованому наборі.
internal static class BenchDemo
{
    private const decimal Threshold = 30_000m;

    public static void Run(int count)
    {
        Stopwatch sw = Stopwatch.StartNew();
        IReadOnlyList<Order> data = SampleData.Generate(count);
        Console.WriteLine($"Згенеровано {data.Count:N0} замовлень за {sw.ElapsedMilliseconds} мс");

        // Прогрів: перший виклик включає JIT-компіляцію і спотворив би замір.
        _ = ReportVariants.TopExpensiveStreaming(data, Threshold, 10);
        _ = ReportVariants.TopExpensiveWithMiddleToList(data, Threshold, 10);

        Console.WriteLine();
        Console.WriteLine("Найкращий час з 5 повторів:");
        Measure("A. без ToList посередині", () => ReportVariants.TopExpensiveStreaming(data, Threshold, 10).Count);
        Measure("B. з ToList посередині", () => ReportVariants.TopExpensiveWithMiddleToList(data, Threshold, 10).Count);
        Measure("C. відкладений запит двічі", () => ReportVariants.CountAndListDeferred(data, Threshold));
        Measure("D. матеріалізовано один раз", () => ReportVariants.CountAndListMaterialized(data, Threshold));
        Measure("ReportService.ByStatus", () => ReportService.ByStatus(data).Count);
        Measure("ReportService.TopCustomers", () => ReportService.TopCustomers(data, 5).Count);
    }

    private static void Measure(string title, Func<int> action, int repeats = 5)
    {
        double best = double.MaxValue;
        int result = 0;

        for (int i = 0; i < repeats; i++)
        {
            Stopwatch sw = Stopwatch.StartNew();
            result = action();
            sw.Stop();
            best = Math.Min(best, sw.Elapsed.TotalMilliseconds);
        }

        Console.WriteLine($"  {title,-32} {best,8:F2} мс   (результат: {result})");
    }
}