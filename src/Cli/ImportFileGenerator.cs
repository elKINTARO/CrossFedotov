using System.Text.Json;
using Core;
using Core.Domain;
using Core.Dto;

namespace Cli;

// Генерує великий файл для демонстрації скасування і тайм-ауту.
internal static class ImportFileGenerator
{
    public static async Task WriteAsync(string path, int count, CancellationToken ct = default)
    {
        IReadOnlyList<Order> orders = SampleData.Generate(count);
        List<OrderDto> dtos = orders.Select(o => o.ToDto()).ToList();

        string? directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (directory is not null)
            Directory.CreateDirectory(directory);

        await using var stream = new FileStream(
            path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await JsonSerializer.SerializeAsync(stream, dtos, cancellationToken: ct);
    }
}