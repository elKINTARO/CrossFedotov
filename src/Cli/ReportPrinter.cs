using Core.Reports;

namespace Cli;

// Лише форматування. Жодної логіки відбору: усі запити в ReportService.
internal static class ReportPrinter
{
    public static void Print(IReadOnlyList<CustomerTotal> rows)
    {
        Console.WriteLine("Топ клієнтів за сумою підтверджених замовлень:");
        if (rows.Count == 0) { Console.WriteLine("  (порожньо)"); return; }

        foreach (CustomerTotal r in rows)
            Console.WriteLine($"  {r.CustomerId,-6}  замовлень: {r.Orders,3}   сума: {r.Revenue,12:N2}");
    }

    public static void Print(IReadOnlyList<OrderSummary> rows)
    {
        Console.WriteLine("Замовлення, дорожчі за поріг:");
        if (rows.Count == 0) { Console.WriteLine("  (порожньо)"); return; }

        foreach (OrderSummary r in rows)
            Console.WriteLine($"  {r.Id,-10} {r.CustomerId,-6} {r.Status,-10} {r.CreatedAt:yyyy-MM-dd}  рядків: {r.Lines,2}  сума: {r.Total,12:N2}");
    }

    public static void Print(string title, IReadOnlyList<GroupSummary> rows)
    {
        Console.WriteLine($"{title}:");
        if (rows.Count == 0) { Console.WriteLine("  (порожньо)"); return; }

        foreach (GroupSummary g in rows)
            Console.WriteLine($"  {g.Key,-10}  замовлень: {g.Count,3}   сума: {g.TotalAmount,12:N2}");
    }

    public static void Print((int Orders, decimal Revenue, decimal AverageCheck, decimal MaxOrder) t)
    {
        Console.WriteLine("Підсумки по підтверджених замовленнях:");
        Console.WriteLine($"  замовлень {t.Orders}, виручка {t.Revenue:N2}, середній чек {t.AverageCheck:N2}, максимальне {t.MaxOrder:N2}");
    }

    public static void Print(IReadOnlyList<ProductSales> rows)
    {
        Console.WriteLine("Топ товарів за проданою кількістю:");
        if (rows.Count == 0) { Console.WriteLine("  (порожньо)"); return; }

        foreach (ProductSales p in rows)
            Console.WriteLine($"  {p.ProductId,-6} {p.Name,-24} шт: {p.Quantity,4}   сума: {p.Revenue,12:N2}");
    }

    public static void Print(IReadOnlyList<CustomerOrdersLine> rows)
    {
        Console.WriteLine("Клієнти × замовлення (GroupJoin):");
        if (rows.Count == 0) { Console.WriteLine("  (порожньо)"); return; }

        foreach (CustomerOrdersLine c in rows)
            Console.WriteLine($"  {c.CustomerId,-6} {c.FullName,-18} замовлень: {c.Orders,3}   підтверджено на: {c.Revenue,12:N2}");
    }
}