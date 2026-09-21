namespace Core.Dto;

public sealed record MixedImportResult(
    IReadOnlyList<ProductDto> Products,
    IReadOnlyList<CustomerDto> Customers,
    IReadOnlyList<string> Errors);