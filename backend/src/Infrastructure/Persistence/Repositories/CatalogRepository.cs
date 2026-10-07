using Microsoft.EntityFrameworkCore;
using UniversityLostFound.Application.Catalog;
using UniversityLostFound.Domain.Locations;

namespace UniversityLostFound.Infrastructure.Persistence.Repositories;

internal sealed class CatalogRepository(AppDbContext db) : ICatalogRepository
{
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetQuestionOptionsAsync(
        Guid categoryId, CancellationToken cancellationToken = default)
    {
        var rows = await db.Questions
            .AsNoTracking()
            .Where(q => q.CategoryId == categoryId)
            .Select(q => new
            {
                q.Id,
                OptionIds = db.QuestionOptions.Where(o => o.QuestionId == q.Id).Select(o => o.Id).ToList(),
            })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Id, r => (IReadOnlyList<Guid>)r.OptionIds);
    }

    public Task<UniversityLocation?> FindLocationAsync(Guid id, CancellationToken cancellationToken = default)
        => db.UniversityLocations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default)
        => await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto(
                c.Id,
                c.Name,
                db.Questions
                    .Where(q => q.CategoryId == c.Id)
                    .OrderBy(q => q.DisplayOrder)
                    .Select(q => new QuestionDto(
                        q.Id,
                        q.Text,
                        db.QuestionOptions
                            .Where(o => o.QuestionId == q.Id)
                            .OrderBy(o => o.DisplayOrder)
                            .Select(o => new QuestionOptionDto(o.Id, o.Text))
                            .ToList()))
                    .ToList()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LocationDto>> ListLocationsAsync(CancellationToken cancellationToken = default)
        => await db.UniversityLocations
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .Select(l => new LocationDto(l.Id, l.Name, l.IsHandoverPoint))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> GetQuestionTextsAsync(
        IReadOnlyCollection<Guid> questionIds, CancellationToken cancellationToken = default)
        => await db.Questions
            .AsNoTracking()
            .Where(q => questionIds.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, q => q.Text, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> GetOptionTextsAsync(
        IReadOnlyCollection<Guid> optionIds, CancellationToken cancellationToken = default)
        => await db.QuestionOptions
            .AsNoTracking()
            .Where(o => optionIds.Contains(o.Id))
            .ToDictionaryAsync(o => o.Id, o => o.Text, cancellationToken);
}
