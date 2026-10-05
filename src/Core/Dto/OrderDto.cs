namespace Core.Dto;

public sealed record OrderLineDto(string ProductId, string Name, decimal Price, int Quantity);

public sealed record OrderDto(
    string Id,
    string CustomerId,
    DateTimeOffset CreatedAt,
    string Status,
    IReadOnlyList<OrderLineDto> Lines);