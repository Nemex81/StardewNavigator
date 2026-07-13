# GEMINI.md — StardewNavigator Mod Guidelines

This document provides context, architectural constraints, rules, and workflows for any AI assistant developing on this repository.

---

## 1. Contesto del progetto

### Scopo della mod
`StardewNavigator` è una mod SMAPI standalone per Stardew Valley estrapolata dal modulo Navigator di `stardew-access`. Consente il calcolo automatico dei percorsi e la navigazione assistita verso punti di interesse (POI) e warp della mappa.

### Tecnologie e Dipendenze
- **Linguaggio**: C#
- **Framework**: .NET 6.0
- **Ecosistema**: SMAPI (Stardew Modding API) 4.0+
- **Pacchetti NuGet**:
  - `Pathoschild.Stardew.ModBuildConfig` (compilazione e deploy automatico)
- **Integrazioni (Soft Dependency)**:
  - **stardew-access** (Unique ID SMAPI: `shoaib.stardewaccess` o `stardew.access`): Se rilevato all'avvio del gioco nel registro mod SMAPI, il navigatore indirizza le istruzioni vocali direttamente allo screen reader tramite riflessione (reflection) per evitare accoppiamenti forti a livello di codice compilato.
    - I dettagli sull'architettura della Reflection, sui contratti osservati e sul bridge dinamico sono delegati al documento tecnico `@./docs/stardew-access-integration.md`.
  - **Generic Mod Config Menu (GMCM)**: Se rilevato, espone le opzioni di configurazione nel menu impostazioni in-game.

---

## 2. Struttura del codice

Il repository è strutturato come segue:

