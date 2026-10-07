namespace UniversityLostFound.Application.Catalog;

public sealed class ListCategoriesHandler(ICatalogRepository catalog)
{
    public async Task<IReadOnlyList<CategoryDto>> HandleAsync(CancellationToken cancellationToken = default)
        => await catalog.ListCategoriesAsync(cancellationToken);
}
