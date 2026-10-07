using UniversityLostFound.Domain.Locations;

namespace UniversityLostFound.Application.Catalog;

public interface ICatalogRepository
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetQuestionOptionsAsync(
        Guid categoryId, CancellationToken cancellationToken = default);

    Task<UniversityLocation?> FindLocationAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocationDto>> ListLocationsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetQuestionTextsAsync(
        IReadOnlyCollection<Guid> questionIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetOptionTextsAsync(
        IReadOnlyCollection<Guid> optionIds, CancellationToken cancellationToken = default);
}
