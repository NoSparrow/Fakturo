using Faktury.Core.Model;
using Faktury.Core.Numeracja;
using Faktury.Core.Obliczenia;
using Faktury.Core.Walidacja;
using Faktury.Pdf;

// =====================================================================================
//  DEMO PROTOTYPU "Faktury" - generowanie faktury w PDF z poziomu konsoli.
//  WSZYSTKIE DANE SĄ FIKCYJNE. NIP-y mają poprawną sumę kontrolną, ale nie należą
//  do żadnych realnych firm (zgodnie z zaleceniem MF: do testów używać losowych NIP).
// =====================================================================================

// Formatowanie liczb po polsku (1 234,56) niezależnie od ustawień systemu.
System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("pl-PL");

string katalog = ArgumentPo("--katalog") ?? Path.Combine(Directory.GetCurrentDirectory(), "wyjscie");
bool otworz = args.Contains("--otworz");
bool drukuj = args.Contains("--drukuj");

// 1. Kontrahenci --------------------------------------------------------------------
var sprzedawca = new Kontrahent
{
    Nazwa = "Mega Śrubka Jan Kowalski",
    Nip = "5261040828",            // fikcyjny, poprawna suma kontrolna
    Ulica = "ul. Radomska 12",
    KodPocztowy = "27-200",
    Miejscowosc = "Starachowice",
    Email = "biuro@mega-srubka.example",
    NumerKonta = "PL 00 1234 5678 0000 0000 1234 5678",
};

var nabywca = new Kontrahent
{
    Nazwa = "Hurtownia Budowlana BUDMAX Sp. z o.o.",
    Nip = "7010001453",            // fikcyjny, poprawna suma kontrolna
    Ulica = "ul. Przemysłowa 3",
    KodPocztowy = "25-001",
    Miejscowosc = "Kielce",
};

// 2. Numer faktury ------------------------------------------------------------------
var dzis = DateOnly.FromDateTime(DateTime.Today);
var numeracja = new GeneratorNumerow("FV");
numeracja.UstawLicznik(dzis.Year, dzis.Month, ostatniNumer: 6); // udajemy, że w tym miesiącu było już 6 faktur

// 3. Faktura z pozycjami w różnych stawkach -----------------------------------------
var faktura = new Faktura
{
    Numer = numeracja.Nastepny(dzis),
    DataWystawienia = dzis,
    DataSprzedazy = dzis.AddDays(-2),
    TerminPlatnosci = dzis.AddDays(14),
    MiejsceWystawienia = "Starachowice",
    Sprzedawca = sprzedawca,
    Nabywca = nabywca,
    FormaPlatnosci = FormaPlatnosci.Przelew,
    Pozycje =
    {
        new PozycjaFaktury { Nazwa = "Śruba M8 x 40", Jednostka = "kg", Ilosc = 2.31m, CenaNetto = 40.00m, Stawka = StawkaVat.Vat23 },
        new PozycjaFaktury { Nazwa = "Rury PCV", Jednostka = "m", Ilosc = 250, CenaNetto = 3.19m, Stawka = StawkaVat.Vat23 },
        new PozycjaFaktury { Nazwa = "Nakrętka M8", Jednostka = "szt.", Ilosc = 500, CenaNetto = 0.99m, RabatProcent = 10, Stawka = StawkaVat.Vat23 },
        new PozycjaFaktury { Nazwa = "Usługa montażowa", Jednostka = "usł.", Ilosc = 1, CenaNetto = 350.00m, Stawka = StawkaVat.Vat23 },
        new PozycjaFaktury { Nazwa = "Dostawa materiałów (transport)", Jednostka = "usł.", Ilosc = 1, CenaNetto = 120.00m, Stawka = StawkaVat.Vat8 },
    },
    Uwagi = "Dokument wygenerowany prototypem programu „Faktury” – dane fikcyjne, wyłącznie do celów dydaktycznych.",
};

// 4. Walidacja ----------------------------------------------------------------------
var bledy = WalidatorFaktury.Sprawdz(faktura);
if (bledy.Count > 0)
{
    Console.Error.WriteLine("Faktura zawiera błędy:");
    foreach (var b in bledy) Console.Error.WriteLine(" - " + b);
    return 1;
}

// 5. Podsumowanie w konsoli ---------------------------------------------------------
var pods = faktura.Podsumowanie;
Console.WriteLine($"Faktura {faktura.Numer}  ({faktura.DataWystawienia:dd.MM.yyyy})");
Console.WriteLine($"Sprzedawca: {sprzedawca.Nazwa}, NIP {WalidatorNip.Formatuj(sprzedawca.Nip!)}");
Console.WriteLine($"Nabywca:    {nabywca.Nazwa}, NIP {WalidatorNip.Formatuj(nabywca.Nip!)}");
Console.WriteLine();
Console.WriteLine($"{"Stawka",-8}{"Netto",14}{"VAT",12}{"Brutto",14}");
foreach (var s in pods.WgStawek)
    Console.WriteLine($"{s.Stawka,-8}{s.Netto,14:N2}{s.Vat,12:N2}{s.Brutto,14:N2}");
Console.WriteLine($"{"RAZEM",-8}{pods.RazemNetto,14:N2}{pods.RazemVat,12:N2}{pods.RazemBrutto,14:N2}");
Console.WriteLine();
Console.WriteLine("Słownie: " + KwotaSlownie.Zamien(pods.RazemBrutto));

// 6. PDF ----------------------------------------------------------------------------
var sciezka = GeneratorPdf.ZapiszDoPliku(faktura, katalog);
Console.WriteLine($"\nZapisano PDF: {sciezka}");

if (otworz) GeneratorPdf.OtworzWSystemie(sciezka);
if (drukuj) GeneratorPdf.Drukuj(sciezka);

return 0;

// ---------------------------------------------------------------------------------
string? ArgumentPo(string nazwa)
{
    int i = Array.IndexOf(args, nazwa);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}
