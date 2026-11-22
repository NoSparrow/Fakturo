using Faktury.Core.Model;

namespace Faktury.Core.Obliczenia;

/// <summary>Suma netto / VAT / brutto dla jednej stawki (jeden wiersz tabelki "Podsumowanie wg stawek").</summary>
public sealed record PodsumowanieStawki(StawkaVat Stawka, decimal Netto, decimal Vat, decimal Brutto);

/// <summary>Pełne podsumowanie faktury (art. 106e ust. 1 pkt 13–15).</summary>
public sealed record PodsumowanieFaktury(
    IReadOnlyList<PodsumowanieStawki> WgStawek,
    decimal RazemNetto,
    decimal RazemVat,
    decimal RazemBrutto);

/// <summary>
/// Liczy podsumowanie faktury.
///
/// DLACZEGO VAT liczymy od SUMY netto w danej stawce, a nie sumujemy VAT z wierszy?
/// Ustawa (art. 106e ust. 1 pkt 14) mówi o "kwocie podatku od sumy wartości sprzedaży netto,
/// z podziałem na stawki". Gdybyśmy zaokrąglali VAT w każdym wierszu i potem sumowali,
/// przy wielu pozycjach pojawiłyby się różnice groszowe między "VAT z wierszy" a
/// "VAT od sumy". Oba podejścia są w praktyce akceptowane, ale trzeba wybrać JEDNO
/// i stosować konsekwentnie. Wybieramy liczenie od sumy – jest ono też domyślne w KSeF
/// (pola P_13_x / P_14_x struktury FA(3) to właśnie sumy netto i VAT wg stawek).
/// </summary>
public static class KalkulatorVat
{
    public static PodsumowanieFaktury Podsumuj(IEnumerable<PozycjaFaktury> pozycje)
    {
        var wgStawek = pozycje
            .GroupBy(p => p.Stawka)
            .OrderByDescending(g => g.Key.Procent)
            .Select(g =>
            {
                var netto = Pieniadze.Zaokraglij(g.Sum(p => p.WartoscNetto));
                var vat = Pieniadze.Zaokraglij(netto * g.Key.Procent / 100m);
                return new PodsumowanieStawki(g.Key, netto, vat, netto + vat);
            })
            .ToList();

        var razemNetto = wgStawek.Sum(s => s.Netto);
        var razemVat = wgStawek.Sum(s => s.Vat);
        return new PodsumowanieFaktury(wgStawek, razemNetto, razemVat, razemNetto + razemVat);
    }

    /// <summary>
    /// Metoda "od brutto" (art. 106e ust. 7): KP = WB × SP / (100 + SP).
    /// Przydatna, gdy sprzedawca ustala ceny brutto (np. sklep detaliczny).
    /// </summary>
    public static decimal KwotaVatOdBrutto(decimal brutto, StawkaVat stawka) =>
        Pieniadze.Zaokraglij(brutto * stawka.Procent / (100m + stawka.Procent));
}
