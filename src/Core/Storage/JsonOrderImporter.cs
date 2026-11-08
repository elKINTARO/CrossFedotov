using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Імпорт замовлень (разом із рядками) з JSON-масиву OrderDto.
// Справжній асинхронний ввід-вивід: FileStream з useAsync: true, без Task.Run.
public sealed class JsonOrderImporter : IOrderImporter
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested(); // скасований токен — до будь-якої роботи з диском

        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

        List<OrderDto> dtos = await JsonSerializer.DeserializeAsync<List<OrderDto>>(stream, Options, ct)
            ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку замовлень.");

        var result = new List<Order>(dtos.Count);
        for (int i = 0; i < dtos.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            result.Add(ToOrder(dtos[i], i + 1));
        }

        return result;
    }

    // [дод 1] IAsyncEnumerable: читання і розбір ідуть порціями, замовлення віддаються одразу.
    // [EnumeratorCancellation] дозволяє передати токен і через WithCancellation(...) у await foreach.
    public async IAsyncEnumerable<Order> StreamAsync(
        string path,
        IProgress<int>? progress = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        await using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

        int index = 0;
        await foreach (OrderDto? dto in JsonSerializer.DeserializeAsyncEnumerable<OrderDto>(stream, Options, ct))
        {
            ct.ThrowIfCancellationRequested();
            index++;

            yield return ToOrder(dto, index);

            // [дод 3] Хід імпорту — частка вже прочитаних байтів файлу.
            progress?.Report((int)(stream.Position * 100 / Math.Max(1, stream.Length)));
        }

        progress?.Report(100);
    }

    // Погані дані — бізнес-помилка: загортаємо в DomainException з номером запису.
    // Початковий виняток зберігається як InnerException, щоб не втратити деталі.
    private static Order ToOrder(OrderDto? dto, int number)
    {
        if (dto is null)
            throw new DomainException($"Запис {number}: порожній елемент масиву.");
        if (dto.Lines is null)
            throw new DomainException($"Запис {number}: відсутнє поле lines.");

        try
        {
            return Order.FromDto(dto);
        }
        catch (Exception ex) when (ex is ArgumentException or DomainException)
        {
            throw new DomainException($"Запис {number} ({dto.Id}): {ex.Message}", ex);
        }
    }
}