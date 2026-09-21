# CrossApp

Наскрізний проєкт з крос-платформного програмування.

## Предметна область

**Замовлення.** Сутності: `Customer` (клієнт), `Product` (товар),
`Order` (замовлення), `OrderLine` (рядок замовлення).

Призначення: оформлення замовлень клієнтів і підрахунок їх вартості.

## Запуск

```
dotnet build
dotnet run --project src/Cli                             # імпорт data/sample.csv
dotnet run --project src/Cli -- data/sample.json         # імпорт JSON
dotnet run --project src/Cli -- --mixed data/mixed.csv   # товари і клієнти в одному файлі
dotnet run --project src/Cli -- --env                    # інформація про середовище
dotnet run --project src/Cli -- --env --json             # те саме у JSON
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
    │   └── Storage/   # сховища (тиждень 5)
    └── Cli/         # консольний застосунок, ProjectReference → Core
    ├── data/
    ├── sample.csv
    ├── sample.json
    └── mixed.csv
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