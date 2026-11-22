namespace Faktury.Core.Obliczenia;

/// <summary>
/// Zamienia kwotę na zapis słowny, np. 1234,56 -> "jeden tysiąc dwieście trzydzieści cztery złote 56/100".
///
/// Zapis słowny NIE jest wymagany ustawą o VAT, ale jest powszechnym zwyczajem na polskich
/// fakturach (utrudnia sfałszowanie kwoty). Pokazuje też ciekawy problem programistyczny:
/// polska odmiana liczebników ("1 tysiąc", "2 tysiące", "5 tysięcy", "22 tysiące", "12 tysięcy").
/// </summary>
public static class KwotaSlownie
{
    private static readonly string[] Jednosci =
        { "", "jeden", "dwa", "trzy", "cztery", "pięć", "sześć", "siedem", "osiem", "dziewięć" };
    private static readonly string[] Nascie =
        { "dziesięć", "jedenaście", "dwanaście", "trzynaście", "czternaście", "piętnaście",
          "szesnaście", "siedemnaście", "osiemnaście", "dziewiętnaście" };
    private static readonly string[] Dziesiatki =
        { "", "", "dwadzieścia", "trzydzieści", "czterdzieści", "pięćdziesiąt",
          "sześćdziesiąt", "siedemdziesiąt", "osiemdziesiąt", "dziewięćdziesiąt" };
    private static readonly string[] Setki =
        { "", "sto", "dwieście", "trzysta", "czterysta", "pięćset",
          "sześćset", "siedemset", "osiemset", "dziewięćset" };

    // Formy: [pojedyncza, mnoga 2-4, mnoga 5+]
    private static readonly string[][] Rzedy =
    {
        new[] { "", "", "" },
        new[] { "tysiąc", "tysiące", "tysięcy" },
        new[] { "milion", "miliony", "milionów" },
        new[] { "miliard", "miliardy", "miliardów" },
    };

    public static string Zamien(decimal kwota, string walutaSlownie = "złoty")
    {
        if (kwota < 0) throw new ArgumentOutOfRangeException(nameof(kwota), "Kwota nie może być ujemna.");
        kwota = Pieniadze.Zaokraglij(kwota);

        long zlote = (long)Math.Floor(kwota);
        int grosze = (int)((kwota - zlote) * 100);

        string slowa = zlote == 0 ? "zero" : LiczbaSlownie(zlote);
        string waluta = OdmienWalute(zlote, walutaSlownie);

        return $"{slowa} {waluta} {grosze:D2}/100";
    }

    private static string LiczbaSlownie(long liczba)
    {
        var czesci = new List<string>();
        int rzad = 0;
        while (liczba > 0)
        {
            int grupa = (int)(liczba % 1000);
            if (grupa > 0)
            {
                string tekst = GrupaSlownie(grupa);
                if (rzad > 0)
                {
                    // "jeden tysiąc" zwyczajowo skraca się do "tysiąc"
                    if (grupa == 1) tekst = "";
                    tekst = (tekst + " " + Rzedy[rzad][FormaMnoga(grupa)]).Trim();
                }
                czesci.Insert(0, tekst);
            }
            liczba /= 1000;
            rzad++;
        }
        return string.Join(" ", czesci);
    }

    private static string GrupaSlownie(int n)
    {
        var s = new List<string>();
        if (n >= 100) s.Add(Setki[n / 100]);
        int reszta = n % 100;
        if (reszta >= 10 && reszta < 20) s.Add(Nascie[reszta - 10]);
        else
        {
            if (reszta >= 20) s.Add(Dziesiatki[reszta / 10]);
            if (reszta % 10 > 0) s.Add(Jednosci[reszta % 10]);
        }
        return string.Join(" ", s);
    }

    /// <summary>0 = pojedyncza (1), 1 = mnoga 2–4 (ale nie 12–14), 2 = pozostałe.</summary>
    private static int FormaMnoga(long n)
    {
        if (n == 1) return 0;
        long dz = n % 100, j = n % 10;
        if (j >= 2 && j <= 4 && (dz < 12 || dz > 14)) return 1;
        return 2;
    }

    private static string OdmienWalute(long n, string podstawa) => podstawa switch
    {
        "złoty" => FormaMnoga(n) switch { 0 => "złoty", 1 => "złote", _ => "złotych" },
        "euro" => "euro",
        _ => podstawa,
    };
}
