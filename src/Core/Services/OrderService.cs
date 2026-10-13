using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

// Бізнес-операції над замовленнями. Сервіс не знає слів «File» і «Dictionary»:
// він працює лише з контрактом IOrderStore, який отримує через конструктор.
public sealed class OrderService(IOrderStore store, TimeProvider? clock = null)
{
    private readonly IOrderStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public Order CreateOrder(string customerId)
    {
        // Правило двох сутностей з лабораторної 4: сервіс має доступ до сховища,
        // тому саме тут можна порахувати інші замовлення клієнта.
        CustomerOrderPolicy.EnsureCanOpenNewOrder(customerId, _store.List());

        Order order = Order.Create($"ORD-{Guid.NewGuid().ToString("N")[..8]}", customerId, _clock.GetUtcNow());
        _store.Add(order); // інваріанти вже перевірив Order.Create, сховище перевіряє лише унікальність id
        return order;
    }

    public void AddLine(string orderId, Product product, int quantity)
    {
        Order order = Require(orderId);
        order.AddLine(product, quantity); // метод сутності з лабораторної 4
        _store.Update(order);
    }

    public void Confirm(string orderId)
    {
        Order order = Require(orderId);
        order.Confirm();
        _store.Update(order);
    }

    public void Cancel(string orderId)
    {
        Order order = Require(orderId);
        order.Cancel();
        _store.Update(order);
    }

    public IReadOnlyList<Order> All() => _store.List();

    public Order? Find(string id) => _store.GetById(id);

    // [дод 2] Довільний фільтр — підготовка до подій і LINQ-звітів тижнів 6-7.
    // Метод не додано в IOrderStore: контракт сховища не розширюємо «про запас».
    public IReadOnlyList<Order> Where(Func<Order, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return _store.List().Where(predicate).ToList();
    }

    private Order Require(string id) =>
        _store.GetById(id) ?? throw new InvalidOperationException($"Немає замовлення з id={id}.");
}