using Core.Events;

namespace Core.Abstractions;

// [дод 1] Альтернатива події: сервіс отримує одержувачів сповіщень через конструктор
// (ручний DI тижня 5). Реалізації живуть у Cli, Core знає лише контракт.
public interface INotificationSink
{
    void Notify(OrderChangedEventArgs change);
}