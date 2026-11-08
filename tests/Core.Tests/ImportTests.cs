using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;

namespace Core.Tests;

public class ImportTests
{
    private const string ValidJson = """
        [
          { "id": "IMP-1", "customerId": "C-1", "createdAt": "2026-01-15T12:00:00+00:00", "status": "Confirmed",
            "lines": [ { "productId": "P-1", "name": "Кабель", "price": 100, "quantity": 3 } ] },
          { "id": "IMP-2", "customerId": "C-2", "createdAt": "2026-01-15T12:00:00+00:00", "status": "Draft",
            "lines": [] }
        ]
        """;

    // Другий запис зіпсований: кількість 0.
    private const string BrokenJson = """
        [
          { "id": "BAD-1", "customerId": "C-1", "createdAt": "2026-01-15T12:00:00+00:00", "status": "Draft",
            "lines": [ { "productId": "P-1", "name": "Кабель", "price": 100, "quantity": 1 } ] },
          { "id": "BAD-2", "customerId": "C-2", "createdAt": "2026-01-15T12:00:00+00:00", "status": "Draft",
            "lines": [ { "productId": "P-2", "name": "Миша", "price": 250, "quantity": 0 } ] }
        ]
        """;

    [Fact] // скасування: вже скасований токен — виняток до будь-якої роботи з диском
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonOrderImporter(), "any.json", ct: cts.Token));
    }

    [Fact] // успішний імпорт з тимчасового файлу + події + ідемпотентність
    public async Task ImportAsync_FromTempFile_AddsOrders_AndIsIdempotent()
    {
        string path = await TempJson.WriteAsync(ValidJson);
        try
        {
            var store = new InMemoryOrderStore();
            var service = new OrderService(store);
            var events = new List<OrderChangedEventArgs>();
            service.Changed += (_, e) => events.Add(e);

            int first = await service.ImportAsync(new JsonOrderImporter(), path);
            int second = await service.ImportAsync(new JsonOrderImporter(), path);

            Assert.Equal(2, first);
            Assert.Equal(0, second);                       // повторний імпорт нічого не дублює
            Assert.Equal(2, events.Count);
            Assert.All(events, e => Assert.Equal(OrderChangeKind.OrderImported, e.Kind));
            Assert.Equal(300m, store.GetById("IMP-1")!.Total);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // зіпсований запис: DomainException з номером, у сховище не потрапило нічого
    public async Task ImportAsync_WithBrokenRecord_ThrowsDomainException_AndAddsNothing()
    {
        string path = await TempJson.WriteAsync(BrokenJson);
        try
        {
            var store = new InMemoryOrderStore();
            var service = new OrderService(store);

            var ex = await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonOrderImporter(), path));

            Assert.Contains("Запис 2", ex.Message);
            Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException); // початкова причина збережена
            Assert.Empty(store.List());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // [дод 1] потоковий імпорт не атомарний: перший запис уже додано
    public async Task ImportStreamAsync_WithBrokenRecord_KeepsRecordsBeforeIt()
    {
        string path = await TempJson.WriteAsync(BrokenJson);
        try
        {
            var store = new InMemoryOrderStore();
            var service = new OrderService(store);

            await Assert.ThrowsAsync<DomainException>(
                () => service.ImportStreamAsync(new JsonOrderImporter(), path));

            Order kept = Assert.Single(store.List());
            Assert.Equal("BAD-1", kept.Id);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // [дод 1 + дод 3] потоковий імпорт додає все і звітує хід до 100%
    public async Task ImportStreamAsync_ReportsProgressUpTo100()
    {
        string path = await TempJson.WriteAsync(ValidJson);
        try
        {
            var service = new OrderService(new InMemoryOrderStore());
            var progress = new ListProgress();

            int added = await service.ImportStreamAsync(new JsonOrderImporter(), path, progress);

            Assert.Equal(2, added);
            Assert.Equal(100, progress.Values[^1]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact] // [дод 2] зв'язані токени: спрацював саме тайм-аут
    public async Task ImportWithTimeout_WhenImporterHangs_ThrowsTimeoutException()
    {
        var service = new OrderService(new InMemoryOrderStore());

        await Assert.ThrowsAsync<TimeoutException>(
            () => service.ImportWithTimeoutAsync(new HangingImporter(), "x.json", TimeSpan.FromMilliseconds(100)));
    }

    [Fact] // [дод 2] скасував користувач — це не тайм-аут
    public async Task ImportWithTimeout_WhenUserCancels_ThrowsOperationCanceled()
    {
        using var user = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var service = new OrderService(new InMemoryOrderStore());

        Exception ex = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportWithTimeoutAsync(new HangingImporter(), "x.json", TimeSpan.FromSeconds(30), ct: user.Token));

        Assert.IsNotType<TimeoutException>(ex);
    }
}