using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class OrderServiceTests
{
    // Кожен тест створює власне сховище: спільного стану між тестами немає.
    private static (OrderService Service, InMemoryOrderStore Store) Create()
    {
        var store = new InMemoryOrderStore();
        return (new OrderService(store, new FixedTimeProvider(TestData.At)), store);
    }

    [Fact] // 1) інваріант аргументу: відмова і незмінний стан
    public void AddLine_WithZeroQuantity_ThrowsArgumentException_AndKeepsState()
    {
        var (service, store) = Create();
        Order order = service.CreateOrder("C-1");
        service.AddLine(order.Id, TestData.Cable(), 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.AddLine(order.Id, TestData.Mouse(), 0));

        Order saved = store.GetById(order.Id)!;
        Assert.Single(saved.Lines);
        Assert.Equal(200m, saved.Total);
    }

    [Fact] // 2) бізнес-правило варіанту «Замовлення»: DomainException «вже підтверджено»
    public void Confirm_Twice_ThrowsDomainException()
    {
        var (service, _) = Create();
        Order order = service.CreateOrder("C-1");
        service.AddLine(order.Id, TestData.Cable(), 1);
        service.Confirm(order.Id);

        var ex = Assert.Throws<DomainException>(() => service.Confirm(order.Id));

        Assert.Contains("вже підтверджено", ex.Message);
    }

    [Fact] // 3) бізнес-правило: рядок у підтверджене замовлення, стан не змінився
    public void AddLine_ToConfirmedOrder_ThrowsDomainException_AndKeepsState()
    {
        var (service, store) = Create();
        Order order = service.CreateOrder("C-1");
        service.AddLine(order.Id, TestData.Cable(), 3);
        service.Confirm(order.Id);

        Assert.Throws<DomainException>(() => service.AddLine(order.Id, TestData.Laptop(), 1));

        Order saved = store.GetById(order.Id)!;
        Assert.Single(saved.Lines);
        Assert.Equal(OrderStatus.Confirmed, saved.Status);
    }

    [Fact] // 4) варіант «Замовлення»: сума після додавання рядків
    public void Total_AfterAddingLines_IsSumOfLines()
    {
        var (service, store) = Create();
        Order order = service.CreateOrder("C-1");

        service.AddLine(order.Id, TestData.Cable(), 3);  // 300
        service.AddLine(order.Id, TestData.Mouse(), 2);  // 500

        Assert.Equal(800m, store.GetById(order.Id)!.Total);
    }

    [Theory] // 5) та сама перевірка суми на кількох наборах
    [InlineData(1, 100)]
    [InlineData(5, 500)]
    [InlineData(12, 1200)]
    public void Total_ForSingleLine_IsPriceTimesQuantity(int quantity, int expected)
    {
        var (service, store) = Create();
        Order order = service.CreateOrder("C-1");

        service.AddLine(order.Id, TestData.Cable(), quantity);

        Assert.Equal((decimal)expected, store.GetById(order.Id)!.Total);
    }

    [Fact] // 6) подія тижня 6 як спосіб перевірки: рівно одна на операцію
    public void CreateOrder_RaisesChangedExactlyOnce()
    {
        var (service, _) = Create();
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        Order order = service.CreateOrder("C-1");

        OrderChangedEventArgs e = Assert.Single(events);
        Assert.Equal(OrderChangeKind.OrderCreated, e.Kind);
        Assert.Equal(order.Id, e.OrderId);
    }

    [Fact] // 7) операція, що впала на інваріанті, події не дає
    public void FailedAddLine_RaisesNoEvent()
    {
        var (service, _) = Create();
        Order order = service.CreateOrder("C-1");
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.AddLine(order.Id, TestData.Cable(), 0));

        Assert.Empty(events);
    }

    [Fact] // 8) подія несе дельту і нову суму
    public void AddLine_RaisesLineAdded_WithDeltaAndTotal()
    {
        var (service, _) = Create();
        Order order = service.CreateOrder("C-1");
        service.AddLine(order.Id, TestData.Cable(), 2);
        var events = new List<OrderChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);

        service.AddLine(order.Id, TestData.Mouse(), 1);

        OrderChangedEventArgs e = Assert.Single(events);
        Assert.Equal(OrderChangeKind.LineAdded, e.Kind);
        Assert.Equal(250m, e.Delta);
        Assert.Equal(450m, e.Total);
    }

    [Fact] // 9) правило двох сутностей з тижня 4: ліміт чернеток клієнта
    public void CreateOrder_OverDraftLimit_ThrowsDomainException()
    {
        var (service, _) = Create();
        for (int i = 0; i < CustomerOrderPolicy.MaxDraftOrders; i++)
            service.CreateOrder("C-9");

        Assert.Throws<DomainException>(() => service.CreateOrder("C-9"));
    }

    [Fact] // 10) неіснуючий id — доменна помилка, а не NullReferenceException
    public void Confirm_UnknownId_ThrowsDomainException()
    {
        var (service, _) = Create();

        Assert.Throws<DomainException>(() => service.Confirm("ORD-NOPE"));
    }

    [Fact] // 11) TimeProvider з тижня 5: час створення відтворюваний
    public void CreateOrder_UsesInjectedClock()
    {
        var (service, _) = Create();

        Order order = service.CreateOrder("C-1");

        Assert.Equal(TestData.At, order.CreatedAt);
    }
}