using Faktury.Core.Model;
using Faktury.Core.Obliczenia;
using Faktury.Core.Walidacja;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Faktury.Pdf;

/// <summary>
/// Generuje wizualizację faktury (PDF, A4) z obiektu <see cref="Faktura"/>.
///
/// Układ dokumentu odpowiada typowej polskiej fakturze:
///  1. nagłówek: tytuł "Faktura nr ...", daty, miejsce wystawienia,
///  2. dwie kolumny: Sprzedawca / Nabywca,
///  3. tabela pozycji (Lp, nazwa, j.m., ilość, cena netto, wartość netto, VAT %, kwota VAT, brutto),
///  4. podsumowanie wg stawek VAT (wymóg art. 106e ust. 1 pkt 13–14),
///  5. razem do zapłaty + słownie, forma i termin płatności, numer konta,
///  6. stopka: podstawa zwolnienia (jeśli dotyczy), uwagi, podpisy (zwyczajowe, nieobowiązkowe).
///
/// Dlaczego PDF, a nie "drukowanie bezpośrednie"? Bo API drukowania jest różne na Windows
/// i Linux, a PDF (1) wygląda identycznie wszędzie, (2) można go wysłać mailem,
/// (3) każdy system ma przeglądarkę PDF, która drukuje. Drukowanie = otwarcie PDF
/// w systemowej przeglądarce lub wysłanie do drukarki (Windows: verb "print", Linux: `lp`).
/// </summary>
public sealed class FakturaPdf : IDocument
{
    private readonly Faktura _f;

    public FakturaPdf(Faktura faktura) => _f = faktura;

