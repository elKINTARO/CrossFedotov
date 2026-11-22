using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Той самий контракт, інша механіка: кеш у пам'яті, дозавантаження при першому
// зверненні, запис на диск після кожної зміни. На диск ідуть DTO, а не сутності:
// формат файлу не диктує форму домену.
// lab09: шлях приходить ззовні (з конфігурації), запис атомарний, JSON у camelCase.
public sealed class FileOrderStore(string path) : IOrderStore
{
    private readonly Dictionary<string, Order> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    public string FilePath => _path;

    public IReadOnlyList<Order> List()
    {
        EnsureLoaded();
        return _cache.Values.ToList();
    }

    public Order? GetById(string id)
    {
        EnsureLoaded();
        return _cache.GetValueOrDefault(id);
    }

    public void Add(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        EnsureLoaded();
        if (_cache.ContainsKey(order.Id))
            throw new InvalidOperationException($"Замовлення з id={order.Id} уже існує.");

        _cache.Add(order.Id, order);
        Flush();
    }

    public void Update(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        EnsureLoaded();
        if (!_cache.ContainsKey(order.Id))
            throw new InvalidOperationException($"Замовлення з id={order.Id} не знайдено, оновлювати нічого.");

        _cache[order.Id] = order;
        Flush();
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        if (!_cache.Remove(id))
            return false;

        Flush();
        return true;
    }

    private void EnsureLoaded()
    {
        if (_loaded)
            return;

        if (!File.Exists(_path))
        {
            _loaded = true; // файлу ще немає — це не помилка, просто порожнє сховище
            return;
        }

        try
        {
            List<OrderDto> dtos = JsonSerializer.Deserialize<List<OrderDto>>(File.ReadAllText(_path), OrderJson.Options) ?? [];

            _cache.Clear();
            foreach (OrderDto dto in dtos)
            {
                // FromDto проганяє дані через ті самі інваріанти, що й Create.
                Order order = Order.FromDto(dto);
                _cache[order.Id] = order;
            }
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or DomainException or NullReferenceException)
        {
            // Зрозуміле повідомлення з шляхом замість необробленого stack trace.
            _cache.Clear();
            throw new StorageException($"Файл даних {_path} пошкоджено або має старий формат: {ex.Message}", ex);
        }

        _loaded = true;
    }

    private void Flush()
    {
        List<OrderDto> dtos = _cache.Values.Select(o => o.ToDto()).ToList();
        OrderJson.WriteAtomic(_path, dtos);
    }
}