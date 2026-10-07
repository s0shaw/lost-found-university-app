using UniversityLostFound.Domain.Common;

namespace UniversityLostFound.UnitTests.Domain;

public sealed class ShortCodeTests
{
    [Fact]
    public void Generate_produces_prefixed_code_of_fixed_shape()
    {
        var code = ShortCode.Generate("UM");

        Assert.StartsWith("UM-", code, StringComparison.Ordinal);
        Assert.Equal(3 + ShortCode.BodyLength, code.Length);
        Assert.True(ShortCode.IsValid(code, "UM"));
    }

    [Fact]
    public void Generate_pads_small_numbers_so_the_length_never_varies()
    {
        for (var i = 0; i < 500; i++)
        {
            var code = ShortCode.Generate("LF");

            Assert.Equal(3 + ShortCode.BodyLength, code.Length);
            Assert.All(code[3..], c => Assert.True(char.IsAsciiDigit(c)));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("CM483920")] // missing dash
    [InlineData("UM-48392")] // body too short
    [InlineData("UM-4839201")] // body too long
    [InlineData("UM-48392X")] // not a digit
    [InlineData("um-483920")] // lower case prefix
    [InlineData("LF-483920")] // wrong prefix
    public void IsValid_rejects_malformed_codes(string? value)
    {
        Assert.False(ShortCode.IsValid(value, "UM"));
    }
}
