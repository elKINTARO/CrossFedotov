using Core.Events;

namespace Cli.Notifications;

// Одне форматування для підписника-події і для обох sink-ів, щоб рядки в журналі збігалися.
internal static class OrderLogFormat
{
    public static string Line(OrderChangedEventArgs e) =>
        $"{e.At:HH:mm:ss} {e.Kind,-15} {e.OrderId,-12} {e.Delta,10:+0.00;-0.00} → рядків {e.LinesCount}, сума {e.Total:F2}";
}