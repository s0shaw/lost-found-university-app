namespace UniversityLostFound.Application.Catalog;

public sealed record CategoryDto(Guid Id, string Name, IReadOnlyList<QuestionDto> Questions);

public sealed record QuestionDto(Guid Id, string Text, IReadOnlyList<QuestionOptionDto> Options);

public sealed record QuestionOptionDto(Guid Id, string Text);
