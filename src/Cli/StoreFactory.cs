using Core;
using Core.Abstractions;
using Core.Storage;

namespace Cli;

// [дод 3] Вибір реалізації винесено з Program.cs у фабрику.
// Фабрика лишається в проєкті Cli: це частина composition root,
// тому конкретні класи сховищ, як і раніше, не потрапляють у Core.Services.
internal static class StoreFactory
{
    public static IOrderStore Create(string[] args, string dataPath)
    {
        IOrderStore store = args.Contains("--file")
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore(SampleData.Orders());

        // Декоратор обгортає будь-яку з двох реалізацій.
        return args.Contains("--cache") ? new CachingOrderStore(store) : store;
    }
}