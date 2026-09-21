namespace Core.Dto;

public sealed record CustomerDto(string Id, string FullName, string Email, string? Phone = null);