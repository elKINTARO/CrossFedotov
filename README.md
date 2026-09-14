# CrossApp

Наскрізний проєкт з крос-платформного програмування.

## Предметна область

**Замовлення.** Сутності: `Customer` (клієнт), `Product` (товар),
`Order` (замовлення), `OrderLine` (рядок замовлення).

Призначення: оформлення замовлень клієнтів і підрахунок їх вартості.

## Запуск

```
 dotnet build
 dotnet run --project src/Cli
 dotnet run --project src/Cli -- --json
```

## Середовище

- .NET SDK 10.0.11
- RID: osx-arm64
- ОС:  macOS 26.6.2
- Редактор: VS Code + C# Dev Kit

## Self-contained публікація

```
 dotnet publish src/Cli -c Release -r win-x64 --self-contained true
 dotnet publish src/Cli -c Release -r linux-x64 --self-contained true
```
## Порівняння розмірів каталогів 

```
 du -sh src/Cli/bin/Release/net10.0/linux-x64/publish
  79M    src/Cli/bin/Release/net10.0/linux-x64/publish
 du -sh src/Cli/bin/Release/net10.0/win-x64/publish
  77M    src/Cli/bin/Release/net10.0/win-x64/publish
 du -sh src/Cli/bin/Release/net10.0/osx-arm64/publish   
  83M    src/Cli/bin/Release/net10.0/osx-arm64/publish  
```

## Структура solution

CrossApp.slnx
└── src/
    ├── Core/        # class library, без точки входу
    │   ├── EnvironmentInfo.cs
    │   ├── Dto/       # record-типи (тиждень 3)
    │   ├── Domain/    # сутності з поведінкою (тиждень 4)
    │   └── Storage/   # сховища (тиждень 5)
    └── Cli/         # консольний застосунок, ProjectReference → Core

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