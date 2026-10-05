using Core.Dto;

namespace Core.Domain;

// [дод 1] Міст між тижнем 3 і тижнем 4: ті самі «дані + помилки»,
// але тепер рядок може не пройти ще й доменні інваріанти.
public static class CatalogBuilder
{
    public static ImportResult<Product> FromImport(ImportResult<ProductDto> source)
    {
        var products = new List<Product>();
        var errors = new List<string>(source.Errors);

        foreach (ProductDto dto in source.Items)
        {
            try
            {
                products.Add(Product.FromDto(dto));
            }
            catch (ArgumentException ex)
            {
                // ArgumentOutOfRangeException успадковує ArgumentException,
                // тому один catch покриває обидва випадки.
                errors.Add($"{dto.Id}: {ex.Message}");
            }
        }

        return new ImportResult<Product>(products, errors);
    }
}