# CrossApp

Наскрізний проєкт з крос-платформного програмування.

## Предметна область

**Замовлення.** Сутності: `Customer` (клієнт), `Product` (товар),
`Order` (замовлення), `OrderLine` (рядок замовлення).

Призначення: оформлення замовлень клієнтів і підрахунок їх вартості.

## Запуск

```
dotnet build
dotnet run --project src/Cli                                     # lab05: сховище в пам'яті
dotnet run --project src/Cli -- --file                           # lab05: сховище у файлі
dotnet run --project src/Cli -- --cache                          # lab05: пам'ять + декоратор кешу
dotnet run --project src/Cli -- --file --cache                   # lab05: файл + декоратор кешу
dotnet run --project src/Cli -- --domain                         # lab04: доменна модель, інваріанти
dotnet run --project src/Cli -- --import                         # lab03: імпорт data/sample.csv
dotnet run --project src/Cli -- --import data/sample.json        # lab03: імпорт JSON
dotnet run --project src/Cli -- --mixed data/mixed.csv           # lab03: товари і клієнти в одному файлі
dotnet run --project src/Cli -- --env                            # lab02: інформація про середовище
dotnet run --project src/Cli -- --env --json                     # lab02: те саме у JSON
dotnet run --project src/Cli -- --events                         # lab06: події, два підписники, відписка
dotnet run --project src/Cli -- --sink                           # lab06: INotificationSink через конструктор
```

Коди завершення: `0` успіх, `1` файл не знайдено, `2` непідтримуваний формат.

## Формат даних

`data/sample.csv` — товари (`ProductDto`):

- кодування UTF-8
- роздільник `;` (кома може бути в назвах)
- заголовок `id;name;price` необов'язковий, рядки з `#` ігноруються
- ціна — додатне число з крапкою (`649.50`), розбір через `CultureInfo.InvariantCulture`

Рядки 12–14 навмисно пошкоджені (тестові дані): бракує колонки, кома в ціні, порожня назва.

`data/mixed.csv` — записи різних типів за префіксом:
`P;id;name;price` — товар, `C;id;fullName;email[;phone]` — клієнт.

## Середовище

- .NET SDK 10.0.11
- RID: osx-arm64
- ОС: macOS 26.6.2
- Редактор: VS Code + C# Dev Kit

## Self-contained публікація

```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true
dotnet publish src/Cli -c Release -r linux-x64 --self-contained true
```

## Порівняння розмірів каталогів 

```bash
du -sh src/Cli/bin/Release/net10.0/linux-x64/publish
# 79M    src/Cli/bin/Release/net10.0/linux-x64/publish

du -sh src/Cli/bin/Release/net10.0/win-x64/publish
# 77M    src/Cli/bin/Release/net10.0/win-x64/publish

du -sh src/Cli/bin/Release/net10.0/osx-arm64/publish   
# 83M    src/Cli/bin/Release/net10.0/osx-arm64/publish  
```

## Структура solution

```text
CrossApp.slnx
└── src/
    ├── Core/        # class library, без точки входу
    │   ├── EnvironmentInfo.cs
    │   ├── Dto/       # ProductDto, CustomerDto, ImportResult<T>, MixedImportResult
    │   ├── Import/    # ProductCsvImporter, ProductJsonImporter, MixedCsvImporter
    │   ├── Domain/    # сутності з поведінкою (тиждень 4)
    │   ├── Events/        # OrderChangeKind, OrderChangedEventArgs
    │   ├── Abstractions/  # IOrderStore — контракт сховища
    │   ├── Services/      # OrderService
    │   ├── Storage/       # InMemoryOrderStore, FileOrderStore, CachingOrderStore
    │   └── SampleData.cs
    │   └── Storage/   # сховища (тиждень 5)
    └── Cli/         # консольний застосунок, ProjectReference → Core
    ├── data/
    ├── sample.csv
    ├── sample.json
    ├── mixed.csv
    └── Notifications/ # OrderLogFormat, ConsoleSink, FileSink
```

Залежність одностороння: Cli → Core.

## Публікація

| RID | Режим | Розмір publish | Потрібен runtime |
|---|---|---|---|
| osx-arm64 | self-contained | 83 МБ | ні |
| osx-arm64 | framework-dependent | 0.2 МБ | так (.NET 10) |
| osx-arm64 | self-contained + trimmed | 18 МБ | ні |

**Self-contained** — у каталог publish копіюється .NET runtime.
Застосунок працює на машині без встановленого .NET, але каталог великий
і прив'язаний до конкретної RID.

**Framework-dependent** — лише код застосунку і його залежності.
Каталог маленький, але на цільовій машині має бути .NET 10.

## Доменна модель (lab04)

`Order` — агрегат: володіє своїми `OrderLine` і є єдиною точкою їх зміни.
`Product` — товар каталогу. Створення лише через фабричні методи `Create`,
конструктори приватні, стан змінюється тільки методами з бізнес-іменами.

### Інваріанти

