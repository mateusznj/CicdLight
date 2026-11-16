# Copilot Instructions - CicdLight

## Projekt
- Aplikacja tray (WinUI / Windows App SDK) monitorująca Azure DevOps i GitHub Actions.
- .NET 10, C# 14, nullable włączone.
- Klienci integracji implementują `IIntegrationClient` i zwracają `Snapshot`.
- Teksty UI wyłącznie przez `Strings` (`Strings.resx` + `Strings.pl.resx`); nie wpisuj tekstów na sztywno.

## Język
- Odpowiedzi i komentarze w rozmowie: po polsku.
- Kod, nazwy identyfikatorów, komunikaty commitów: po angielsku.

## Coding style
- Zasady stylu kodu C# (nazewnictwo, formatowanie, enumy, stałe, async, wyjątki, komentarze) są w `.github/csharp-coding-style.instructions.md` - stosuj je przy każdej zmianie w C#.

## Lokalizacja
- Zasady dotyczące tekstów UI są w `.github/localization.instructions.md` - stosuj je przy każdej zmianie tekstów widocznych dla użytkownika.

## Commity
- Format Conventional Commits, małe litery, bez kropki: `feat: ...`, `fix: ...`, `docs: ...`, `refactor: ...`.
- Jeden logiczny temat na commit.

## Kodowanie plików
- Wszystkie pliki (`.cs`, `.resx`, `.md`) w UTF-8 bez BOM.
- Nie używaj `Set-Content` w PowerShell do edycji plików z polskimi znakami; użyj narzędzi edycji lub `UTF8Encoding($false)`.

## Weryfikacja
- Po zmianach uruchom `dotnet build CicdLight.slnx` i upewnij się, że kończy się bez błędów.

## Moje dodatkowe warunki
<!-- Uzupełnij własne reguły, np.: -->
<!-- - Maksymalna długość metody: 30 linii. -->
<!-- - Zakaz używania `dynamic`. -->
<!-- - Każda nowa publiczna metoda ma mieć test. -->
-
