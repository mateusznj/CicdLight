# Plan: ETA buildów i konfigurowalny sygnał PR-ów w zasobniku

Status: plan do implementacji. Zakres: CicdLight (Azure DevOps i GitHub).

## Ustalenia z kodu

- `Snapshot.Health` oraz `PullRequestsToRevise` ([Models/Snapshot.cs](Models/Snapshot.cs)) liczą każdy PR z nierozwiązanymi komentarzami, także cudze z sekcji „Other pull requests". Stąd żółta ikonka.
- `TrayController.Notify` ([Tray/TrayController.cs](Tray/TrayController.cs)) wysyła toast o komentarzach dla każdego PR-a, bez sprawdzania `IsMine`.
- `BuildInfo` ([Models/BuildInfo.cs](Models/BuildInfo.cs)) ma jeden `Time` (finish, start albo queue w ADO, `updated_at` w GitHubie). Brakuje czasu startu i historii czasów trwania.

## Decyzje produktowe

1. Sygnał PR-ów w ikonce jest konfigurowalny. PR-y „moje" i „other" są rozdzielone w tooltipie ikony.
2. ETA jest widoczne w popupie, a szczegóły także w tooltipie wiersza.

## Zadanie 1 - konfigurowalny sygnał PR-ów (release patch)

- [ ] `Snapshot`: dodać `MyPullRequestsToRevise` (`IsMine && HasUnresolvedComments`) i `OtherPullRequestsToRevise` (`!IsMine && HasUnresolvedComments`).
- [ ] `AppSettings`: dwie niezależne opcje, np. `TrayAlertMyPrComments` (domyślnie `true`) i `TrayAlertOtherPrComments` (domyślnie `false`). Zachować kompatybilność wczesniejszych plików ustawień (brak pola = wartość domyślna).
- [ ] `Snapshot.Health` przyjmuje ustawienia i zapala `Warning` zgodnie z opcjami. `NeedsMyReview` działa jak dotychczas.
- [ ] Nagłówek popupu („x failed, y to review") uwzględnia ustawienia.
- [ ] Tooltip ikony zasobnika pokazuje osobne linie, np. „My PRs: N with comments" i „Other PRs: M with comments", plus dotychczasowe failed/review.
- [ ] `Notify`: toast o komentarzach zależny od tych samych opcji (moje / other).
- [ ] `SettingsWindow`: dwa przełączniki w sekcji powiadomień; klucze PL/EN w [Strings.cs](Strings.cs).
- [ ] Sekcja „Other pull requests" w popupie bez zmian.
- [ ] Sprawdzić poprawność `IsMine` dla GitHuba i ADO (w ADO wpis z listy „moich" nadpisuje `IsMine = true`).

**Akceptacja:** przy domyślnych ustawieniach komentarz pod cudzym PR-em nie zmienia koloru ikony ani nie wywołuje toasta; włączenie opcji „other" przywraca stary sygnał; tooltip rozdziela liczniki.

## Zadanie 2 - ETA buildów

- [ ] `BuildInfo`: dodać `StartTime`, `FinishTime`; `Time` zostaje dla „ago". Dodać `Duration`.
- [ ] ADO: mapować `startTime` / `finishTime` z istniejącej odpowiedzi `builds` (`$top=100`), bez dodatkowych zapytań.
- [ ] GitHub: mapować `run_started_at` i `updated_at` z już pobieranych 100 runów (bez rosnącego ryzyka rate limitu).
- [ ] Nowy `BuildEtaEstimator`: mediana czasu ostatnich 5-10 udanych buildów tego samego pipeline'u i brancha; fallback: ten sam pipeline bez względu na branch; mniej niż 3 próbki = brak ETA.
- [ ] Historia czasów trzymana w pamięci między pollami, żeby wynik nie skakał.
- [ ] Popup ([Windows/TrayPopupWindow.cs](Windows/TrayPopupWindow.cs)): dla `IsRunning` zamiast „ago" pokazać np. „~3 min left"; po przekroczeniu estymaty „overdue"; bez historii „running 4 min".
- [ ] Tooltip wiersza: start, mediana, liczba próbek.
- [ ] Ustawienie „Show build ETA" (domyślnie włączone) i klucze `ETA_*` w `Strings`.

**Akceptacja:** działający build ma w popupie ETA zgodne z historią; brak historii nie psuje wyświetlania; brak dodatkowych zapytań do API.

## Testy i dokumentacja

- [ ] Projekt testowy (xUnit) - w repo go jeszcze nie ma.
- [ ] Testy: `BuildEtaEstimator` (mediana, fallback, brak próbek, overdue) oraz `Snapshot.Health` dla kombinacji opcji moje/other.
- [ ] Aktualizacja [README.md](README.md), [ROADMAP.md](ROADMAP.md) (wiersz „Szacowany czas builda") i nowych `RELEASE_NOTES`.

## Kolejność

1. Zadanie 1 (małe, niskie ryzyko) - osobny release patch.
2. Zadanie 2 - kolejny release.
