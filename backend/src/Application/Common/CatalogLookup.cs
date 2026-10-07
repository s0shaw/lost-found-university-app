using UniversityLostFound.Application.Catalog;
using UniversityLostFound.Domain.Common;

namespace UniversityLostFound.Application.Common;

public static class CatalogLookup
{
    public static async Task<Guid> RequireLocationAsync(
        this ICatalogRepository catalog,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var location = await catalog.FindLocationAsync(id, cancellationToken)
            ?? throw new DomainException("Unknown location.");

        return location.Id;
    }

    public static async Task<Guid> RequireHandoverPointAsync(
        this ICatalogRepository catalog,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var location = await catalog.FindLocationAsync(id, cancellationToken)
            ?? throw new DomainException("Unknown handover point.");

        if (!location.IsHandoverPoint)
        {
            throw new DomainException("This location does not accept handovers.");
        }

        return location.Id;
    }
}
