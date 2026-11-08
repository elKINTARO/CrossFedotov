namespace Core;

// Порушення бізнес-правила: операція коректна за формою, але домен її не дозволяє.
// Приклади: рядок у підтверджене замовлення, повторне підтвердження, ліміт чернеток клієнта.
// Некоректні аргументи (порожній id, кількість 0) лишаються ArgumentException.
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception inner) : base(message, inner) { }
}