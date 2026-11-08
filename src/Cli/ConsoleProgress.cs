namespace Cli;

// [дод 3] IProgress<int>, який малює відсотки в одному рядку консолі.
// Синхронна реалізація, а не Progress<T>: Progress<T> у консолі викликає обробник
// у пулі потоків, і рядки могли б з'явитися не по порядку або вже після результату.
internal sealed class ConsoleProgress(string title) : IProgress<int>
{
    private int _last = -1;

    public void Report(int value)
    {
        if (value == _last)
            return;

        _last = value;
        Console.Write($"\r  {title}: {value,3}%");
        if (value >= 100)
            Console.WriteLine();
    }

    // Якщо імпорт перервався посеред рядка — перейти на новий, щоб повідомлення не злиплося.
    public void Finish()
    {
        if (_last is >= 0 and < 100)
            Console.WriteLine();
    }
}