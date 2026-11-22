# ADR-001: Wybór stosu technologicznego

- **Status:** zaakceptowana (2026-10-09, L1)
- **Decydenci:** Osoba A, Osoba B - za projekt odpowiada jedna osoba i pełni obie role
- **Kontekst:** Program ma działać na Windows 11 i Linux z jednej bazy kodu, zespół zna C#, projekt będzie rozwijany jako praca inżynierska z integracją KSeF; 1–2 osoby, ok. 12 tygodni na MVP.

## Decyzja
| Warstwa | Wybór | Alternatywy odrzucone | Powód |
|---|---|---|---|
| Język / platforma | C# 12, .NET 8 LTS (migracja do .NET 10 LTS na starcie P1) | Java, Python, TypeScript | Znajomość C#; oficjalny klient KSeF w C#; .NET wieloplatformowy |
| UI | Avalonia UI 11 (MVVM, CommunityToolkit.Mvvm) | .NET MAUI (brak Linux), WPF/WinForms (tylko Windows), Uno (mniejsza społeczność desktop), Electron (inny język) | Jedyny dojrzały framework desktop .NET z Tier-1 Linux; XAML podobny do WPF |
| Plan B dla UI | ASP.NET Core Blazor (lokalnie, UI w przeglądarce) | — | Jeśli Avalonia zablokuje P1; warstwa Core bez zmian |
| Dane | SQLite przez EF Core (migracje) | PostgreSQL/MySQL (wymagają serwera), pliki JSON (brak transakcji/zapytań) | Jeden plik, zero instalacji, transakcje, przenośność |
| PDF | QuestPDF (Community) | iText (AGPL/komercyjna), HTML→PDF przez przeglądarkę (zależność od Chromium), MigraDoc | Natywny C#, Skia, identyczny render, licencja odpowiednia dla projektu niekomercyjnego; `Settings.License = Community` ustawione w kodzie |
| Czcionka | Lato (OFL 1.1) osadzona jako zasób | czcionki systemowe | Polskie znaki i identyczny wygląd na obu systemach |
| Drukowanie | PDF → systemowa przeglądarka / `print` (Windows) / `lp` CUPS (Linux) | natywne API drukowania | Brak wspólnego API drukowania w .NET dla obu systemów |
| KSeF | `IKsefClient` + `SymulatorKsef` + `KsefApiClient` na bibliotece `KSeF.Client` (MF, MIT); XML FA(3) przez `System.Xml.Serialization` + walidacja XSD | własna implementacja XAdES/REST | Nie implementować kryptografii samemu; symulator zapewnia demonstrowalność offline |
| Testy | xUnit + coverlet | NUnit, MSTest | Standard w .NET, już użyte w P0 |
| Diagramy | PlantUML (źródła w repo) + eksport PNG/SVG | draw.io, StarUML | Tekstowe, wersjonowalne, zalecane w instrukcji |
| Repozytorium / zadania / CI | GitHub, Issues + Projects (Kanban), Actions (`dotnet build/test`) | GitLab, Trello, Jira | Wymóg instrukcji; bezpłatne dla studentów |
| Dokumentacja | Markdown w `docs/` + eksport `pandoc` do PDF/DOCX na kamienie milowe | Google Docs, Word | Wersjonowanie razem z kodem; instrukcja dopuszcza Markdown |

## Konsekwencje
- (+) Jedna baza kodu, przenośność potwierdzona prototypem P0 (PDF wygenerowany na Linuxie z polskimi znakami).
- (+) Core bez zależności → testowalność i swoboda zmiany UI.
- (−) Avalonia to nowa biblioteka dla zespołu → ryzyko R-02; mitygacja: „Hello Avalonia” przed L2.
- (−) Dwie implementacje KSeF do utrzymania; akceptowalne, bo symulator jest prosty.
- Licencje do wymienienia w pracy: Avalonia (MIT), EF Core (MIT), SQLite (public domain), QuestPDF (Community), KSeF.Client (MIT), Lato (OFL), xUnit (Apache 2.0).
