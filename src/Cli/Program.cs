using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = Encoding.UTF8;

var info = new
{
    Student = "Федотов Кирил",
    Group = "ФЕІ-36с",
    OsDescription = RuntimeInformation.OSDescription,
    OsVersion = Environment.OSVersion.ToString(),
    ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
    RuntimeIdentifier = RuntimeInformation.RuntimeIdentifier,
    ClrVersion = Environment.Version.ToString(),
    Framework = RuntimeInformation.FrameworkDescription,
    BaseDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Замовлення (Customer, Product, Order, OrderLine)"
};

if (args.Contains("--json"))
{
    var options = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };
    Console.WriteLine(JsonSerializer.Serialize(info, options));
    return;
}

Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
Console.WriteLine($"Студент: {info.Student}, група {info.Group}");
Console.WriteLine(new string('-', 52));
Console.WriteLine($"ОС (OSDescription)   : {info.OsDescription}");
Console.WriteLine($"ОС (Environment)     : {info.OsVersion}");
Console.WriteLine($"Архітектура процесу  : {info.ProcessArchitecture}");
Console.WriteLine($"RID                  : {info.RuntimeIdentifier}");
Console.WriteLine($"Версія .NET (CLR)    : {info.ClrVersion}");
Console.WriteLine($"Runtime              : {info.Framework}");
Console.WriteLine($"Каталог застосунку   : {info.BaseDirectory}");
Console.WriteLine($"Поточний каталог     : {info.CurrentDirectory}");
Console.WriteLine(new string('-', 52));
Console.WriteLine($"Предметна область: {info.Domain}");