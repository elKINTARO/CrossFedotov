using Core.Domain;

namespace Core.Abstractions;

// Асинхронний контракт імпорту замовлень. Сервіс отримує імпортер параметром (ручний DI тижня 5),
// тому в тестах замість файлового можна передати фейковий.
public interface IOrderImporter
{
    // Читає весь файл і повертає замовлення лише якщо ВСІ записи коректні.
    Task<IReadOnlyList<Order>> LoadAsync(string path, CancellationToken ct = default);

    // [дод 1] Потоковий варіант: віддає замовлення по одному, не чекаючи кінця файлу.
    // progress — відсоток прочитаних байтів файлу [дод 3].
    IAsyncEnumerable<Order> StreamAsync(string path, IProgress<int>? progress = null, CancellationToken ct = default);
}