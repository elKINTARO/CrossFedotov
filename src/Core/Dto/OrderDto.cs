namespace Core.Dto;

// Варіант «Замовлення»: рядок містить productName, quantity, price.
public sealed record OrderLineDto(string ProductId, string ProductName, decimal Price, int Quantity);

public sealed record OrderDto(
    string Id,
    string CustomerId,
    DateTimeOffset CreatedAt,
    string Status,
    IReadOnlyList<OrderLineDto> Lines);