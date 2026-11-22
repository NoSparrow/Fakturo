namespace Faktury.Core.Numeracja;

/// <summary>
/// Nadaje kolejne numery faktur.
///
/// Art. 106e ust. 1 pkt 2: numer ma być "kolejny, nadany w ramach jednej lub więcej serii,
/// który w sposób jednoznaczny identyfikuje fakturę". Przepis NIE narzuca formatu – ale
/// w praktyce przyjmuje się numerację ciągłą w obrębie miesiąca lub roku, bez "dziur"
/// (brakujący numer to pytanie przy kontroli skarbowej: "gdzie jest faktura nr 7?").
///
/// Domyślny format: FV/RRRR/MM/NNNN, np. FV/2026/10/0001 – seria resetuje się co miesiąc.
///
/// W pełnej aplikacji licznik będzie trzymany w bazie danych (tabela NumeracjaFaktur),
/// a nadanie numeru musi być operacją atomową (transakcja), żeby dwa równoległe zapisy
/// nie dostały tego samego numeru. Tutaj – prosta wersja w pamięci do prototypu i testów.
/// </summary>
public sealed class GeneratorNumerow
{
    private readonly Dictionary<string, int> _liczniki = new();
    private readonly string _prefiks;

    public GeneratorNumerow(string prefiks = "FV") => _prefiks = prefiks;

    public string Nastepny(DateOnly dataWystawienia)
    {
        var klucz = $"{dataWystawienia.Year:D4}/{dataWystawienia.Month:D2}";
        _liczniki.TryGetValue(klucz, out var ostatni);
        var nowy = ostatni + 1;
        _liczniki[klucz] = nowy;
        return $"{_prefiks}/{klucz}/{nowy:D4}";
    }

    /// <summary>Ustawia stan licznika (np. po wczytaniu z bazy ostatniego użytego numeru).</summary>
    public void UstawLicznik(int rok, int miesiac, int ostatniNumer) =>
        _liczniki[$"{rok:D4}/{miesiac:D2}"] = ostatniNumer;
}
