using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Core;

namespace Cli;

// Вивід інформації про середовище (lab01-02), винесений з Program.cs.
internal static class EnvironmentView
{
    public static void Print(bool asJson)
    {
        EnvironmentReport report = EnvironmentInfo.Collect();

        if (asJson)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };
            Console.WriteLine(JsonSerializer.Serialize(report, options));
            return;
        }

        Console.WriteLine("CrossApp – інформація про середовище");
        Console.WriteLine("Студент: Федотов Кирило, група ФЕІ-36с");
        Console.WriteLine(new string('-', 52));
        Console.WriteLine($"ОС              : {report.OsDescription}");
        Console.WriteLine($"Runtime         : {report.FrameworkDescription}");
        Console.WriteLine($"Архітектура     : {report.ProcessArchitecture}");
        Console.WriteLine($"RID (визначено) : {report.DetectedRid}");
        Console.WriteLine($"RID (від .NET)  : {report.ReportedRid}");
        Console.WriteLine($"Каталог збірки  : {report.BaseDirectory}");
        Console.WriteLine($"Поточний каталог: {report.CurrentDirectory}");
        Console.WriteLine($"TFM             : {report.BuildNote}");
    }
}