- [StardewNavigator.sln](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator.sln): Soluzione Visual Studio.
- [StardewNavigator/](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/)
  - [StardewNavigator.csproj](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/StardewNavigator.csproj): Progetto SMAPI standard.
  - [ModEntry.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/ModEntry.cs): Entry point. Gestisce il caricamento, la registrazione agli eventi di gioco (ButtonPressed, Warped, SaveLoaded) e l'inizializzazione del controllo aggiornamenti asincrono.
  - [ModConfig.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/ModConfig.cs): Modello di configurazione del mod (tasto di menu, profili attivi, overrides e controllo aggiornamenti all'avvio).
  - [NavigatorSpeaker.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/NavigatorSpeaker.cs): Wrapper per la sintesi vocale. Invia l'output a `stardew-access` via reflection o ripiega sul sintetizzatore vocale interno (SAPI) in modalità standalone.
  - [Log.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Log.cs): Gestore di logging interno che rimanda alla console di monitoraggio di SMAPI.
  - [Features/Navigator/](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/):
    - [Navigator.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/Navigator.cs): State machine principale che controlla le fasi di pathfinding e la transizione tra warp.
    - [RouteEngine.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/RouteEngine.cs): Algoritmo BFS per la generazione dei grafi multimappa basato sulle porte degli edifici e sui warps di gioco.
    - [NavigatorMenu.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/NavigatorMenu.cs): UI in-game testuale, navigabile e compatibile con screen reader per selezionare le destinazioni.
    - [DestinationRegistry.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/DestinationRegistry.cs): Registro che carica e compila i POI dal JSON delle destinazioni.
    - [NumpadController.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/NumpadController.cs): Dispatcher di runtime e gestore della ripetizione continua (repeat) e del timing.
    - [TileInspector.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/TileInspector.cs): Gestore standalone di ispezione delle tile (descrizioni ambientali).
    - [GridMovement.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/GridMovement.cs): Gestore standalone dei singoli passi fisici a griglia del personaggio.
    - [NumpadActionCatalog.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/NumpadActionCatalog.cs): Catalogo delle azioni semantiche eseguibili dal tastierino.
    - [NumpadProfileRegistry.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/NumpadProfileRegistry.cs): Registro dei profili predefiniti (Blind/Sighted) e degli override dinamici.
    - [NumpadConfigMenu.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/NumpadConfigMenu.cs): Menu in-game per la personalizzazione interattiva dei tasti.
    - [InputChord.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/InputChord.cs): Rappresentazione delle combinazioni di tasti e tasti modificatori.
  - [assets/](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/assets/):
    - [navigator_destinations.json](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/assets/navigator_destinations.json): Registro strutturato con chiavi i18n per le mappe e i punti di interesse calcolabili.
  - [i18n/](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/i18n/): File JSON standard SMAPI per la localizzazione (`default.json`, `it.json`).
- [installer/](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/installer/)
  - [install.ps1](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/installer/install.ps1): Script interattivo di installazione, disinstallazione e aggiornamento con rilevamento automatico del percorso di gioco.
  - [install.bat](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/installer/install.bat): Launcher double-click per aggirare l'ExecutionPolicy di Windows PowerShell.

---

## 3. Build, test e deploy locale

### Prerequisiti
- .NET 6.0 SDK installato.
- Gioco Stardew Valley installato.
- File `StardewNavigator.csproj.user` configurato per indicare il percorso corretto di Stardew Valley per il compilatore. Esempio:
  ```xml
  <Project>
    <PropertyGroup>
      <GamePath>C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley</GamePath>
    </PropertyGroup>
  </Project>
  ```

### Comandi Build
Esegui i comandi dalla root del repository:

- **Build Debug**:
  ```powershell
  & "C:\Users\nemex\AppData\Local\dotnet\dotnet.exe" build StardewNavigator.sln
  ```
- **Build Release**:
  ```powershell
  & "C:\Users\nemex\AppData\Local\dotnet\dotnet.exe" build StardewNavigator.sln --configuration Release
  ```

### Deploy Locale
Il pacchetto `Pathoschild.Stardew.ModBuildConfig` copia automaticamente l'output compilato nella cartella `Mods/StardewNavigator/` di gioco.

### Installer e Aggiornamento
Per installare, aggiornare o disinstallare manualmente il mod in locale, fare doppio clic su [install.bat](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/installer/install.bat). Lo script effettuerà l'autodetect della cartella Mods e scaricherà l'ultima release ufficiale da GitHub.

---

## 4. Regole di sviluppo permanenti

- **Always** garantire 0 errori e 0 avvisi (inclusi sia compiler warnings sia static analyzer warnings) nella build locale di Release prima di committare, taggare o pushare modifiche.
- **Never** committare file machine-specific o configurazioni locali come `StardewNavigator.csproj.user` o `installer/config.json` (già ignorati in `.gitignore`). Se un file machine-specific o temporaneo (es. `StardewNavigator.csproj.user`, `installer/config.json`, cartelle `bin/`, `obj/`) viene tracciato per errore, va rimosso dall'indice di git usando `git rm --cached <file>`.
- **Must** utilizzare **Conventional Commits** in **lingua inglese** per ogni commit (es. `feat: ...`, `fix: ...`). I messaggi non devono essere generici e devono descrivere chiaramente il cambiamento.
- **Must** localizzare i testi tramite chiavi i18n standard di SMAPI. Ogni nuova chiave deve essere aggiunta contestualmente sia in `i18n/default.json` (EN) sia in `i18n/it.json` (IT). La parità delle chiavi tra i due file è obbligatoria e verificabile via `git diff i18n/`. Non inserire stringhe hardcoded nel codice C#. Per le convenzioni di naming, i token di interpolazione e il workflow completo, consultare `@./docs/localization.md`.
- **Never** invocare direttamente librerie di `stardew-access` a tempo di compilazione. Qualsiasi interazione deve passare attraverso la reflection centralizzata in `StardewAccessBridge.cs`. Le logiche di fallback standalone (ispezione tile e movimenti a griglia) devono risiedere esclusivamente in `TileInspector.cs` e `GridMovement.cs`. `NavigatorSpeaker.cs` è esclusivamente un wrapper chiamante per l'output vocale, non il componente centrale del bridge.
- **Always** chiedere chiarimenti all'utente prima di effettuare grandi refactoring o cambiamenti all'algoritmo di routing BFS.

---

## 5. Pattern architetturali e convenzioni interne

### Flusso di Dispatch dell'Input Numpad
Qualsiasi pressione di un tasto sul tastierino numerico fluisce attraverso questi passaggi sequenziali:
1. **Intercettazione**: L'evento `ButtonPressed` in `NavigatorFeature.cs` lo cattura ad alta priorità.
2. **Orchestrazione e pre-guard**: `NumpadController.HandleButton` esegue controlli fisici preliminari (NumLock, WorldReady, PlayerFree), risolve i casi speciali hardcoded (tasto Decimal, navigazione nel menu NavigatorMenu aperto, micro-movimenti Ctrl) e interroga il registro.
3. **Risoluzione logica**: `NumpadProfileRegistry.TryResolveAction` unisce il profilo corrente (Blind/Sighted) con il dizionario delle mappature personalizzate `NumpadOverrides`, applicando lo swap automatico e filtrando le chiavi disattivate (`None`).
4. **Esecuzione**:
   - Le azioni che richiedono repeat continui o cooldown interni rimangono nel loop dei tick o nel dispatcher di `NumpadController` (es. repeat di TileViewer, ripetizione di `GridMovement.MoveGrid`).
   - Le letture di fallback nel mondo (ReadTile) vengono delegate a `TileInspector.cs`.
   - Tutte le altre azioni logiche standard sono delegate a `NumpadActionCatalog.Execute`.

### Localizzazione al Caricamento (Option A)
Per mantenere il codice semplice, i file `navigator_destinations.json` contengono le sole chiavi i18n (es. `"nav.map.farm"`). `DestinationRegistry.cs` risolve e sostituisce queste chiavi con le stringhe localizzate corrispondenti a runtime all'interno del metodo `ResolveCoordinates()`, eseguito ad ogni caricamento di partita (`SaveLoaded`).

### In-Game Update Checking
All'avvio del gioco, se `CheckForUpdatesOnStartup` è attivo, `ModEntry.cs` esegue una chiamata asincrona non bloccante via `Task.Run` all'API GitHub per recuperare l'ultima release. In caso di aggiornamento disponibile, viene notificato al giocatore in-game acusticamente (`NavigatorSpeaker.Say`) e registrato nei log di SMAPI.

---

## 6. Git workflow e release management

### Convenzioni Commit
Tutti i commit devono essere scritti in **lingua inglese**, essere descrittivi e seguire il formato:
`<tipo>(<ambito>): <descrizione>` (con ambito opzionale)
Tipi supportati: `feat`, `fix`, `docs`, `refactor`, `perf`, `chore`.

### Checklist Pre-Push
Prima di effettuare il push su GitHub:
1. Eseguire `git status` per assicurarsi che non siano stati accidentalmente indicizzati file machine-specific (`csproj.user`), file locali dell'installer (`installer/config.json`) o cartelle temporanee (`bin/`, `obj/`).
2. Se presenti nello stage, rimuoverli dall'indice tramite `git rm --cached <file>`.

### Flusso di Release Obbligatorio
Per pubblicare una nuova versione del mod, attenersi rigorosamente alla seguente sequenza:

1. **Bump Versione**: Aggiornare il campo `"Version"` nel file `StardewNavigator/manifest.json`.
2. **Build Release**: Eseguire il build locale in configurazione Release per generare lo zip del pacchetto:
   `dotnet build StardewNavigator.sln --configuration Release`
   Il file zip verrà posizionato in `StardewNavigator/bin/Release/net6.0/StardewNavigator [Versione].zip` (o `Debug/net6.0` se compilato in Debug).
3. **Verifica ZIP (Checklist Pre-Release)**: Scompattare temporaneamente o ispezionare il file ZIP generato per verificare che contenga la build compilata corretta, che le risorse (cartella `assets/`, cartella `i18n/`) siano presenti ed aggiornate, e che il `manifest.json` mostri la versione corretta appena configurata.
4. **Commit Bump**: Committare la modifica del manifest (`git commit -m "chore(manifest): bump version to vX.Y.Z"`).
5. **Tagging**: Creare il tag Git corrispondente (`git tag vX.Y.Z`).
6. **Push branch**: Pushare il branch main (`git push origin main`).
7. **Push tag**: Pushare il tag Git (`git push origin vX.Y.Z`).
8. **Release GitHub**: Creare la release con GitHub CLI:
   `gh release create vX.Y.Z --title "vX.Y.Z — Titolo" --notes "Note di rilascio"`
9. **Upload ZIP**: Allegare lo zip verificato alla release creata:
   `gh release upload vX.Y.Z "StardewNavigator/bin/Release/net6.0/StardewNavigator [Versione].zip"`

---

## 7. Stato attuale e prossimi passi

- **Versione Corrente**: `1.2.11` (tracciata nel manifest.json).
- **Prossimi punti di attenzione**:
  - Validare il funzionamento dell'installer PowerShell su diverse configurazioni di Windows ExecutionPolicy.
  - Monitorare la reattività del check aggiornamenti asincrono all'avvio in condizioni di scarsa connettività di rete.

---

## 8. Indice della documentazione tecnica interna (docs/)

Per mantenere questo file conciso ed evitare letture non necessarie ad ogni turno di lavoro dell'agente, i dettagli implementativi specifici sono organizzati in documenti tecnici specializzati sotto la cartella `docs/`. L'agente è tenuto a leggere ed applicare tali indicazioni nei seguenti casi:

* **Task che coinvolgono il comportamento condizionale con stardew-access**:
  - Documento da consultare: `@./docs/stardew-access-integration.md`
  - Ambiti coperti: Modifiche a `StardewAccessBridge.cs`, logiche di integrazione, gestione di GridMovement, TileViewer, ObjectTracker, output vocale (TTS), Reflection o input simulato.

* **Task relativi all'input, modificatori, scorciatoie, o all'interazione con l'ambiente**:
  - Documento da consultare: `@./docs/input-management.md`
  - Ambiti coperti: Modifiche a `NumpadController.cs`, `GridMovement.cs`, `NumpadActionCatalog.cs`, tasti modificatori (`Ctrl`, `Alt`, `Shift`), soppressione, rate-limiting, cooldown o interazioni con NVDA/AltGr.

* **Task che coinvolgono stringhe UI, messaggi vocali, nomi di destinazioni o configurazione GMCM**:
  - Documento da consultare: `@./docs/localization.md`
  - Ambiti coperti: Aggiunta o modifica di chiavi in `i18n/default.json` e `i18n/it.json`, aggiunta di destinazioni in `navigator_destinations.json`, uso di `Helper.Translation.Get()`, token di interpolazione `{{...}}`.
