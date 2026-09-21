using Core.Dto;

namespace Core.Import;

// Формат: P;id;name;price  або  C;id;fullName;email[;phone]
public static class MixedCsvImporter
{
    private const char Separator = ';';

    public static MixedImportResult Load(string path)
    {
        var products = new List<ProductDto>();
        var customers = new List<CustomerDto>();
        var errors = new List<string>();
        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            switch (ParseLine(line))
            {
                case ProductOk p:
                    products.Add(p.Value);
                    break;
                case CustomerOk c:
                    customers.Add(c.Value);
                    break;
                case Failed f:
                    errors.Add($"рядок {number}: {f.Reason}");
                    break;
            }
        }

        return new MixedImportResult(products, customers, errors);
    }

    private static Outcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["P", var id, var name, var raw]
                when id != "" && name != "" && ProductCsvImporter.TryParsePrice(raw, out decimal price)
                => new ProductOk(new ProductDto(id, name, price)),
            ["P", ..] => new Failed("некоректний товар, очікую P;id;name;price"),

            ["C", var id, var fullName, var email]
                when id != "" && fullName != "" && email.Contains('@')
                => new CustomerOk(new CustomerDto(id, fullName, email)),
            ["C", var id, var fullName, var email, var phone]
                when id != "" && fullName != "" && email.Contains('@')
                => new CustomerOk(new CustomerDto(id, fullName, email, phone == "" ? null : phone)),
            ["C", ..] => new Failed("некоректний клієнт, очікую C;id;fullName;email[;phone]"),

            [var kind, ..] => new Failed($"невідомий тип запису '{kind}'"),
            _ => new Failed("порожній рядок")
        };
    }

    private abstract record Outcome;
    private sealed record ProductOk(ProductDto Value) : Outcome;
    private sealed record CustomerOk(CustomerDto Value) : Outcome;
    private sealed record Failed(string Reason) : Outcome;
}