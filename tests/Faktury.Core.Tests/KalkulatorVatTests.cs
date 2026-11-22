using Faktury.Core.Model;
using Faktury.Core.Obliczenia;
using Xunit;

namespace Faktury.Core.Tests;

public class KalkulatorVatTests
{
    [Fact]
    public void Zaokraglanie_od_pol_grosza_w_gore_zgodnie_z_art_106e_ust_11()
    {
        // 0,125 zł -> 0,13 zł (a nie 0,12 jak w zaokrąglaniu bankowym)
        Assert.Equal(0.13m, Pieniadze.Zaokraglij(0.125m));
        Assert.Equal(0.12m, Pieniadze.Zaokraglij(0.124m));
        Assert.Equal(1.00m, Pieniadze.Zaokraglij(0.995m));
    }

    [Fact]
    public void Wartosc_netto_pozycji_to_ilosc_razy_cena()
    {
        var p = new PozycjaFaktury { Nazwa = "x", Ilosc = 3, CenaNetto = 19.99m, Stawka = StawkaVat.Vat23 };
        Assert.Equal(59.97m, p.WartoscNetto);
        Assert.Equal(13.79m, p.KwotaVat);      // 59.97 * 0.23 = 13.7931
        Assert.Equal(73.76m, p.WartoscBrutto);
    }

    [Fact]
    public void Rabat_obniza_cene_jednostkowa()
    {
        var p = new PozycjaFaktury { Nazwa = "x", Ilosc = 2, CenaNetto = 100m, RabatProcent = 10, Stawka = StawkaVat.Vat23 };
        Assert.Equal(90m, p.CenaNettoPoRabacie);
        Assert.Equal(180m, p.WartoscNetto);
    }

    [Fact]
    public void Podsumowanie_grupuje_wg_stawek_i_liczy_vat_od_sumy()
    {
        var pozycje = new[]
        {
            new PozycjaFaktury { Nazwa = "a", Ilosc = 1, CenaNetto = 100m, Stawka = StawkaVat.Vat23 },
            new PozycjaFaktury { Nazwa = "b", Ilosc = 1, CenaNetto = 200m, Stawka = StawkaVat.Vat23 },
            new PozycjaFaktury { Nazwa = "c", Ilosc = 1, CenaNetto = 50m,  Stawka = StawkaVat.Vat8 },
        };

        var p = KalkulatorVat.Podsumuj(pozycje);

        Assert.Equal(2, p.WgStawek.Count);
        var s23 = p.WgStawek.Single(s => s.Stawka == StawkaVat.Vat23);
        Assert.Equal(300m, s23.Netto);
        Assert.Equal(69m, s23.Vat);
        var s8 = p.WgStawek.Single(s => s.Stawka == StawkaVat.Vat8);
        Assert.Equal(4m, s8.Vat);
        Assert.Equal(350m, p.RazemNetto);
        Assert.Equal(73m, p.RazemVat);
        Assert.Equal(423m, p.RazemBrutto);
    }

    [Fact]
    public void Vat_od_sumy_moze_roznic_sie_od_sumy_vat_z_wierszy_o_grosz()
    {
        // 3 pozycje po 0,10 zł netto w 23%: VAT z każdego wiersza = 0,023 -> 0,02; suma 0,06.
        // VAT od sumy netto 0,30 = 0,069 -> 0,07. Dokładnie dlatego stosujemy JEDNĄ metodę.
        var pozycje = Enumerable.Range(0, 3)
            .Select(_ => new PozycjaFaktury { Nazwa = "x", Ilosc = 1, CenaNetto = 0.10m, Stawka = StawkaVat.Vat23 })
            .ToList();

        var sumaZWierszy = pozycje.Sum(p => p.KwotaVat);
        var odSumy = KalkulatorVat.Podsumuj(pozycje).RazemVat;

        Assert.Equal(0.06m, sumaZWierszy);
        Assert.Equal(0.07m, odSumy);
    }

    [Fact]
    public void Metoda_od_brutto_wg_wzoru_z_art_106e_ust_7()
    {
        // 123 zł brutto w 23% -> VAT = 123*23/123 = 23 zł
        Assert.Equal(23m, KalkulatorVat.KwotaVatOdBrutto(123m, StawkaVat.Vat23));
        // 108 zł brutto w 8% -> 8 zł
        Assert.Equal(8m, KalkulatorVat.KwotaVatOdBrutto(108m, StawkaVat.Vat8));
    }

    [Fact]
    public void Stawka_zwolniona_nie_nalicza_podatku()
    {
        var p = KalkulatorVat.Podsumuj(new[]
        {
            new PozycjaFaktury { Nazwa = "korepetycje", Ilosc = 10, CenaNetto = 60m, Stawka = StawkaVat.Zwolniona },
        });
        Assert.Equal(600m, p.RazemNetto);
        Assert.Equal(0m, p.RazemVat);
        Assert.Equal(600m, p.RazemBrutto);
    }
}
