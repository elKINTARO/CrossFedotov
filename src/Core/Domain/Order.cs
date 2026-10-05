using Core.Dto;

namespace Core.Domain;

// Агрегат: Order володіє своїми рядками і є єдиною точкою входу для їх зміни.
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public string Id { get; }
    public string CustomerId { get; }
    public DateTimeOffset CreatedAt { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;

    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();
    public decimal Total => _lines.Sum(l => l.Sum);

    private Order(string id, string customerId, DateTimeOffset createdAt)
    {
        Id = id;
        CustomerId = customerId;
        CreatedAt = createdAt;
    }

    // Дата приходить ззовні, а не з DateTimeOffset.Now: інакше поведінку
    // неможливо перевірити тестами тижня 8.
    public static Order Create(string id, string customerId, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Номер замовлення обов'язковий", nameof(id));
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Замовлення без клієнта неможливе", nameof(customerId));

        return new Order(id.Trim().ToUpperInvariant(), customerId.Trim().ToUpperInvariant(), createdAt);
    }

    public void AddLine(string productId, string name, decimal price, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException(
                $"Замовлення {Id} у статусі {Status}, рядки додавати не можна");

        // Перевірки рядка — у самому OrderLine, щоб правило жило в одному місці.
        OrderLine line = OrderLine.Create(productId, name, price, quantity);
        _lines.Add(line);
    }

    public void AddLine(Product product, int quantity) =>
        AddLine(product.Id, product.Name, product.Price, quantity);

    public void Confirm()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException(
                $"Замовлення {Id} порожнє, підтвердити його не можна");

        TransitionTo(OrderStatus.Confirmed);
    }

    public void Cancel() => TransitionTo(OrderStatus.Cancelled);

    // [дод 3] Таблиця допустимих переходів через switch expression над кортежем.
    private void TransitionTo(OrderStatus next)
    {
        bool allowed = (Status, next) switch
        {
            (OrderStatus.Draft, OrderStatus.Confirmed) => true,
            (OrderStatus.Draft, OrderStatus.Cancelled) => true,
            (OrderStatus.Confirmed, OrderStatus.Cancelled) => true,
            _ => false
        };

        if (!allowed)
            throw new InvalidOperationException(
                $"Замовлення {Id}: перехід {Status} → {next} неможливий");

        Status = next;
    }

    public OrderDto ToDto() => new(
        Id,
        CustomerId,
        CreatedAt,
        Status.ToString(),
        _lines.Select(l => new OrderLineDto(l.ProductId, l.Name, l.Price, l.Quantity)).ToList());

    public static Order FromDto(OrderDto dto)
    {
        if (!Enum.TryParse(dto.Status, ignoreCase: true, out OrderStatus status) || !Enum.IsDefined(status))
            throw new ArgumentException($"Невідомий статус '{dto.Status}'", nameof(dto));

        Order order = Create(dto.Id, dto.CustomerId, dto.CreatedAt);

        // Рядки додаються у чернетці й проходять ті самі інваріанти, що й уперше,
        // і лише потім відновлюється статус.
        foreach (OrderLineDto line in dto.Lines)
            order.AddLine(line.ProductId, line.Name, line.Price, line.Quantity);

        switch (status)
        {
            case OrderStatus.Confirmed:
                order.Confirm();
                break;
            case OrderStatus.Cancelled:
                order.Cancel();
                break;
        }

        return order;
    }

    public override string ToString() =>
        $"{Id} [{Status}] клієнт {CustomerId}, рядків {_lines.Count}, сума {Total:F2}";
}