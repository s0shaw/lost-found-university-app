using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.UnitTests.Domain;

public sealed class UniversityMemberTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private const string UniversityId = "UM-483920";

    [Fact]
    public void Member_has_no_validity_window_and_is_valid_at_any_time()
    {
        var member = UniversityMember.CreateMember(UniversityId, "Ada Lovelace");

        Assert.Equal(UniversityMemberType.Member, member.Type);
        Assert.Null(member.ValidFrom);
        Assert.Null(member.ValidUntil);
        Assert.True(member.IsValidAt(Now.AddYears(-5)));
        Assert.True(member.IsValidAt(Now.AddYears(5)));
    }

    [Fact]
    public void Visitor_is_valid_only_inside_its_window()
    {
        var visitor = UniversityMember.CreateVisitor(UniversityId, "Guest", Now, Now.AddDays(1));

        Assert.False(visitor.IsValidAt(Now.AddMinutes(-1)));
        Assert.True(visitor.IsValidAt(Now));
        Assert.True(visitor.IsValidAt(Now.AddDays(1)));
        Assert.False(visitor.IsValidAt(Now.AddDays(1).AddMinutes(1)));
    }

    [Fact]
    public void Visitor_window_must_end_after_it_starts()
    {
        var ex = Assert.Throws<DomainException>(() =>
            UniversityMember.CreateVisitor(UniversityId, "Guest", Now, Now.AddHours(-1))
        );

        Assert.Contains("must end after it starts", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deactivated_member_is_invalid_even_inside_its_window()
    {
        var visitor = UniversityMember.CreateVisitor(UniversityId, "Guest", Now, Now.AddDays(1));

        visitor.Deactivate();

        Assert.False(visitor.IsValidAt(Now));
    }
}
