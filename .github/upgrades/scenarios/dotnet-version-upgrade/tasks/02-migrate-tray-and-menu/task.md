# 02-migrate-tray-and-menu: Migracja ikony tray, menu i powiadomień

Przepisać `TrayApplicationContext` na kontroler oparty o `TaskbarIcon` (H.NotifyIcon): dynamiczna ikona kolorowa (zielony/niebieski/żółty/czerwony/szary) generowana przez System.Drawing, tooltip, menu z sekcjami Pipelines/Releases/Review requested/My pull requests otwierającymi przeglądarkę, pozycje Refresh/Open Azure DevOps/Choose pipelines/Settings/Exit, timer odpytywania (`DispatcherQueueTimer`), powiadomienia (nieudany build, naprawiony build, nowa prośba o review) z kliknięciem otwierającym URL. Zachować logikę `Notify`/baseline i `AzureDevOpsClient`.

**Done when**: Ikona zmienia kolor wg `Health`, menu działa lewym i prawym kliknięciem, powiadomienia pokazują się i otwierają URL, kod nie używa System.Windows.Forms.
