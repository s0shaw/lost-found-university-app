namespace UniversityLostFound.Application.Catalog;

public sealed class ListLocationsHandler(ICatalogRepository catalog)
{
    public async Task<IReadOnlyList<LocationDto>> HandleAsync(CancellationToken cancellationToken = default)
        => await catalog.ListLocationsAsync(cancellationToken);
}
