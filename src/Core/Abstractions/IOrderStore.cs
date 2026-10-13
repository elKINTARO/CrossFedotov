using Core.Domain;

namespace Core.Abstractions;

// Контракт сховища: лише операції, які справді потрібні OrderService.
// Жодного SaveToFile чи Flush — специфіка реалізації в контракт не потрапляє.
public interface IOrderStore
{
    // IReadOnlyList — зовнішній код не мутує сховище в обхід Add/Update/Remove.
    IReadOnlyList<Order> List();
    // List використовує OrderService.All, Where, ліміт чернеток клієнта

    // Order? — запису з таким id може не бути, це нормальний результат пошуку.
    Order? GetById(string id);
    // GetById використовує Find і кожна операція зміни стану

    void Add(Order order);
    // Add використовує CreateOrder

    void Update(Order order);
    // Update використовує AddLine, Confirm, Cancel

    bool Remove(string id);
}