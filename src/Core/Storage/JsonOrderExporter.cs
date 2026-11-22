using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Експорт усієї колекції в JSON. Формат той самий, що й у FileOrderStore,
// тож експортований файл можна знову імпортувати командою import.
public static class JsonOrderExporter
{
    public static int Export(IEnumerable<Order> orders, string path)
    {
        ArgumentNullException.ThrowIfNull(orders);

        List<OrderDto> dtos = orders.OrderBy(o => o.CreatedAt)
                                    .ThenBy(o => o.Id, StringComparer.Ordinal)
                                    .Select(o => o.ToDto())
                                    .ToList();

        OrderJson.WriteAtomic(path, dtos);
        return dtos.Count;
    }
}