# CicdLight - plan rozwoju

Dokument jest roboczym planem porównującym obecne możliwości CicdLight z funkcjami opisanymi na [stronie CatLight](https://catlight.io/a/features). Będziemy go aktualizować po decyzjach produktowych i kolejnych wdrożeniach.

- Ostatni przegląd: 2026-11-15
- Status: propozycja, kolejność wymaga potwierdzenia
- Zakres obecnej aplikacji: Windows, Azure DevOps i GitHub

## Stan obecny

- CicdLight pokazuje w zasobniku zbiorczy status buildów, wydań i PR-ów.
- Popup pokazuje najnowsze buildy, wydania oraz PR-y oczekujące na review i własne PR-y.
- Użytkownik może wybrać obserwowane pipeline'y i wydania.
- Powiadomienia obejmują nieudane i naprawione buildy, nowe prośby o review oraz przyrost nierozwiązanych komentarzy. Część ustawień jest per projekt lub repozytorium.
- Brakuje wspólnego, trwałego stanu pracy zespołu. Konfiguracja i historia są lokalne dla użytkownika.

## Porównanie funkcji

| Obszar | Stan CicdLight | Luka |
|---|---|---|
| Osobisty pulpit i lista zadań | Popup z buildami, wydaniami i wybranymi kategoriami PR-ów | Brak jednej posortowanej listy wszystkich działań, filtrów i własnych reguł priorytetu |
| Lista obserwowanych | Wybór pipeline'ów i wydań | Brak śledzenia konkretnych PR-ów, issues i work items oraz zmian ich stanu |
| Historia pipeline'ów | Wyświetlane są najnowsze wyniki | Brak osobnej historii dla branchy, pierwszego błędnego builda i kontekstu poprzednich zmian |
| Szacowany czas builda | Czas do końca trwających buildów w popupie (mediana ostatnich udanych uruchomień) | Brak estymacji dla branchy bez historii i trwałej historii między sesjami |
| Potwierdzanie alertów | Brak | Nie można oznaczyć alertu jako zauważonego ani odłożyć go na później |
| Powiadomienia buildów | Błędy i naprawy | Brak pełnego zestawu zdarzeń, np. startu i każdego zakończenia, oraz filtrów po branchu |
| Powiadomienia wydań | Wydania są widoczne w popupie | Brak powiadomień o sukcesie, błędzie i oczekujących akceptacjach |
| Powiadomienia PR | Nowe prośby o review i przyrost nierozwiązanych komentarzy | Brak powiadomień o akceptacji, żądaniu zmian i zamknięciu własnego PR-a |
| Issues i work items | Brak | Brak GitHub Issues i Azure Boards oraz zapytań wybieranych przez użytkownika |
| Wspólne dashboardy | Brak | Brak wielu pulpitów współdzielonych z zespołem lub przeznaczonych na ekran statusowy |
| „I will investigate” | Brak | Brak właściciela analizy, czasu rozpoczęcia, aktualizacji, komentarzy i stanu rozwiązania |
| Team Status | Brak | Brak zestawienia aktywnych alertów przypisanych do osób i obciążenia zespołu |
| Pierwsza przyczyna awarii | Brak | Brak wskazania pierwszego nieudanego builda lub osoby, której zmiana zapoczątkowała awarię |
| WIP i review zespołowe | Brak | Brak sygnału, nad czym pracuje dana osoba i czy wykonuje review |
| Systemy operacyjne | Windows | Brak wersji macOS i Linux |

## Proponowana kolejność

### Etap 1 - uzupełnienie zdarzeń i filtrów powiadomień

- [ ] Dodać powiadomienia o rozpoczęciu builda i jego zakończeniu.
- [ ] Dodać powiadomienia o sukcesie, błędzie i akceptacji wydania, jeśli dostawca udostępnia takie zdarzenie.
- [ ] Dodać powiadomienia o akceptacji PR-a, żądaniu zmian i zamknięciu własnego PR-a.
- [ ] Rozszerzyć ustawienia per połączenie o typ zdarzenia, pipeline/repozytorium i branch.
- [ ] Zapobiec wielokrotnym toastom tego samego zdarzenia po ponownym odpytywaniu API.

**Akceptacja:** każde zdarzenie ma testy przejść statusu, konfigurację powiadomień i zachowuje obecne ustawienia użytkownika.

### Etap 2 - osobista lista zadań i watchlista

- [ ] Rozdzielić elementy wymagające działania od elementów, na które użytkownik czeka.
- [ ] Pozwolić obserwować wybrane PR-y, wydania, issues i work items, a nie tylko pipeline'y.
- [ ] Dodać filtrowanie i grupowanie po połączeniu, repozytorium, branchu i rodzaju elementu.
- [ ] Dodać proste reguły priorytetu, np. nieudany build przed oczekującym review.
- [ ] Zapisywać lokalne preferencje widoku bez zmiany danych w systemach źródłowych.

**Akceptacja:** po restarcie użytkownik zachowuje watchlistę i ustawienia widoku; usunięcie połączenia usuwa lub jawnie odłącza jego lokalne wpisy.

### Etap 3 - potwierdzanie alertów i „I will investigate”

#### Zachowanie produktu

- [ ] Dodać akcję **I will investigate** dla nieudanego builda, wydania i PR-a.
- [ ] Zapisać osobę prowadzącą, czas rozpoczęcia, stan (investigating/resolved) i opcjonalną notatkę.
- [ ] Pokazać osobę i czas trwania analizy w popupie oraz w szczegółach alertu.
- [ ] Pozwolić zwolnić przypisanie lub przekazać je innej osobie.
- [ ] Dodać osobną akcję acknowledge/snooze. Acknowledge oznacza „wiem o problemie”, a investigate oznacza „zajmuję się nim”; nie powinny być tym samym stanem.
- [ ] Uzgodnić usuwanie lub zamykanie wpisu, gdy źródłowy build/PR przestanie być aktywny.

#### Integracja z GitHub

- Dla PR-a można użyć istniejącego mechanizmu Issues API: przypisanie użytkownika do PR-a i etykieta, np. `cicdlight:investigating`. GitHub traktuje PR także jako issue, więc assignees i labels są dostępne przez Issues API.
- Przypisanie jest widoczne dla zespołu, ale zmienia dane repozytorium i wymaga uprawnień zapisu. CicdLight musi pytać o zgodę, nie nadpisywać istniejących assignees i odróżniać własną etykietę od etykiet użytkownika.
- Workflow run nie ma analogicznego pola assignee/investigation. Check Runs API pozwala publikować statusy, ale zapis check run wymaga GitHub App. Nie należy udawać, że zwykły token PAT może dopisać właściciela do workflow run.
- **Propozycja:** wspólny stan CicdLight jest źródłem prawdy dla analiz buildów; dla PR-ów można opcjonalnie synchronizować assignee/label po wyraźnym włączeniu tej funkcji.

#### Integracja z Azure DevOps

- Dla builda można użyć Build Tags API i dodać tag w rodzaju `cicdlight:investigating:<identity>`. API pozwala dodać tag do builda i wymaga uprawnienia do zapisu buildów.
- Dla PR-a Azure Repos udostępnia Pull Request Threads API. Można utworzyć komentarz/wiadomość o rozpoczęciu analizy, ale wątek jest rozmową, a nie dedykowanym polem właściciela analizy.
- Nie należy automatycznie zmieniać głosu reviewerów ani tworzyć komentarzy bez zgody użytkownika.
- **Propozycja:** wspólny stan CicdLight pozostaje źródłem prawdy; tag builda i komentarz w PR są opcjonalnymi integracjami, które można włączać osobno.

#### Wspólna architektura

- Sama lokalna aplikacja nie może wiarygodnie pokazać zespołowi, kto kliknął **I will investigate**. Potrzebny jest współdzielony magazyn stanu lub synchronizacja przez API dostawcy.
- Preferowany kierunek dla kompletnego Team Status: małe API CicdLight z uwierzytelnianiem, bazą danych, historią zmian i kontrolą dostępu do organizacji.
- Nie wysyłać PAT-ów ani tokenów GitHub do wspólnego serwisu. Backend przechowuje wyłącznie rekordy analizy i identyfikatory źródłowych elementów.
- Jako ograniczony pierwszy krok można wdrożyć stan lokalny. UI powinien wtedy jasno informować, że wpis nie jest widoczny dla innych użytkowników.

**Akceptacja:** dwa równoczesne klienty widzą tę samą osobę prowadzącą i ten sam stan; konflikt dwóch zgłoszeń jest rozstrzygany przez serwer i widoczny dla obu stron.

### Etap 4 - historia i kontekst awarii

- [ ] Ustalić model historycznych buildów i sposób przechowywania identyfikatorów providerów.
- [ ] Pokazać osobną historię dla pipeline'u i branchu.
- [ ] Wskazać pierwszy nieudany build w bieżącej serii awarii.
- [ ] Pokazać poprzednie udane buildy i odnośniki do zmian/commitów, gdy API dostawcy je udostępnia.
- [ ] Dodać testy dla brakujących, niekompletnych i przeterminowanych danych.

### Etap 5 - estymacja czasu

- [x] Zbierać czasy rozpoczęcia i zakończenia buildów.
- [x] Obliczać estymację per pipeline, opcjonalnie per branch, na podstawie mediany ostatnich udanych buildów.
- [x] Nie pokazywać estymacji, gdy brakuje minimalnej liczby próbek.
- [x] Pokazać, że jest to szacunek, a nie gwarantowany czas ukończenia (prefiks „~”, „dłużej niż zwykle”).

### Etap 6 - GitHub Issues i Azure Boards

- [ ] Dodać do GitHub konfigurację repozytorium, stanów, labels, assignees i filtrów Issues.
- [ ] Dodać do Azure DevOps wybór zapytania WIQL lub projektu/query dla Work Items.
- [ ] Pokazać przypisanie, priorytet, stan, ostatnią zmianę i link do elementu.
- [ ] Dodać watchlistę i powiadomienia o nowych lub zmienionych elementach.
- [ ] Ograniczyć zakres pobierania przez zapytanie i pamiętać limity API.

**Akceptacja:** użytkownik może dodać lub usunąć zapytanie bez edycji konfiguracji ręcznie, a błędne zapytanie nie ukrywa buildów i PR-ów.

### Etap 7 - Team Status i współdzielone dashboardy

- [ ] Zbudować listę zespołu na podstawie jawnie skonfigurowanych członków/organizacji, nie na podstawie przypadkowych autorów commitów.
- [ ] Pokazać na osobę liczbę aktywnych alertów, analiz w toku, próśb o review i elementów oczekujących.
- [ ] Pokazać czas od przejęcia analizy i umożliwić otwarcie elementów źródłowych.
- [ ] Dodać wiele dashboardów z filtrami dla projektu, repozytorium, zespołu lub roli.
- [ ] Dodać tryb ekranu statusowego bez danych wrażliwych i z kontrolą odświeżania.
- [ ] Ustalić role, retencję, prywatność i zachowanie po odejściu członka zespołu.

**Zależność:** pełny Team Status wymaga wspólnego API z Etapu 3 albo dobrze określonej, natywnej synchronizacji providerów. Same ustawienia lokalne nie wystarczą.

### Etap 8 - praca w toku i priorytety zespołowe

- [ ] Umożliwić jawne oznaczenie **Working on** i **Reviewing**.
- [ ] Dodać reguły priorytetu zespołu, np. awaria produkcyjnego pipeline'u ponad zwykłym review.
- [ ] Wstrzymywać niższe powiadomienia, gdy użytkownik pracuje nad elementem o wyższym priorytecie.
- [ ] Pokazywać regułę, która spowodowała priorytet lub odfiltrowanie powiadomienia.

### Etap 9 - wsparcie platform

- [ ] Przed rozpoczęciem portu wybrać framework UI i model dystrybucji wspólne dla Windows, macOS i Linux.
- [ ] Wydzielić integracje i logikę domenową od Windows tray/toast i DPAPI.
- [ ] Zaprojektować bezpieczne przechowywanie sekretów odpowiednio dla każdej platformy.

**Decyzja produktowa:** nie rozpoczynać portu, dopóki nie zostanie ustalone, czy CicdLight ma pozostać aplikacją Windows.

## Decyzje do podjęcia

- [ ] Czy funkcje zespołowe mają używać hostowanego backendu CicdLight, czy tylko natywnych metadanych GitHub/Azure DevOps?
- [ ] Czy **I will investigate** ma w trybie team tworzyć/zmieniać etykiety, assignees lub komentarze w zewnętrznym repozytorium?
- [ ] Czy najpierw wdrażamy lokalny acknowledge/investigate, czy od razu projektujemy synchronizację zespołową?
- [ ] Który etap ma wejść do następnego wydania?
- [ ] Czy CicdLight pozostaje aplikacją tylko dla Windows?

## Źródła

- [CatLight features](https://catlight.io/a/features)
- [GitHub REST API - issue assignees](https://docs.github.com/en/rest/issues/assignees?apiVersion=2022-11-28)
- [GitHub REST API - check runs](https://docs.github.com/en/rest/checks/runs?apiVersion=2022-11-28)
- [GitHub REST API - workflow runs](https://docs.github.com/en/rest/actions/workflow-runs?apiVersion=2022-11-28)
- [Azure DevOps REST API - add build tag](https://learn.microsoft.com/en-us/rest/api/azure/devops/build/tags/add-build-tag?view=azure-devops-rest-7.1)
- [Azure DevOps REST API - create pull request thread](https://learn.microsoft.com/en-us/rest/api/azure/devops/git/pull-request-threads/create?view=azure-devops-rest-7.1)
