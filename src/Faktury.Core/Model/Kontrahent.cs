namespace Faktury.Core.Model;

/// <summary>
/// Strona transakcji – sprzedawca (wystawca) albo nabywca.
///
/// Art. 106e ust. 1 pkt 3–5 ustawy o VAT wymaga na fakturze:
///  - imion i nazwisk lub nazw podatnika i nabywcy,
///  - ich adresów,
///  - NIP sprzedawcy oraz NIP nabywcy (jeśli nabywca jest podatnikiem).
/// Dla konsumenta (osoby prywatnej) NIP nabywcy nie występuje – stąd pole jest opcjonalne.
/// </summary>
public sealed class Kontrahent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Nazwa firmy albo imię i nazwisko.</summary>
    public required string Nazwa { get; init; }

    /// <summary>NIP (10 cyfr, bez myślników). Null dla osoby prywatnej.</summary>
    public string? Nip { get; init; }

    public required string Ulica { get; init; }
    public required string KodPocztowy { get; init; }
    public required string Miejscowosc { get; init; }
    public string Kraj { get; init; } = "Polska";

    // Dane nieobowiązkowe, ale praktyczne (kontakt, przelew).
    public string? Email { get; init; }
    public string? Telefon { get; init; }
    public string? NumerKonta { get; init; }

    /// <summary>Adres w jednej linii – wygodne przy drukowaniu.</summary>
    public string AdresJednaLinia => $"{Ulica}, {KodPocztowy} {Miejscowosc}";
}
