using Faktury.Core.Model;
using Faktury.Core.Numeracja;
using Faktury.Core.Obliczenia;
using Faktury.Core.Walidacja;
using Xunit;

namespace Faktury.Core.Tests;

public class NumeracjaTests
{
    [Fact]
    public void Numeruje_kolejno_w_obrebie_miesiaca_i_resetuje_w_nowym()
    {
        var g = new GeneratorNumerow("FV");
        Assert.Equal("FV/2026/10/0001", g.Nastepny(new DateOnly(2026, 10, 9)));
        Assert.Equal("FV/2026/10/0002", g.Nastepny(new DateOnly(2026, 10, 20)));
        Assert.Equal("FV/2026/11/0001", g.Nastepny(new DateOnly(2026, 11, 2)));
    }

    [Fact]
    public void Kontynuuje_od_wczytanego_licznika()
    {
        var g = new GeneratorNumerow();
        g.UstawLicznik(2026, 10, 41);
        Assert.Equal("FV/2026/10/0042", g.Nastepny(new DateOnly(2026, 10, 31)));
    }
}

public class KwotaSlownieTests
{
    [Theory]
    [InlineData(0, "zero złotych 00/100")]
    [InlineData(1, "jeden złoty 00/100")]
    [InlineData(2.5, "dwa złote 50/100")]
    [InlineData(5, "pięć złotych 00/100")]
    [InlineData(12, "dwanaście złotych 00/100")]
    [InlineData(22, "dwadzieścia dwa złote 00/100")]
    [InlineData(1000, "tysiąc złotych 00/100")]
    [InlineData(2000, "dwa tysiące złotych 00/100")]
    [InlineData(5000, "pięć tysięcy złotych 00/100")]
    [InlineData(12000, "dwanaście tysięcy złotych 00/100")]
    [InlineData(1234.56, "tysiąc dwieście trzydzieści cztery złote 56/100")]
    [InlineData(1000000, "milion złotych 00/100")]
    public void Zamienia_kwote_na_slowa(decimal kwota, string oczekiwane) =>
        Assert.Equal(oczekiwane, KwotaSlownie.Zamien(kwota));
}

public class WalidatorFakturyTests
{
    private static Faktura Poprawna() => new()
    {
        Numer = "FV/2026/10/0001",
        DataWystawienia = new DateOnly(2026, 10, 9),
        DataSprzedazy = new DateOnly(2026, 10, 9),
        TerminPlatnosci = new DateOnly(2026, 10, 23),
        Sprzedawca = new Kontrahent { Nazwa = "S", Nip = "5261040828", Ulica = "u", KodPocztowy = "00-000", Miejscowosc = "m" },
        Nabywca = new Kontrahent { Nazwa = "N", Nip = "7010001453", Ulica = "u", KodPocztowy = "00-000", Miejscowosc = "m" },
        Pozycje = { new PozycjaFaktury { Nazwa = "usługa", Ilosc = 1, CenaNetto = 100m, Stawka = StawkaVat.Vat23 } },
    };

    [Fact]
    public void Poprawna_faktura_nie_ma_bledow() => Assert.Empty(WalidatorFaktury.Sprawdz(Poprawna()));

    [Fact]
    public void Wykrywa_brak_podstawy_zwolnienia()
    {
        var f = Poprawna();
        f.Pozycje.Add(new PozycjaFaktury { Nazwa = "zw", Ilosc = 1, CenaNetto = 10m, Stawka = StawkaVat.Zwolniona });
        Assert.Contains(WalidatorFaktury.Sprawdz(f), b => b.Contains("pkt 19"));
    }

    [Fact]
    public void Wykrywa_bledny_nip_sprzedawcy()
    {
        var f = Poprawna();
        var zly = new Faktura
        {
            Numer = f.Numer, DataWystawienia = f.DataWystawienia, DataSprzedazy = f.DataSprzedazy,
            TerminPlatnosci = f.TerminPlatnosci, Nabywca = f.Nabywca, Pozycje = f.Pozycje,
            Sprzedawca = new Kontrahent { Nazwa = "S", Nip = "1234567890", Ulica = "u", KodPocztowy = "00-000", Miejscowosc = "m" },
        };
        Assert.Contains(WalidatorFaktury.Sprawdz(zly), b => b.Contains("NIP sprzedawcy"));
    }
}
