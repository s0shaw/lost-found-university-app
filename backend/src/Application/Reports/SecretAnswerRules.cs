using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Reports;

public static class SecretAnswerRules
{
    public static void Validate(
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> questionOptions,
        IReadOnlyList<SecretAnswer> answers)
    {
        if (questionOptions.Count == 0)
        {
            throw new DomainException("Unknown category.");
        }

        if (answers.Count != questionOptions.Count)
        {
            throw new DomainException(
                $"This category has {questionOptions.Count} questions and every one of them needs an answer.");
        }

        if (answers.DistinctBy(a => a.QuestionId).Count() != answers.Count)
        {
            throw new DomainException("A question cannot be answered twice.");
        }

        foreach (var answer in answers)
        {
            if (!questionOptions.TryGetValue(answer.QuestionId, out var options))
            {
                throw new DomainException("An answer points at a question outside this category.");
            }

            if (!options.Contains(answer.OptionId))
            {
                throw new DomainException("An answer points at an option that belongs to another question.");
            }
        }
    }
}
