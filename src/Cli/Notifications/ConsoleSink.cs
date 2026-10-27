using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

internal sealed class ConsoleSink : INotificationSink
{
    public void Notify(OrderChangedEventArgs change) =>
        Console.WriteLine($"[console] {OrderLogFormat.Line(change)}");
}