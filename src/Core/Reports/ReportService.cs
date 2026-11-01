using System.Globalization;
using Core.Domain;
using Core.Dto;

namespace Core.Reports;

// Усі запити звітів. Приймають IEnumerable<Order>, а не сховище: це чисті функції,
// їх легко перевірити тестом і викликати з будь-якого джерела (пам'ять, файл, тиждень 11 — БД).
// Повертають IReadOnlyList: результат уже обчислено, повторний Count() запит не перевиконує.
public static class ReportService
{
    // 1) Where + GroupBy + Sum + топ-N. Питання: хто з клієнтів приносить найбільше грошей?
    public static IReadOnlyList<CustomerTotal> TopCustomers(IEnumerable<Order> source, int n) =>
        source.Where(o => o.Status == OrderStatus.Confirmed)
              .GroupBy(o => o.CustomerId, StringComparer.OrdinalIgnoreCase)
              .Select(g => new CustomerTotal(g.Key, g.Count(), g.Sum(o => o.Total)))
              .TopBy(c => c.Revenue, c => c.CustomerId, n)
              .ToList();

    // 2) Where + сортування з двома ключами + Select у DTO. Питання: які замовлення дорожчі за N?
    public static IReadOnlyList<OrderSummary> ExpensiveOrders(IEnumerable<Order> source, decimal threshold) =>
        source.Where(o => o.Total > threshold)              // спочатку фільтр, потім сортування
              .OrderByDescending(o => o.Total)
              .ThenBy(o => o.Id, StringComparer.Ordinal)
              .Select(ToSummary)
              .ToList();

    // 3) GroupBy за статусом. Питання: скільки замовлень і грошей у кожному статусі?
    public static IReadOnlyList<GroupSummary> ByStatus(IEnumerable<Order> source) =>
        source.GroupBy(o => o.Status)
              .Select(g => new GroupSummary(g.Key.ToString(), g.Count(), g.Sum(o => o.Total)))
              .OrderByDescending(s => s.TotalAmount)
              .ThenBy(s => s.Key, StringComparer.Ordinal)
              .ToList();

    // [дод 3] Той самий звіт у синтаксисі запитів. Компілятор перекладає його у ті самі виклики.
    public static IReadOnlyList<GroupSummary> ByStatusQuery(IEnumerable<Order> source) =>
        (from order in source
         group order by order.Status into g
         let total = g.Sum(o => o.Total)
         orderby total descending, g.Key.ToString()
         select new GroupSummary(g.Key.ToString(), g.Count(), total))
        .ToList();

    // 4) GroupBy за місяцем. Питання: як розподіляються замовлення в часі?
    public static IReadOnlyList<GroupSummary> ByMonth(IEnumerable<Order> source) =>
        source.GroupBy(o => o.CreatedAt.ToString("yyyy-MM", CultureInfo.InvariantCulture))
              .Select(g => new GroupSummary(g.Key, g.Count(), g.Sum(o => o.Total)))
              .OrderBy(s => s.Key, StringComparer.Ordinal)
              .ToList();

    // 5) Агрегати. Питання: скільки підтверджених, виручка, середній чек, найбільше замовлення?
    // Average і Max на порожній послідовності кидають виняток, тому порожнечу перевіряємо до них.
    public static (int Orders, decimal Revenue, decimal AverageCheck, decimal MaxOrder) Totals(IEnumerable<Order> source)
    {
        List<decimal> totals = source.Where(o => o.Status == OrderStatus.Confirmed)
                                     .Select(o => o.Total)
                                     .ToList();

        return totals.Count == 0
            ? (0, 0m, 0m, 0m)
            : (totals.Count, totals.Sum(), totals.Average(), totals.Max());
    }

    // 6) SelectMany + GroupBy + топ-N. Питання: які товари продаються найбільше?
    public static IReadOnlyList<ProductSales> TopProducts(IEnumerable<Order> source, int n) =>
        source.Where(o => o.Status == OrderStatus.Confirmed)
              .SelectMany(o => o.Lines)                     // усі рядки всіх замовлень в одну послідовність
              .GroupBy(l => l.ProductId, StringComparer.OrdinalIgnoreCase)
              .Select(g => new ProductSales(g.Key, g.First().Name, g.Sum(l => l.Quantity), g.Sum(l => l.Sum)))
              .TopBy(p => p.Quantity, p => p.ProductId, n)
              .ToList();

    // [дод 1] GroupJoin двох колекцій: довідник клієнтів × замовлення.
    // На відміну від Join, клієнт без жодного замовлення теж потрапляє в результат (з нулями).
    public static IReadOnlyList<CustomerOrdersLine> CustomersWithOrders(
        IEnumerable<CustomerDto> customers, IEnumerable<Order> orders) =>
        customers.GroupJoin(
                     orders,
                     customer => customer.Id,
                     order => order.CustomerId,
                     (customer, its) => new CustomerOrdersLine(
                         customer.Id,
                         customer.FullName,
                         its.Count(),
                         its.Where(o => o.Status == OrderStatus.Confirmed).Sum(o => o.Total)),
                     StringComparer.OrdinalIgnoreCase)
                 .OrderByDescending(l => l.Revenue)
                 .ThenBy(l => l.CustomerId, StringComparer.Ordinal)
                 .ToList();

    internal static OrderSummary ToSummary(Order o) =>
        new(o.Id, o.CustomerId, o.Status.ToString(), o.Lines.Count, o.Total, o.CreatedAt);
}