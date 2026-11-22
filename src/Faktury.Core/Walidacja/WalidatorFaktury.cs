using Faktury.Core.Model;

namespace Faktury.Core.Walidacja;

/// <summary>
/// Sprawdza, czy faktura zawiera wszystkie elementy wymagane przez art. 106e ustawy o VAT
/// oraz czy dane są spójne. Zwraca listę komunikatów (pusta lista = faktura poprawna).
///
/// DLACZEGO lista komunikatów, a nie wyjątek?
/// Użytkownik w formularzu chce zobaczyć WSZYSTKIE błędy naraz, a nie poprawiać je po jednym.
/// </summary>
public static class WalidatorFaktury
{
    public static IReadOnlyList<string> Sprawdz(Faktura f)
    {
        var bledy = new List<string>();

        if (string.IsNullOrWhiteSpace(f.Numer))
            bledy.Add("Brak numeru faktury (art. 106e ust. 1 pkt 2).");

        if (f.DataSprzedazy > f.DataWystawienia.AddMonths(1))
            bledy.Add("Data sprzedaży jest później niż miesiąc po dacie wystawienia – sprawdź daty.");

        if (f.TerminPlatnosci < f.DataWystawienia)
            bledy.Add("Termin płatności jest wcześniejszy niż data wystawienia.");

        // Sprzedawca – zawsze musi mieć NIP (wystawcą faktury jest podatnik).
        if (!WalidatorNip.JestPoprawny(f.Sprzedawca.Nip))
            bledy.Add("NIP sprzedawcy jest pusty lub ma błędną sumę kontrolną (art. 106e ust. 1 pkt 4).");

        // Nabywca – NIP opcjonalny (konsument), ale jeśli podany, to poprawny.
        if (f.Nabywca.Nip is not null && !WalidatorNip.JestPoprawny(f.Nabywca.Nip))
            bledy.Add("NIP nabywcy ma błędną sumę kontrolną (art. 106e ust. 1 pkt 5).");

        if (f.Pozycje.Count == 0)
            bledy.Add("Faktura nie ma żadnej pozycji (art. 106e ust. 1 pkt 7).");

        for (int i = 0; i < f.Pozycje.Count; i++)
        {
            var p = f.Pozycje[i];
            if (string.IsNullOrWhiteSpace(p.Nazwa)) bledy.Add($"Pozycja {i + 1}: brak nazwy towaru/usługi.");
            if (p.Ilosc <= 0) bledy.Add($"Pozycja {i + 1}: ilość musi być dodatnia.");
            if (p.CenaNetto < 0) bledy.Add($"Pozycja {i + 1}: cena netto nie może być ujemna.");
            if (p.RabatProcent is < 0 or > 100) bledy.Add($"Pozycja {i + 1}: rabat musi być w zakresie 0–100%.");
        }

        if (f.ZawieraPozycjeZwolnione && string.IsNullOrWhiteSpace(f.PodstawaZwolnienia))
            bledy.Add("Faktura zawiera pozycje 'zw', ale nie podano podstawy zwolnienia (art. 106e ust. 1 pkt 19).");

        return bledy;
    }
}
