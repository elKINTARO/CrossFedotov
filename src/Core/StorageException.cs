namespace Core;

// Технічна помилка сховища: файл даних пошкоджений або не читається.
// На відміну від DomainException, це не порушення бізнес-правила.
// Початкова причина (JsonException, ArgumentException тощо) зберігається в InnerException.
public sealed class StorageException(string message, Exception inner) : Exception(message, inner);