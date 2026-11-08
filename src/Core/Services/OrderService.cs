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

        // ===== lab08: асинхронний імпорт =====

    // Імпорт усього файлу. Імпортер приходить параметром (ручний DI), токен передається далі.
    // Файл спершу перевіряється повністю: якщо хоч один запис поганий, у сховище не потрапить нічого.
    public async Task<int> ImportAsync(
        IOrderImporter importer,
        string path,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<Order> items = await importer.LoadAsync(path, ct);

        int added = 0;
        for (int i = 0; i < items.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            if (TryAddImported(items[i]))
                added++;

            progress?.Report((i + 1) * 100 / items.Count); // [дод 3]
        }

        return added;
    }

    // [дод 1] Потоковий імпорт: await foreach по IAsyncEnumerable.
    // Не атомарний: записи до зіпсованого вже додані — це ціна потокової обробки.
    public async Task<int> ImportStreamAsync(
        IOrderImporter importer,
        string path,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(importer);

        int added = 0;
        await foreach (Order order in importer.StreamAsync(path, progress, ct))
        {
            if (TryAddImported(order))
                added++;
        }

        return added;
    }

    // [дод 2] Зв'язані токени: скасування користувача АБО тайм-аут.
    // Якщо спрацював саме тайм-аут — кидаємо TimeoutException, щоб викликач розрізнив причини.
    public async Task<int> ImportWithTimeoutAsync(
        IOrderImporter importer,
        string path,
        TimeSpan timeout,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            return await ImportAsync(importer, path, progress, linked.Token);
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Імпорт не завершився за {timeout.TotalSeconds:F1} с.", ex);
        }
    }

    // Ідемпотентність: повторний імпорт того самого файлу нічого не дублює.
    private bool TryAddImported(Order order)
    {
        if (_store.GetById(order.Id) is not null)
            return false;

        _store.Add(order);
        OnChanged(order, OrderChangeKind.OrderImported, order.Total);
        return true;
    }

    private Order Require(string id) =>
        _store.GetById(id) ?? throw new DomainException($"Немає замовлення з id={id}.");
}