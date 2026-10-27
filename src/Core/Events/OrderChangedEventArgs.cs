namespace Core.Events;

// Дані події: що сталося з яким замовленням і який у нього стан ПІСЛЯ зміни.
// Це знімок значень: сам Order назовні не віддаємо,
// щоб підписник не міг змінити агрегат в обхід сервісу.
public sealed class OrderChangedEventArgs(
    string orderId,
    string customerId,
    OrderChangeKind kind,
    int linesCount,
    decimal total,
    decimal delta,
    DateTimeOffset at) : EventArgs
{
    public string OrderId { get; } = orderId;
    public string CustomerId { get; } = customerId;
    public OrderChangeKind Kind { get; } = kind;
    public int LinesCount { get; } = linesCount;
    public decimal Total { get; } = total;   // сума замовлення після зміни
    public decimal Delta { get; } = delta;   // на скільки змінилась сума
    public DateTimeOffset At { get; } = at;
}