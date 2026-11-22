namespace Faktury.Core.Walidacja;

/// <summary>
/// Sprawdza poprawność formalną numeru NIP (suma kontrolna).
///
/// NIP ma 10 cyfr. Ostatnia (10.) cyfra jest cyfrą kontrolną liczoną z pierwszych 9:
///   suma = c1*6 + c2*5 + c3*7 + c4*2 + c5*3 + c6*4 + c7*5 + c8*6 + c9*7
///   cyfra kontrolna = suma mod 11   (wynik 10 jest niedozwolony – taki NIP nie istnieje)
///
/// To jest WALIDACJA FORMALNA – mówi tylko, że numer "może istnieć". Czy firma o takim
/// NIP naprawdę istnieje i jest czynnym podatnikiem VAT, sprawdza się w Wykazie
/// podatników VAT (tzw. "biała lista") – API Ministerstwa Finansów. To zadanie na późniejszy
/// przyrost.
/// </summary>
public static class WalidatorNip
{
    private static readonly int[] Wagi = { 6, 5, 7, 2, 3, 4, 5, 6, 7 };

    /// <summary>Usuwa myślniki, spacje i prefiks "PL".</summary>
    public static string Normalizuj(string nip)
    {
        var s = nip.Trim().ToUpperInvariant();
        if (s.StartsWith("PL")) s = s[2..];
        return new string(s.Where(char.IsDigit).ToArray());
    }

    public static bool JestPoprawny(string? nip)
    {
        if (string.IsNullOrWhiteSpace(nip)) return false;
        var cyfry = Normalizuj(nip);
        if (cyfry.Length != 10) return false;

        int suma = 0;
        for (int i = 0; i < 9; i++)
            suma += (cyfry[i] - '0') * Wagi[i];

        int kontrolna = suma % 11;
        if (kontrolna == 10) return false;

        return kontrolna == cyfry[9] - '0';
    }

    /// <summary>Format do druku: 123-456-78-90 (zwyczajowy; ustawa formatu nie narzuca).</summary>
    public static string Formatuj(string nip)
    {
        var c = Normalizuj(nip);
        return c.Length == 10 ? $"{c[..3]}-{c[3..6]}-{c[6..8]}-{c[8..]}" : nip;
    }
}
