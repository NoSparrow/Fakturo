namespace Faktury.Core.Model;

/// <summary>
/// Stawka VAT użyta w pozycji faktury.
///
/// DLACZEGO osobna klasa, a nie zwykła liczba (np. 23)?
/// Bo na fakturze występują też stawki "nieliczbowe": "zw" (zwolnione z VAT),
/// "np" (nie podlega opodatkowaniu) czy "oo" (odwrotne obciążenie). Ustawa o VAT
/// (art. 106e ust. 1 pkt 12–14) wymaga podania stawki ORAZ sum netto/VAT
/// z podziałem na poszczególne stawki – więc stawka musi być pełnoprawnym
/// obiektem, po którym można grupować.
/// </summary>
public sealed record StawkaVat
{
    /// <summary>Kod drukowany na fakturze, np. "23", "8", "zw".</summary>
    public string Kod { get; }

    /// <summary>Procent podatku. Dla stawek "zw"/"np" wynosi 0.</summary>
    public decimal Procent { get; }

    /// <summary>Czy stawka jest liczbowa (23/8/5/0) czy opisowa (zw/np).</summary>
    public bool JestLiczbowa { get; }

    private StawkaVat(string kod, decimal procent, bool jestLiczbowa)
    {
        Kod = kod;
        Procent = procent;
        JestLiczbowa = jestLiczbowa;
    }

    // Stawki obowiązujące w Polsce (stan: 2026).
    public static readonly StawkaVat Vat23 = new("23", 23m, true);
    public static readonly StawkaVat Vat8 = new("8", 8m, true);
    public static readonly StawkaVat Vat5 = new("5", 5m, true);
    public static readonly StawkaVat Vat0 = new("0", 0m, true);

    /// <summary>
    /// Sprzedaż zwolniona z VAT (np. podatnik korzystający ze zwolnienia podmiotowego
    /// z art. 113 ust. 1 ustawy o VAT – tzw. "nievatowiec"). Na fakturze trzeba wtedy
    /// podać PODSTAWĘ zwolnienia (art. 106e ust. 1 pkt 19) – patrz pole
    /// <see cref="Faktura.PodstawaZwolnienia"/>.
    /// </summary>
    public static readonly StawkaVat Zwolniona = new("zw", 0m, false);

    /// <summary>Czynność nie podlega opodatkowaniu w Polsce (np. usługa dla firmy z UE).</summary>
    public static readonly StawkaVat NiePodlega = new("np", 0m, false);

    public static IReadOnlyList<StawkaVat> Wszystkie { get; } =
        new[] { Vat23, Vat8, Vat5, Vat0, Zwolniona, NiePodlega };

    public static StawkaVat ZKodu(string kod) =>
        Wszystkie.FirstOrDefault(s => s.Kod.Equals(kod, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Nieznana stawka VAT: '{kod}'", nameof(kod));

    public override string ToString() => JestLiczbowa ? $"{Kod}%" : Kod;
}
