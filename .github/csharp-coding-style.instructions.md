---
applyTo: "**/*.cs"
description: "Dla kodu C# w CicdLight stosuj konwencje repozytorium dotyczące nazewnictwa, enumów, stałych, komentarzy i organizacji plików."
---

## Instrukcje stylu kodu C# (C# Coding Style)

- Używaj angielskich nazw dla klas, interfejsów, enumów, metod, parametrów, właściwości i zmiennych lokalnych.
- Nazwy powinny opisywać przechowywaną wartość lub zachowanie. Kolekcje, takie jak listy, tablice i słowniki, powinny mieć nazwy w liczbie mnogiej.
- Nazwy klas, interfejsów i enumów powinny być w liczbie pojedynczej.
- Każda klasa, interfejs, enum, record i struct musi znajdować się w osobnym pliku, którego nazwa odpowiada nazwie typu. Nie umieszczaj wielu typów w jednym pliku, także gdy są ze sobą powiązane lub małe (np. DTO, modele, request/response). Wyjątkiem są typy zagnieżdżone (nested types) oraz wersje generyczne i niegeneryczne tego samego typu. Istniejące typy zebrane w `Models.cs` przenoś do osobnych plików przy okazji ich modyfikacji.
- Stosuj standardowe konwencje C# PascalCase i camelCase. Pola prywatne powinny zaczynać się od `_`.
- Stałe współdzielone między klasami umieszczaj w folderze `Consts` (np. `Consts/Icons.cs`). Przed zdefiniowaniem nowej stałej sprawdź, czy już tam nie istnieje, i używaj jej zamiast powielać wartość lub hardkodować literał. Lokalne stałe pozostaw w klasie, która ich używa, tylko gdy nie mają znaczenia poza nią.
- Nie hardkoduj adresów URL, nazw nagłówków ani wersji API w wielu miejscach; trzymaj je jako stałe w klasie klienta integracji (np. `Api` w `AzureDevOpsClient`).
- Dodawaj komentarze po angielsku tylko wtedy, gdy sama nazwa nie wyjaśnia intencji lub gdy złożony blok zyskuje na krótkim wyjaśnieniu.
- Stosuj wskazówki analizatorów (Visual Studio / ReSharper), gdy poprawiają spójność i czytelność, w tym usuwanie nieużywanych dyrektyw `using`, nieużywanych zmiennych i zbędnych pustych linii.

## Reguły dla enumów (Enum Rules)

- Enumy, których wartości są zapisywane w ustawieniach użytkownika (np. w `AppSettings` / pliku JSON), powinny mieć jawne wartości liczbowe i zaczynać się od `1`, aby zmiana kolejności nie zmieniła znaczenia zapisanych danych.
- Domyślnie unikaj członków `NotSet` lub równoważnych oznaczających brak wartości.
- Enumy używane wyłącznie w kodzie, które nie są zapisywane, mogą pomijać jawne wartości liczbowe.

## Formatowanie i składnia (Formatting)

- Namespace file-scoped (`namespace CicdLight;`).
- Wcięcia: 4 spacje; nawiasy klamrowe w stylu Allman (nowa linia).
- Zawsze używaj klamr w `if`, `else`, `for`, `foreach`, `while`, `using` i `lock`, także dla jednej instrukcji; nie stosuj jednolinijkowców typu `if (x) return;` ani `foreach (...) DoSomething();` - klamry i ciało w osobnych liniach.
- Pola prywatne: `_camelCase`; stałe i typy: `PascalCase`; parametry i zmienne lokalne: `camelCase`.
- Preferuj `var`, gdy typ wynika z prawej strony.
- Preferuj `record` dla modeli danych, `sealed` dla klas bez dziedziczenia.
- Używaj collection expressions (`[]`) i wyrażeń `=>` dla krótkich metod.

## Asynchroniczność i wyjątki (Async And Exceptions)

- Operacje I/O są `async` i przyjmują `CancellationToken ct`; nie używaj `.Result` / `.Wait()` poza zadaniami już zakończonymi (po `Task.WhenAll`).
- Nie połykaj wyjątków bez powodu; łap konkretne typy (np. `HttpRequestException`) i dodaj krótki komentarz, dlaczego.
- Komentarze tylko tam, gdzie wyjaśniają nieoczywistą decyzję; bez komentarzy opisujących oczywisty kod.
- Minimalne zmiany: nie refaktoryzuj kodu niezwiązanego z zadaniem.

## Organizacja plików (File Organization)

- Pliki dziel według odpowiedzialności i umieszczaj w folderach:
  - `Integrations/` - klienci integracji (`IIntegrationClient`, `AzureDevOpsClient`, `GitHubActionsClient`),
  - `Models/` - modele danych i rekordy (`Snapshot`, `BuildInfo`, `PullRequestInfo`, ...),
  - `Enums/` - enumy,
  - `Consts/` - stałe współdzielone,
  - `Tray/` - logika ikony w zasobniku (`TrayController`),
  - `Windows/` - okna i kontrolki WinUI (popup, ustawienia, edytory),
  - `Settings/` - ustawienia aplikacji i ich persystencja (`AppSettings`).
- Namespace pozostaje `CicdLight` niezależnie od folderu (bez namespace per folder).
- Nie twórz plików zbiorczych (`Models.cs`, `Helpers.cs`, `Utils.cs`); nowy typ trafia do własnego pliku w odpowiednim folderze.
- Duże klasy UI dziel na pliki `partial` tylko wtedy, gdy rozdzielenie odpowiedzialności poprawia czytelność.
- Przeniesienia plików wykonuj w osobnym commicie (`refactor: ...`), bez zmian logiki.
