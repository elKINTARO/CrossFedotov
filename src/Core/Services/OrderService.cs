using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

// Бізнес-операції над замовленнями. Сервіс працює лише з контрактом IOrderStore
// і повідомляє про зміни подією Changed. Як обробити повідомлення — вирішує зовнішній код.
public sealed class OrderService(
    IOrderStore store,
    TimeProvider? clock = null,
    IReadOnlyList<INotificationSink>? sinks = null)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly IReadOnlyList<INotificationSink> _sinks = sinks ?? [];

    // Подія: ззовні можна лише підписатися (+=) або відписатися (-=).
    // Викликати її може тільки OrderService.
    public event EventHandler<OrderChangedEventArgs>? Changed;

    // Єдина точка підняття. Викликається лише ПІСЛЯ успішної зміни і збереження.
    private void OnChanged(Order order, OrderChangeKind kind, decimal delta = 0m)
    {
        var args = new OrderChangedEventArgs(
            order.Id, order.CustomerId, kind, order.Lines.Count, order.Total, delta, _clock.GetLocalNow());

        Changed?.Invoke(this, args);

        foreach (INotificationSink sink in _sinks)   // [дод 1]
            sink.Notify(args);
    }

    public Order CreateOrder(string customerId)
    {
        // Правило двох сутностей з лабораторної 4: лише сервіс бачить усі замовлення клієнта.
        CustomerOrderPolicy.EnsureCanOpenNewOrder(customerId, _store.List());

        Order order = Order.Create($"ORD-{Guid.NewGuid().ToString("N")[..8]}", customerId, _clock.GetUtcNow());
        _store.Add(order);
        OnChanged(order, OrderChangeKind.OrderCreated); // після зміни, не до
        return order;
    }

    public void AddLine(string orderId, Product product, int quantity)
    {
        Order order = Require(orderId);
        decimal before = order.Total;
        order.AddLine(product, quantity); // кине виняток на інваріанті → до OnChanged не дійде
        _store.Update(order);
        OnChanged(order, OrderChangeKind.LineAdded, order.Total - before);
    }

    public void Confirm(string orderId)
    {
        Order order = Require(orderId);
        order.Confirm();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderConfirmed);
    }

    public void Cancel(string orderId)
    {
        Order order = Require(orderId);
        order.Cancel();
        _store.Update(order);
        OnChanged(order, OrderChangeKind.OrderCancelled);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    // Func, а не подія: тут потрібне значення у відповідь, критерій задає викликач.
    public IReadOnlyList<Order> FindAll(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    private Order Require(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає замовлення з id={id}.");
}