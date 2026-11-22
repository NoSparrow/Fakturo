namespace Faktury.Core.Obliczenia;

/// <summary>
/// Pomocnicze operacje na kwotach pieniężnych.
///
/// Art. 106e ust. 11 ustawy o VAT: "Kwoty wykazywane w fakturze zaokrągla się do pełnych
/// groszy, przy czym końcówki poniżej 0,5 grosza pomija się, a końcówki od 0,5 grosza
/// zaokrągla się do 1 grosza."
///
/// UWAGA – PUŁAPKA: domyślne <c>Math.Round(x, 2)</c> w .NET używa trybu
/// <c>MidpointRounding.ToEven</c> (tzw. zaokrąglanie bankowe): 0.125 -> 0.12, 0.135 -> 0.14.
/// Ustawa każe zaokrąglać "od 0,5 grosza w górę", czyli <c>MidpointRounding.AwayFromZero</c>.
/// Dlatego NIGDY nie używamy gołego Math.Round do kwot – tylko tej metody.
/// </summary>
public static class Pieniadze
{
    public static decimal Zaokraglij(decimal kwota) =>
        Math.Round(kwota, 2, MidpointRounding.AwayFromZero);

    /// <summary>Format "1 234,56 zł" zgodny z polskimi zwyczajami.</summary>
    public static string Formatuj(decimal kwota, string waluta = "zł")
    {
        var pl = System.Globalization.CultureInfo.GetCultureInfo("pl-PL");
        // W pl-PL separatorem tysięcy jest spacja nierozdzielająca (U+00A0) – zamieniamy na zwykłą dla czytelności w PDF.
        var tekst = kwota.ToString("N2", pl).Replace('\u00A0', ' ');
        return $"{tekst} {waluta}";
    }
}
