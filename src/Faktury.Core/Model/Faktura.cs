using Faktury.Core.Obliczenia;

namespace Faktury.Core.Model;

/// <summary>Rodzaj dokumentu. W tej wersji obsługujemy tylko fakturę podstawową (VAT).</summary>
public enum RodzajFaktury
{
    /// <summary>Zwykła faktura sprzedaży ("FA" w KSeF – RodzajFaktury = VAT).</summary>
    Vat,
    // Przyszłe przyrosty: Korygujaca, Zaliczkowa, Proforma (pro forma NIE jest fakturą w rozumieniu ustawy).
}

public enum FormaPlatnosci
{
    Przelew,
    Gotowka,
    Karta,
    Blik,
}

/// <summary>
/// Faktura – główna encja systemu.
///
/// Mapowanie na ustawę o VAT (art. 106e ust. 1):
///  pkt 1 – <see cref="DataWystawienia"/>
///  pkt 2 – <see cref="Numer"/> (kolejny, jednoznaczny, w ramach serii)
///  pkt 3–5 – <see cref="Sprzedawca"/>, <see cref="Nabywca"/>
///  pkt 6 – <see cref="DataSprzedazy"/> (jeśli różna od daty wystawienia)
///  pkt 7–12 – <see cref="Pozycje"/>
///  pkt 13–15 – <see cref="Podsumowanie"/> (sumy wg stawek i kwota należności ogółem)
///  pkt 19 – <see cref="PodstawaZwolnienia"/> (tylko gdy stawka "zw")
/// </summary>
public sealed class Faktura
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public RodzajFaktury Rodzaj { get; init; } = RodzajFaktury.Vat;

    /// <summary>Numer faktury, np. "FV/2026/10/0001".</summary>
    public required string Numer { get; init; }

    public required DateOnly DataWystawienia { get; init; }

    /// <summary>Data dokonania dostawy / wykonania usługi. Gdy równa dacie wystawienia, można jej nie drukować.</summary>
    public required DateOnly DataSprzedazy { get; init; }

    /// <summary>Miejsce wystawienia – zwyczajowe, nie wymagane ustawą.</summary>
    public string? MiejsceWystawienia { get; init; }

    public required Kontrahent Sprzedawca { get; init; }
    public required Kontrahent Nabywca { get; init; }

    public List<PozycjaFaktury> Pozycje { get; init; } = new();

    public FormaPlatnosci FormaPlatnosci { get; init; } = FormaPlatnosci.Przelew;
    public required DateOnly TerminPlatnosci { get; init; }

    /// <summary>Waluta – w MVP zawsze PLN (kwoty VAT muszą być w złotych – art. 106e ust. 11).</summary>
    public string Waluta { get; init; } = "PLN";

    /// <summary>
    /// Podstawa prawna zwolnienia z VAT – wymagana, jeśli jakakolwiek pozycja ma stawkę "zw"
    /// (art. 106e ust. 1 pkt 19), np. "art. 113 ust. 1 ustawy o VAT".
    /// </summary>
    public string? PodstawaZwolnienia { get; init; }

    public string? Uwagi { get; init; }

    /// <summary>Numer nadany przez KSeF po przyjęciu faktury. Null = faktura nie była wysłana (etap przyszły).</summary>
    public string? NumerKsef { get; set; }

    /// <summary>Podsumowanie kwot – liczone na żądanie z pozycji (jedno źródło prawdy).</summary>
    public PodsumowanieFaktury Podsumowanie => KalkulatorVat.Podsumuj(Pozycje);

    /// <summary>Czy faktura zawiera pozycje zwolnione z VAT (wtedy wymagana podstawa zwolnienia).</summary>
    public bool ZawieraPozycjeZwolnione => Pozycje.Any(p => p.Stawka == StawkaVat.Zwolniona);
}
