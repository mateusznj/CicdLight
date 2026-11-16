---
applyTo: "**/*.{cs,resx}"
description: "Dla etykiet i tekstów widocznych dla użytkownika zawsze używaj zasobów resx i zgłaszaj brakujący tekst polski lub angielski zamiast go wymyślać."
---

dla CicdLight (Localization)

zawsze używaj zasobów `.resx` przez klasę `Strings` (`Strings.Get`, `Strings.Format`) zamiast
- Używaj ponownie istniejących kluczy zasobów, gdy znaczenie już się zgadza; dodawaj nowe klucze tylko wtedy, gdy tekst jest rzeczywiście nowy.
- Gdy zmiana wymaga zlokalizowanego tekstu, a wymagany tekst polski lub angielski jest brakujący, niekompletny lub niejednoznaczny, zgłoś lukę zamiast wymyślać tłumaczenie lub sformułowanie produktowe.
- Nie wprowadzaj po cichu tekstu UI tylko po angielsku lub tylko po polsku, gdy otaczająca funkcjonalność już używa zlokalizowanych zasobów.
spójne z istniejącymi prefiksami (np. `Section_`, `Popup_`, `Menu_`, `Toast_`, `Status_`, `Choose_`, `Error_`, `Tray_`).
- Zasoby znajdują się w `Strings.resx` (angielski) oraz `Strings.pl.resx` (polski); każdy nowy klucz dodawaj w obu plikach.
- Teksty z parametrami zapisuj jako format z placeholderami (`{0}`) i wywołuj przez `Strings.Format`; nie sklejaj zlokalizowanych tekstów konkatenacją.