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