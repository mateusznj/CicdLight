# CicdLight v1.2.0

## 🇵🇱 Polski

Wersja 1.2.0 dodaje szacowany czas do końca trwających buildów oraz konfigurowalne alerty o nierozwiązanych komentarzach w pull requestach. Poprawia też działanie na monitorach ze skalowaniem.

### Najważniejsze zmiany

- **ETA buildów:** popup pokazuje szacowany czas do końca trwających buildów (prefiks „~”, „dłużej niż zwykle”). Szacunek to mediana ostatnich udanych uruchomień tego samego pipeline'u, z preferencją dla tego samego brancha; wymaga co najmniej 3 próbek. Szczegóły są w tooltipie wiersza. Funkcję można wyłączyć w **Ustawienia → Ogólne**.
- **Alerty o komentarzach w PR-ach:** w **Ustawienia → Ogólne** osobno włączasz żółtą ikonę i powiadomienia dla nierozwiązanych komentarzy w Twoich PR-ach (domyślnie włączone) i w pozostałych PR-ach (domyślnie wyłączone). Tooltip ikony pokazuje osobne liczniki „moje” i „inne”.
- Poprawiono przewijanie kółkiem myszy w listach na monitorach ze skalowaniem.
- Aplikacja deklaruje tryb Per-Monitor V2 DPI, dzięki czemu interfejs jest ostrzejszy i poprawnie skalowany na wielu monitorach.

### Instalacja

Po opublikowaniu pobierz instalator Windows x64 z sekcji **Assets** na stronie [CicdLight v1.2.0](https://github.com/mateusznj/CicdLight/releases/tag/v1.2.0). Instalator aktualizuje wcześniejsze wersje.

## 🇬🇧 English

Version 1.2.0 adds estimated time remaining for running builds and configurable alerts for unresolved pull request comments. It also improves behavior on scaled displays.

### Highlights

- **Build ETA:** the popup shows the estimated time left for running builds (prefixed with "~", or "longer than usual"). The estimate is the median of recent successful runs of the same pipeline, preferring the same branch, and requires at least 3 samples. Details are shown in the row tooltip. It can be turned off in **Settings → General**.
- **PR comment alerts:** in **Settings → General** you choose separately whether unresolved comments turn the tray icon yellow and raise notifications for your own PRs (on by default) and for other PRs (off by default). The tray tooltip lists "mine" and "other" counts separately.
- Fixed mouse wheel scrolling in lists on scaled monitors.
- The app now declares Per-Monitor V2 DPI awareness, so the UI is sharper and scales correctly across multiple monitors.

### Installation

Once released, download the Windows x64 installer from the **Assets** section on the [CicdLight v1.2.0 release page](https://github.com/mateusznj/CicdLight/releases/tag/v1.2.0). The installer upgrades earlier versions.