| Правило | Тип винятку | Де перевіряється |
|---|---|---|
| Номер замовлення не порожній | `ArgumentException` | `Order.Create` |
| Замовлення має клієнта | `ArgumentException` | `Order.Create` |
| Id і назва товару в рядку не порожні | `ArgumentException` | `OrderLine.Create` |
| Кількість у рядку > 0 | `ArgumentOutOfRangeException` | `OrderLine.Create` |
| Ціна в рядку ≥ 0 | `ArgumentOutOfRangeException` | `OrderLine.Create` |
| Ціна товару каталогу > 0 | `ArgumentOutOfRangeException` | `Product.Create`, `Product.ChangePrice` |
| Рядки можна додавати лише в чернетку | `InvalidOperationException` | `Order.AddLine` |
| Порожнє замовлення не підтверджується | `InvalidOperationException` | `Order.Confirm` |
| Допустимі переходи статусів (Draft→Confirmed, Draft→Cancelled, Confirmed→Cancelled) | `InvalidOperationException` | `Order.TransitionTo` |
| Клієнт має не більше 3 чернеток | `InvalidOperationException` | `CustomerOrderPolicy` |

`Argument*` — некоректний вхідний аргумент; `InvalidOperationException` — аргументи
коректні, але операція неможлива в поточному стані. На тижні 10 Api перетворить їх
на 400 і 409 відповідно.

### DTO і сутність

`ProductDto` / `OrderDto` переносять дані (формат файлу і сховища тижня 5),
сутності захищають правила. Зв'язок — `ToDto()` і `FromDto()`; `FromDto`
проходить ті самі перевірки, що й `Create`, тому пошкоджений файл не створить
некоректний об'єкт.

## Сервісний шар і сховища (lab05)

| Роль | Тип |
|---|---|
| Контракт сховища | `Core.Abstractions.IOrderStore` |
| Реалізація в пам'яті | `Core.Storage.InMemoryOrderStore` — `Dictionary`, дані зникають після виходу |
| Файлова реалізація | `Core.Storage.FileOrderStore` — кеш у пам'яті, запис JSON після кожної зміни |
| Декоратор | `Core.Storage.CachingOrderStore` — кешує читання будь-якого `IOrderStore` |
| Сервіс | `Core.Services.OrderService` — `CreateOrder`, `AddLine`, `Confirm`, `Cancel`, `All`, `Find`, `Where` |
| Composition root | `src/Cli/Program.cs` + `src/Cli/StoreFactory.cs` |

Схема залежностей:

```
Cli (Program.cs, StoreFactory) → OrderService → IOrderStore ← InMemoryOrderStore | FileOrderStore | CachingOrderStore(IOrderStore)
```

Файл даних у режимі `--file`: `<каталог збірки>/data/orders.json`
(для `dotnet run` це `src/Cli/bin/Debug/net10.0/data/orders.json`).
Формат — масив `OrderDto` з вкладеними `OrderLineDto`; агрегат зберігається як одне ціле.
Якщо формат змінився і запуск падає з `JsonException`, файл треба видалити.

DI-контейнер не використовується: залежності передаються через конструктор вручну.

### Фабрика проти «усе в Program.cs»

Без фабрики вибір реалізації виглядав би так, прямо в `Program.cs`:

```csharp
IOrderStore store = args.Contains("--file")
    ? new FileOrderStore(dataPath)
    : new InMemoryOrderStore(SampleData.Orders());
if (args.Contains("--cache")) store = new CachingOrderStore(store);
```

`StoreFactory` містить той самий код, але `Program.cs` лишається коротким, а правила
вибору сховища можна змінювати й тестувати окремо. Обидва варіанти — composition root:
конкретні класи сховищ живуть лише в проєкті Cli, а `OrderService` про них не знає.

## Доменні події (lab06)

`OrderService` оголошує `event EventHandler<OrderChangedEventArgs>? Changed`
і піднімає її в одному методі `OnChanged` — лише після успішної зміни і збереження.

| Подія | Коли | Delta |
|---|---|---|
| `OrderCreated` | `CreateOrder` | 0 |
| `LineAdded` | `AddLine` | сума доданого рядка |
| `OrderConfirmed` | `Confirm` | 0 |
| `OrderCancelled` | `Cancel` | 0 |

Дані події: `OrderId`, `CustomerId`, `Kind`, `LinesCount`, `Total` (після зміни), `Delta`, `At`.

Підписники в Cli (`EventsDemo`): лямбда-лічильник із замиканням і локальна функція,
що пише в консоль та в `logs/app.log`. Після `-=` операції виконуються без запису в журнал.

Журнал: `<каталог збірки>/logs/app.log` (для `dotnet run` — `src/Cli/bin/Debug/net10.0/logs/app.log`).

Core не звертається до консолі і не пише журнал. Перевірка: `grep -rn "Console" src/Core` — порожньо.

Майбутні підписники: тиждень 10 — логер Api, тиждень 12 — компонент Blazor, що оновлює таблицю.

### Делегати

- `FindAll(Func<Order, bool>)` — пошук, критерій задає викликач, потрібне значення у відповідь.
- `ProductCsvImporter.Load(path, Action<string>? onSkipped)` — звіт про пропущені рядки в момент читання.

### Подія проти INotificationSink (додаткове 1)

| | `event Changed` | `INotificationSink` через конструктор |
|---|---|---|
| Хто вирішує про одержувачів | підписник сам, у будь-який момент | composition root, один раз |
| Відписка під час роботи | `-=` | неможлива |
| Залежність сервісу | ні від чого | від інтерфейсу в конструкторі |
| Що видно з сигнатури | нічого | конструктор явно показує одержувачів |
| Тест тижня 8 | підписатись лямбдою | передати фейковий sink |

Подія зручніша, коли підписники з'являються і зникають (компонент Blazor).
Sink — коли набір одержувачів фіксований і має бути явно видно в конструкторі.