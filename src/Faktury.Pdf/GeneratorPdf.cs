using System.Diagnostics;
using System.Runtime.InteropServices;
using Faktury.Core.Model;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Faktury.Pdf;

/// <summary>Fasada: zapis do pliku, bajtów oraz drukowanie w sposób niezależny od systemu.</summary>
public static class GeneratorPdf
{
    /// <summary>Nazwa rodziny czcionek używanej w dokumentach.</summary>
    public const string Czcionka = "Lato";

    static GeneratorPdf()
    {
        // Wymagane przez QuestPDF – deklaracja, z której licencji korzystamy.
        // Community: osoby prywatne, projekty open source, firmy < 1 mln USD przychodu.
        QuestPDF.Settings.License = LicenseType.Community;
        ZarejestrujCzcionki();
    }

    /// <summary>
    /// Rejestruje osadzoną czcionkę Lato. DLACZEGO? Bo "Arial" istnieje na Windows, ale nie na
    /// Linuxie – PDF wyglądałby inaczej lub brakowałoby polskich znaków. Osadzona czcionka
    /// = ten sam wygląd wszędzie.
    /// </summary>
    private static void ZarejestrujCzcionki()
    {
        var asm = typeof(GeneratorPdf).Assembly;
        foreach (var nazwa in asm.GetManifestResourceNames().Where(n => n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)))
        {
            using var strumien = asm.GetManifestResourceStream(nazwa)!;
            QuestPDF.Drawing.FontManager.RegisterFontFromStream(strumien);
        }
    }

    public static byte[] DoBajtow(Faktura f) => new FakturaPdf(f).GeneratePdf();

    public static string ZapiszDoPliku(Faktura f, string katalog)
    {
        Directory.CreateDirectory(katalog);
        // Numer faktury zawiera "/", którego nie wolno użyć w nazwie pliku.
        var nazwa = "Faktura_" + f.Numer.Replace('/', '_') + ".pdf";
        var sciezka = Path.Combine(katalog, nazwa);
        new FakturaPdf(f).GeneratePdf(sciezka);
        return sciezka;
    }

    /// <summary>
    /// Otwiera PDF w domyślnej przeglądarce systemu (użytkownik drukuje z niej Ctrl+P).
    /// Najbardziej niezawodny i przenośny sposób "drukowania".
    /// </summary>
    public static void OtworzWSystemie(string sciezkaPdf)
    {
        Process.Start(new ProcessStartInfo(sciezkaPdf) { UseShellExecute = true });
    }

    /// <summary>
    /// Wysyła PDF bezpośrednio do domyślnej drukarki.
    ///  - Windows: czasownik powłoki "print" (używa zarejestrowanej aplikacji do PDF),
    ///  - Linux/macOS: polecenie CUPS `lp` (działa, jeśli skonfigurowano drukarkę w systemie).
    /// </summary>
    public static void Drukuj(string sciezkaPdf)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo(sciezkaPdf) { UseShellExecute = true, Verb = "print" });
        }
        else
        {
            Process.Start(new ProcessStartInfo("lp", $"\"{sciezkaPdf}\"") { UseShellExecute = false });
        }
    }
}