    public DocumentMetadata GetMetadata() => new()
    {
        Title = $"Faktura {_f.Numer}",
        Author = _f.Sprzedawca.Nazwa,
        Subject = "Faktura VAT",
        Creator = "Faktury (praca inżynierska)",
    };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily(GeneratorPdf.Czcionka));

            page.Header().Element(Naglowek);
            page.Content().Element(Tresc);
            page.Footer().AlignCenter().Text(t =>
            {
                t.Span("Strona ");
                t.CurrentPageNumber();
                t.Span(" z ");
                t.TotalPages();
            });
        });
    }

    // ---------------------------------------------------------------- nagłówek
    private void Naglowek(IContainer c)
    {
        c.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text($"Faktura nr {_f.Numer}").FontSize(18).Bold();
                if (_f.NumerKsef is not null)
                    col.Item().Text($"Numer KSeF: {_f.NumerKsef}").FontSize(8).FontColor(Colors.Grey.Darken1);
            });

            row.ConstantItem(220).Column(col =>
            {
                col.Item().Element(e => Para(e, "Data wystawienia:", _f.DataWystawienia.ToString("dd.MM.yyyy")));
                // Pkt 6: datę sprzedaży podaje się, jeśli różni się od daty wystawienia – my drukujemy zawsze, to dozwolone.
                col.Item().Element(e => Para(e, "Data sprzedaży:", _f.DataSprzedazy.ToString("dd.MM.yyyy")));
                if (_f.MiejsceWystawienia is not null)
                    col.Item().Element(e => Para(e, "Miejsce wystawienia:", _f.MiejsceWystawienia));
            });
        });
    }

    private static void Para(IContainer c, string etykieta, string wartosc) =>
        c.Row(r =>
        {
            r.RelativeItem().AlignRight().Text(etykieta).FontColor(Colors.Grey.Darken2);
            r.ConstantItem(90).AlignRight().Text(wartosc).Bold();
        });

    // ---------------------------------------------------------------- treść
    private void Tresc(IContainer c)
    {
        c.PaddingTop(15).Column(col =>
        {
            col.Spacing(12);
            col.Item().Element(Strony);
            col.Item().Element(TabelaPozycji);
            col.Item().Element(Podsumowanie);
            col.Item().Element(Stopka);
        });
    }

    private void Strony(IContainer c)
    {
        c.Row(row =>
        {
            row.Spacing(20);
            row.RelativeItem().Element(e => Kontrahent(e, "Sprzedawca", _f.Sprzedawca));
            row.RelativeItem().Element(e => Kontrahent(e, "Nabywca", _f.Nabywca));
        });
    }

    private static void Kontrahent(IContainer c, string tytul, Kontrahent k)
    {
        c.Column(col =>
        {
            col.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingBottom(2)
               .Text(tytul).FontSize(11).Bold();
            col.Item().PaddingTop(4).Text(k.Nazwa).Bold();
            col.Item().Text(k.Ulica);
            col.Item().Text($"{k.KodPocztowy} {k.Miejscowosc}");
            if (k.Nip is not null)
                col.Item().Text($"NIP: {WalidatorNip.Formatuj(k.Nip)}");
            if (k.Email is not null) col.Item().Text(k.Email).FontColor(Colors.Grey.Darken1);
        });
    }

    // ---------------------------------------------------------------- tabela pozycji
    private void TabelaPozycji(IContainer c)
    {
        c.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(22);   // Lp
                cols.RelativeColumn(4);    // Nazwa
                cols.ConstantColumn(32);   // j.m.
                cols.ConstantColumn(40);   // Ilość
                cols.ConstantColumn(62);   // Cena netto
                cols.ConstantColumn(66);   // Wartość netto
                cols.ConstantColumn(36);   // VAT %
                cols.ConstantColumn(60);   // Kwota VAT
                cols.ConstantColumn(66);   // Brutto
            });

            table.Header(h =>
            {
                foreach (var t in new[] { "Lp", "Nazwa towaru / usługi", "J.m.", "Ilość", "Cena netto", "Wartość netto", "VAT", "Kwota VAT", "Wartość brutto" })
                    h.Cell().Element(KomorkaNaglowka).Text(t);
            });

            int lp = 1;
            foreach (var p in _f.Pozycje)
            {
                table.Cell().Element(Komorka).Text(lp++.ToString());
                table.Cell().Element(Komorka).Text(txt =>
                {
                    txt.Span(p.Nazwa);
                    if (p.RabatProcent > 0) txt.Span($"  (rabat {p.RabatProcent:0.##}%)").FontColor(Colors.Grey.Darken1);
                });
                table.Cell().Element(Komorka).Text(p.Jednostka);
                table.Cell().Element(KomorkaLiczba).Text(p.Ilosc.ToString("0.##"));
                table.Cell().Element(KomorkaLiczba).Text(Kwota(p.CenaNettoPoRabacie));
                table.Cell().Element(KomorkaLiczba).Text(Kwota(p.WartoscNetto));
                table.Cell().Element(KomorkaLiczba).Text(p.Stawka.ToString());
                table.Cell().Element(KomorkaLiczba).Text(Kwota(p.KwotaVat));
                table.Cell().Element(KomorkaLiczba).Text(Kwota(p.WartoscBrutto));
            }
        });
    }

    private static IContainer KomorkaNaglowka(IContainer c) =>
        c.Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Medium)
         .Padding(3).DefaultTextStyle(x => x.SemiBold().FontSize(8));

    private static IContainer Komorka(IContainer c) =>
        c.Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(3);

    private static IContainer KomorkaLiczba(IContainer c) => Komorka(c).AlignRight();

    // ---------------------------------------------------------------- podsumowanie
    private void Podsumowanie(IContainer c)
    {
        var pods = _f.Podsumowanie;

        c.Row(row =>
        {
            row.Spacing(20);

            // Lewa: płatność
            row.RelativeItem().Column(col =>
            {
                col.Spacing(2);
                col.Item().Text("Płatność").FontSize(11).Bold();
                col.Item().Text($"Forma: {NazwaFormy(_f.FormaPlatnosci)}");
                col.Item().Text($"Termin: {_f.TerminPlatnosci:dd.MM.yyyy}");
                if (_f.Sprzedawca.NumerKonta is not null)
                    col.Item().Text($"Nr konta: {_f.Sprzedawca.NumerKonta}");
            });

            // Prawa: tabela wg stawek (pkt 13–14) i razem (pkt 15)
            row.ConstantItem(280).Column(col =>
            {
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(60);
                        cd.RelativeColumn();
                        cd.RelativeColumn();
                        cd.RelativeColumn();
                    });
                    t.Header(h =>
                    {
                        h.Cell().Element(KomorkaNaglowka).Text("Stawka");
                        h.Cell().Element(KomorkaNaglowka).AlignRight().Text("Netto");
                        h.Cell().Element(KomorkaNaglowka).AlignRight().Text("VAT");
                        h.Cell().Element(KomorkaNaglowka).AlignRight().Text("Brutto");
                    });
                    foreach (var s in pods.WgStawek)
                    {
                        t.Cell().Element(Komorka).Text(s.Stawka.ToString());
                        t.Cell().Element(KomorkaLiczba).Text(Kwota(s.Netto));
                        t.Cell().Element(KomorkaLiczba).Text(Kwota(s.Vat));
                        t.Cell().Element(KomorkaLiczba).Text(Kwota(s.Brutto));
                    }
                    t.Cell().Element(Komorka).Text("Razem").Bold();
                    t.Cell().Element(KomorkaLiczba).Text(Kwota(pods.RazemNetto)).Bold();
                    t.Cell().Element(KomorkaLiczba).Text(Kwota(pods.RazemVat)).Bold();
                    t.Cell().Element(KomorkaLiczba).Text(Kwota(pods.RazemBrutto)).Bold();
                });

                col.Item().PaddingTop(8).AlignRight()
                   .Text($"Do zapłaty: {Pieniadze.Formatuj(pods.RazemBrutto, _f.Waluta)}").FontSize(13).Bold();
                col.Item().AlignRight()
                   .Text($"Słownie: {KwotaSlownie.Zamien(pods.RazemBrutto)}").FontSize(8).Italic();
            });
        });
    }

    // ---------------------------------------------------------------- stopka
    private void Stopka(IContainer c)
    {
        c.Column(col =>
        {
            col.Spacing(6);
            if (_f.ZawieraPozycjeZwolnione && _f.PodstawaZwolnienia is not null)
                col.Item().Text($"Podstawa zwolnienia z VAT: {_f.PodstawaZwolnienia}").FontSize(8);
            if (_f.Uwagi is not null)
                col.Item().Text($"Uwagi: {_f.Uwagi}").FontSize(8);

            col.Item().PaddingTop(30).Row(r =>
            {
                r.RelativeItem().AlignCenter().Column(cc =>
                {
                    cc.Item().BorderTop(0.5f).PaddingTop(2).AlignCenter()
                      .Text("Osoba upoważniona do wystawienia").FontSize(7).FontColor(Colors.Grey.Darken1);
                });
                r.ConstantItem(80);
                r.RelativeItem().AlignCenter().Column(cc =>
                {
                    cc.Item().BorderTop(0.5f).PaddingTop(2).AlignCenter()
                      .Text("Osoba upoważniona do odbioru").FontSize(7).FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }

    private static string Kwota(decimal k) =>
        k.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("pl-PL")).Replace('\u00A0', ' ');

    private static string NazwaFormy(FormaPlatnosci f) => f switch
    {
        FormaPlatnosci.Przelew => "przelew",
        FormaPlatnosci.Gotowka => "gotówka",
        FormaPlatnosci.Karta => "karta płatnicza",
        FormaPlatnosci.Blik => "BLIK",
        _ => f.ToString(),
    };
}
