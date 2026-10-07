using FluentValidation;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Application.Staff;

public sealed record LoginCommand(string UniversityId, string Password);

public sealed record StaffLoginResult(StaffSessionDto Session, StaffToken Token);

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public const int PasswordMaxLength = 128;

    public LoginValidator()
    {
        RuleFor(c => c.UniversityId).NotEmpty().MaximumLength(ShortCode.LengthFor(UniversityMember.CodePrefix));
        RuleFor(c => c.Password).NotEmpty().MaximumLength(PasswordMaxLength);
    }
}

public sealed class LoginHandler(
    IStaffAccountRepository staff,
    IPasswordHasher hasher,
    IStaffTokenIssuer tokens,
    AttemptGuard attempts)
{
    // Same message for unknown id, wrong password, deactivated account — else this becomes a "is this id staff" oracle.
    public const string InvalidCredentialsMessage = "University id or password is incorrect.";

    public async Task<StaffLoginResult> HandleAsync(
        LoginCommand command, CancellationToken cancellationToken = default)
    {
        var universityId = UniversityMemberLookup.Normalize(command.UniversityId);
        var attemptKey = "login:" + universityId;
        attempts.EnsureAllowed(attemptKey, AttemptGuard.LoginLimit);

        var account = await staff.FindActiveByUniversityIdAsync(universityId, cancellationToken);

        // Always verify, even with no account: an early return would make an unknown id faster to probe.
        if (!hasher.Verify(command.Password, account?.PasswordHash ?? hasher.DummyHash) || account is null)
        {
            attempts.RecordFailure(attemptKey);
            throw new InvalidCredentialsException(InvalidCredentialsMessage);
        }

        attempts.Reset(attemptKey);

        var token = tokens.Issue(account.StaffAccountId, account.UniversityId, account.FullName);

        return new StaffLoginResult(
            new StaffSessionDto(account.UniversityId, account.FullName, token.ExpiresAt),
            token);
    }
}
