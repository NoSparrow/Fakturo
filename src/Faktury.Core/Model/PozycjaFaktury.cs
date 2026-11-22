using Faktury.Core.Obliczenia;

namespace Faktury.Core.Model;

/// <summary>
/// Jeden wiersz faktury (w strukturze KSeF FA(3) to element <c>FaWiersz</c>).
///
/// Ustawa o VAT, art. 106e ust. 1 pkt 7–12, wymaga dla każdej pozycji:
///  pkt 7  – nazwę (rodzaj) towaru lub usługi,
///  pkt 8  – miarę i ilość,
///  pkt 9  – cenę jednostkową netto,
///  pkt 10 – kwoty rabatów (jeśli nie są ujęte w cenie),
///  pkt 11 – wartość sprzedaży netto,
///  pkt 12 – stawkę podatku.
///
/// DLACZEGO <c>decimal</c>, a nie <c>double</c>?
/// Typ double przechowuje liczby binarnie i 0.1 + 0.2 != 0.3. Przy pieniądzach
/// prowadzi to do błędów groszowych, które na fakturze są niedopuszczalne.
/// decimal przechowuje liczby dziesiętnie – 0.1m + 0.2m == 0.3m.
/// </summary>
public sealed class PozycjaFaktury
{
    public required string Nazwa { get; init; }

    /// <summary>Jednostka miary, np. "szt.", "godz.", "usł.", "kg", "m".</summary>
    public string Jednostka { get; init; } = "szt.";

    public required decimal Ilosc { get; init; }

    /// <summary>Cena jednostkowa netto (bez VAT).</summary>
    public required decimal CenaNetto { get; init; }

    /// <summary>Rabat procentowy 0–100. Domyślnie brak.</summary>
    public decimal RabatProcent { get; init; } = 0m;

    public required StawkaVat Stawka { get; init; }

    /// <summary>Opcjonalny kod PKWiU / GTU – przydatny w rozliczeniach, nieobowiązkowy.</summary>
    public string? Pkwiu { get; init; }

    // ---- Wartości wyliczane (nie przechowujemy ich, żeby nie było rozjazdów) ----

    /// <summary>Cena jednostkowa netto po rabacie.</summary>
    public decimal CenaNettoPoRabacie =>
        Pieniadze.Zaokraglij(CenaNetto * (1m - RabatProcent / 100m));

    /// <summary>Wartość netto pozycji = ilość × cena netto po rabacie (pkt 11).</summary>
    public decimal WartoscNetto => Pieniadze.Zaokraglij(Ilosc * CenaNettoPoRabacie);

    /// <summary>Kwota VAT pozycji – TYLKO do prezentacji w wierszu. Sumy VAT faktury liczymy wg stawek (patrz KalkulatorVat).</summary>
    public decimal KwotaVat => Pieniadze.Zaokraglij(WartoscNetto * Stawka.Procent / 100m);

    public decimal WartoscBrutto => WartoscNetto + KwotaVat;
}
