namespace Core.Domain;

// [дод 2] Інваріант, що охоплює ДВІ сутності: клієнта і всі його замовлення.
// Жоден окремий Order не бачить інших замовлень, тому правило не може жити
// всередині сутності. З лабораторної 5 його викликає OrderService.CreateOrder:
// лише сервіс має доступ до сховища і бачить усі замовлення клієнта.
public static class CustomerOrderPolicy
{
    public const int MaxDraftOrders = 3;

    public static void EnsureCanOpenNewOrder(string customerId, IReadOnlyList<Order> existing)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ArgumentException("Ідентифікатор клієнта обов'язковий", nameof(customerId));

        int drafts = existing.Count(o =>
            o.CustomerId == customerId.Trim().ToUpperInvariant() && o.Status == OrderStatus.Draft);

        if (drafts >= MaxDraftOrders)
            throw new InvalidOperationException(
                $"Клієнт {customerId} уже має {drafts} незавершених замовлень (ліміт {MaxDraftOrders})");
    }
}