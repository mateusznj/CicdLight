# .NET Version Upgrade

## Preferences
- **Flow Mode**: Automatic
- **Target Framework**: net10.0 (LTS)
- **UI Framework**: WinUI 3 (migracja z WinForms, wszystkie funkcje zachowane)
- **Language**: odpowiedzi po polsku
- **Extra scope**: aktualizacja README.md

## Upgrade Options
- **Upgrade Strategy**: All-at-Once
- **UI Framework**: WinUI 3 unpackaged + H.NotifyIcon.WinUI

## Strategy
**Selected**: All-at-Once
**Rationale**: Jeden projekt bez referencji projektowych.

### Execution Constraints
- Jedna atomowa zmiana; walidacja builda po każdym zadaniu
- Format settings.json i DPAPI bez zmian (kompatybilność wsteczna)

## Source Control
- **Source Branch**: main
- **Working Branch**: upgrade-dotnet-10
- **Commit Strategy**: After Each Task
- **Branch Sync**: Auto (Merge)
