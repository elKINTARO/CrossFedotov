using Core.Reports;

namespace Core.Tests;

public class ReportServiceTests
{
    [Theory] // звіт на кількох порогах. Суми: A 30 000, B 1 000, C 500, D 1 000, E 60 000, F 100
    [InlineData(10_000, 2)]   // A, E
    [InlineData(900, 4)]      // A, B, D, E
    [InlineData(0, 6)]        // усі
    [InlineData(100_000, 0)]  // жодного
    public void ExpensiveOrders_CountsOnlyAboveThreshold(int threshold, int expected) =>
        Assert.Equal(expected, ReportService.ExpensiveOrders(TestData.Orders(), threshold).Count);

    [Fact] // детермінований топ-N: C-2 і C-3 мають однакові 1 000, порядок вирішує id
    public void TopCustomers_IsDeterministic() =>
        Assert.Equal(
            new[] { "C-1", "C-2", "C-3" },
            ReportService.TopCustomers(TestData.Orders(), 3).Select(c => c.CustomerId));

    [Fact] // той самий топ на перевернутому джерелі — порядок не залежить від джерела
    public void TopCustomers_DoesNotDependOnSourceOrder() =>
        Assert.Equal<CustomerTotal>(
            ReportService.TopCustomers(TestData.Orders(), 3),
            ReportService.TopCustomers(TestData.Orders().Reverse(), 3));

    [Fact] // скасовані не рахуються у виручку
    public void TopCustomers_IgnoresCancelledOrders()
    {
        CustomerTotal c2 = ReportService.TopCustomers(TestData.Orders(), 5).Single(c => c.CustomerId == "C-2");

        Assert.Equal(new CustomerTotal("C-2", 1, 1_000m), c2);
    }

    [Fact]
    public void Totals_CountsConfirmedOnly()
    {
        var t = ReportService.Totals(TestData.Orders());

        Assert.Equal(4, t.Orders);
        Assert.Equal(32_500m, t.Revenue);
        Assert.Equal(8_125m, t.AverageCheck);
        Assert.Equal(30_000m, t.MaxOrder);
    }

    [Fact] // порожнє джерело не кидає виняток
    public void Totals_OnEmptySource_ReturnsZeros() =>
        Assert.Equal((0, 0m, 0m, 0m), ReportService.Totals([]));

    [Fact] // GroupBy за статусом: три групи
    public void ByStatus_GroupsAllOrders()
    {
        IReadOnlyList<GroupSummary> groups = ReportService.ByStatus(TestData.Orders());

        Assert.Equal(3, groups.Count);
        Assert.Equal(6, groups.Sum(g => g.Count));
        Assert.Equal(new GroupSummary("Cancelled", 1, 60_000m), groups[0]);
    }

    [Fact] // синтаксис запитів дає той самий результат, що й методний
    public void ByStatusQuery_MatchesMethodSyntax() =>
        Assert.Equal<GroupSummary>(ReportService.ByStatus(TestData.Orders()), ReportService.ByStatusQuery(TestData.Orders()));
}