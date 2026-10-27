using Core.Abstractions;
using Core.Events;

namespace Cli.Notifications;

internal sealed class FileSink : INotificationSink
{
    private readonly string _path;

    public FileSink(string path)
    {
        _path = path;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    }

    public void Notify(OrderChangedEventArgs change)
    {
        try
        {
            File.AppendAllText(_path, $"[sink] {OrderLogFormat.Line(change)}{Environment.NewLine}");
        }
        catch (IOException ex)
        {
            // Збій журналу не повинен ламати вже виконану бізнес-операцію.
            Console.WriteLine($"  (лог не записано: {ex.Message})");
        }
    }
}