using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class ProductCsvImporter
{
    // Крапка з комою: не конфліктує з комою в назвах товарів
    private const char Separator = ';';

    public static ImportResult<ProductDto> Load(string path)
    {
        var items = new List<ProductDto>();
        var errors = new List<string>();
        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;
            if (number == 1 && line.StartsWith("id", StringComparison.OrdinalIgnoreCase))
                continue; // рядок заголовків

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<ProductDto>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 3 } => new ParseFailed($"очікую 3 колонки, отримав {parts.Length}"),
            ["", _, _] or [_, "", _] => new ParseFailed("Id або назва порожні"),
            [var id, var name, var raw] when TryParsePrice(raw, out decimal price)
                => new ParseOk(new ProductDto(id, name, price)),
            [_, _, var raw] => new ParseFailed($"ціна '{raw}' не є додатним числом у форматі 0.00"),
            _ => new ParseFailed($"занадто багато колонок: {parts.Length}")
        };
    }

    // AllowDecimalPoint, а не Number: Number дозволяє роздільник тисяч (кому),
    // і "249,90" тихо перетворилося б на 24990.
    internal static bool TryParsePrice(string raw, out decimal price) =>
        decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out price)
        && price > 0;

    private abstract record ParseOutcome;
    private sealed record ParseOk(ProductDto Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}