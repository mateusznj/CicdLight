# CicdLight v1.3.0

## 🇵🇱 Polski

Wersja 1.3.0 pozwala wybierać repozytoria, z których pokazywane są pull requesty, dodaje ikony do powiadomień i naprawia ikonę komentarzy przy długich tytułach PR-ów.

### Najważniejsze zmiany

- **Wybór repozytoriów dla PR-ów:** w ustawieniach połączenia Azure DevOps, pod tabelą powiadomień, dla każdego projektu możesz wybrać repozytoria, z których mają być pokazywane pull requesty. Domyślnie widoczne są wszystkie, także nowo utworzone. Odznaczone repozytoria nie są pobierane, pokazywane ani uwzględniane w ikonie, liczniku komentarzy i powiadomieniach.
- **Ikony powiadomień:** nieudany build, naprawiony build, prośba o review PR-a i nowe nierozwiązane komentarze mają własne ikony.
- **Przycisk testowy:** **Ustawienia → Ogólne → Wyślij powiadomienie testowe** wysyła kolejno przykłady wszystkich powiadomień (co 6 sekund), żeby można było zobaczyć ich wygląd.
- **Poprawka:** przy długich tytułach PR-ów ikona z liczbą nierozwiązanych komentarzy nie jest już wypychana poza okno popupu.

### Instalacja

Po opublikowaniu pobierz instalator Windows x64 z sekcji **Assets** na stronie [CicdLight v1.3.0](https://github.com/mateusznj/CicdLight/releases/tag/v1.3.0). Instalator aktualizuje wcześniejsze wersje.

## 🇬🇧 English

Version 1.3.0 lets you choose which repositories pull requests are shown from, adds icons to notifications, and fixes the comments icon for long PR titles.

### Highlights

- **Repository selection for PRs:** in an Azure DevOps connection's settings, below the notifications table, you can choose per project which repositories to show pull requests from. All repositories, including newly created ones, are visible by default. Unchecked repositories are not fetched, shown, or counted in the tray icon, comment counters, and notifications.
- **Notification icons:** failed builds, fixed builds, PR review requests, and new unresolved comments each have their own icon.
- **Test button:** **Settings → General → Send test notification** now sends an example of every notification in turn (6 seconds apart) so you can preview how they look.
- **Fix:** with long PR titles, the unresolved comments icon is no longer pushed out of the popup window.

### Installation

Once released, download the Windows x64 installer from the **Assets** section on the [CicdLight v1.3.0 release page](https://github.com/mateusznj/CicdLight/releases/tag/v1.3.0). The installer upgrades earlier versions.
