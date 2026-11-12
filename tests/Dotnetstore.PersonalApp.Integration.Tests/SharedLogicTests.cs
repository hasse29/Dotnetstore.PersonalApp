using Dotnetstore.PersonalApp.Shared.Photos;

namespace Dotnetstore.PersonalApp.Integration.Tests;

public sealed class PhotoInitialsTests
{
    [Theory]
    [InlineData("Acme Tools AB", "AT")]
    [InlineData("anna andersson", "AA")]
    [InlineData("  Madonna  ", "M")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void TakesTheFirstLettersOfTheFirstTwoWords(string name, string expected) =>
        PhotoInitials.From(name).ShouldBe(expected);

    [Fact]
    public void ANullNameIsRejected() => Should.Throw<ArgumentNullException>(() => PhotoInitials.From(null!));
}
