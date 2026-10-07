using UniversityLostFound.Application.Reports;

namespace UniversityLostFound.UnitTests.Application;

public sealed class TrackReportValidatorTests
{
    private readonly TrackReportValidator _validator = new();

    [Fact]
    public void A_university_id_and_a_tracking_code_are_enough()
        => Assert.True(_validator.Validate(new TrackReportCommand("UM-204718", "LF-703529")).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_university_id_is_rejected(string universityId)
        => Assert.False(_validator.Validate(new TrackReportCommand(universityId, "LF-703529")).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_tracking_code_is_rejected(string trackingCode)
        => Assert.False(_validator.Validate(new TrackReportCommand("UM-204718", trackingCode)).IsValid);
}
