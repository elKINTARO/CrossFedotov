namespace Core.Reports;

// Рядки звітів. Record: незмінні й порівнюються за значенням, що зручно для тестів тижня 8.
// Звіти повертають ці типи, а не Order: контракт звіту не залежить від будови домену.

// Клієнт і сума його підтверджених замовлень.
public sealed record CustomerTotal(string CustomerId, int Orders, decimal Revenue);

// Одне замовлення у звіті. Статус рядком: клієнт API не мусить знати наш enum.
public sealed record OrderSummary(
    string Id, string CustomerId, string Status, int Lines, decimal Total, DateTimeOffset CreatedAt);

// Підсумок групи: ключ, кількість, сума.
public sealed record GroupSummary(string Key, int Count, decimal TotalAmount);

// Товар і скільки його продано.
public sealed record ProductSales(string ProductId, string Name, int Quantity, decimal Revenue);

// [дод 1] Клієнт із довідника + його замовлення (результат GroupJoin).
public sealed record CustomerOrdersLine(string CustomerId, string FullName, int Orders, decimal Revenue);