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