using Faktury.Core.Walidacja;
using Xunit;

namespace Faktury.Core.Tests;

public class WalidatorNipTests
{
    [Theory]
    [InlineData("5261040828")]      // poprawna suma kontrolna
    [InlineData("526-104-08-28")]   // z myślnikami
    [InlineData("PL5261040828")]    // z prefiksem kraju
    [InlineData("7010001453")]
    public void Akceptuje_poprawne_nipy(string nip) => Assert.True(WalidatorNip.JestPoprawny(nip));

    [Theory]
    [InlineData("5261040829")]  // zła cyfra kontrolna
    [InlineData("123456789")]   // za krótki
    [InlineData("12345678901")] // za długi
    [InlineData("")]
    [InlineData(null)]
    [InlineData("ABCDEFGHIJ")]
    public void Odrzuca_bledne_nipy(string? nip) => Assert.False(WalidatorNip.JestPoprawny(nip));

    [Fact]
    public void Formatuje_do_druku() => Assert.Equal("526-104-08-28", WalidatorNip.Formatuj("5261040828"));
}
