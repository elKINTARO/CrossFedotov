using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

// [дод 1] Декоратор: приймає інший IOrderStore і сам є IOrderStore.
// Той самий контракт, нова поведінка — кешування читань. Сервіс про це не знає.
public sealed class CachingOrderStore(IOrderStore inner) : IOrderStore
{
    private readonly IOrderStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private IReadOnlyList<Order>? _list;

    public IOrderStore Inner => _inner;

    // Скільки разів запитали список і скільки з них реально дійшли до внутрішнього сховища.
    public int Reads { get; private set; }
    public int InnerReads { get; private set; }

    public IReadOnlyList<Order> List()
    {
        Reads++;
        if (_list is null)
        {
            _list = _inner.List();
            InnerReads++;
        }

        return _list;
    }

    public Order? GetById(string id) =>
        List().FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase));

    // Будь-яка зміна скидає кеш: наступне читання піде у внутрішнє сховище.
    public void Add(Order order)
    {
        _inner.Add(order);
        _list = null;
    }

    public void Update(Order order)
    {
        _inner.Update(order);
        _list = null;
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        _list = null;
        return removed;
    }
}