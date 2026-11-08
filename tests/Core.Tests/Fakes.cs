using System.Runtime.CompilerServices;
using Core.Abstractions;
using Core.Domain;

namespace Core.Tests;

// Годинник, що завжди показує один і той самий момент: час у тестах відтворюваний.
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

// Імпортер, що «висить», доки його не скасують: потрібен для тестів тайм-ауту.
internal sealed class HangingImporter : IOrderImporter
{
    public async Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default)
    {
        await Task.Delay(Timeout.Infinite, ct);
        return [];
    }

    public async IAsyncEnumerable<Order> StreamAsync(
        string path, IProgress<int>? progress = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Delay(Timeout.Infinite, ct);
        yield break;
    }
}

// Синхронний IProgress: просто складає значення в список (Progress<T> викликав би обробник пізніше).
internal sealed class ListProgress : IProgress<int>
{
    public List<int> Values { get; } = [];

    public void Report(int value) => Values.Add(value);
}

// Тимчасовий файл з унікальним іменем: тести не заважають один одному і не чіпають data/.
internal static class TempJson
{
    public static async Task<string> WriteAsync(string json)
    {
        string path = Path.Combine(Path.GetTempPath(), $"crossapp-test-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        return path;
    }
}