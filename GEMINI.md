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
  - **stardew-access** (`shoaib.stardewaccess`): Se rilevato all'avvio del gioco, il navigatore indirizza le istruzioni vocali direttamente allo screen reader tramite riflessione (reflection) per evitare accoppiamenti forti a livello di codice compilato.
  - **Generic Mod Config Menu (GMCM)**: Se rilevato, espone le opzioni di configurazione nel menu impostazioni in-game.

---

## 2. Struttura del codice

Il repository è strutturato come segue:

- [StardewNavigator.sln](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator.sln): Soluzione Visual Studio.
- [StardewNavigator/](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/)
  - [StardewNavigator.csproj](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/StardewNavigator.csproj): Progetto SMAPI standard.
  - [ModEntry.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/ModEntry.cs): Entry point. Gestisce il caricamento, la registrazione agli eventi di gioco (ButtonPressed, Warped, SaveLoaded) e l'inizializzazione del controllo aggiornamenti asincrono.
  - [ModConfig.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/ModConfig.cs): Modello di configurazione del mod (tasto di menu, durata messaggi HUD, controllo aggiornamenti all'avvio).
  - [NavigatorSpeaker.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/NavigatorSpeaker.cs): Wrapper per la sintesi vocale. Invia l'output a `stardew-access` via reflection o ripiega su un `HUDMessage` in-game per giocatori normovedenti.
  - [Log.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Log.cs): Gestore di logging interno che rimanda alla console di monitoraggio di SMAPI.
  - [Features/Navigator/](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/):
    - [Navigator.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/Navigator.cs): State machine principale che controlla le fasi di pathfinding e la transizione tra warp.
    - [RouteEngine.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/RouteEngine.cs): Algoritmo BFS per la generazione dei grafi multimappa basato sulle porte degli edifici e sui warps di gioco.
    - [NavigatorMenu.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/NavigatorMenu.cs): UI in-game testuale, navigabile e compatibile con screen reader per selezionare le destinazioni.
    - [DestinationRegistry.cs](file:///C:/Users/nemex/OneDrive/Documenti/GitHub/StardewNavigator/StardewNavigator/Features/Navigator/DestinationRegistry.cs): Registro che carica e compila i POI dal JSON delle destinazioni.
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

- **Always** garantire 0 errori e 0 avvisi nella build di Release prima di committare, taggare o pushare modifiche.
- **Never** committare file machine-specific come `StardewNavigator.csproj.user` (gia ignorato in `.gitignore`).
- **Must** utilizzare **Conventional Commits** per ogni commit (es. `feat: ...`, `fix: ...`).
- **Must** localizzare i testi tramite chiavi i18n standard di SMAPI (in `i18n/default.json`). Non inserire stringhe hardcoded nel codice C# o nel file `navigator_destinations.json`.
- **Never** invocare direttamente librerie di `stardew-access` a tempo di compilazione. Qualsiasi interazione deve passare attraverso la reflection implementata in `NavigatorSpeaker.cs`.
- **Always** chiedere chiarimenti all'utente prima di effettuare grandi refactoring o cambiamenti all'algoritmo di routing BFS.

---

## 5. Pattern architetturali e convenzioni interne

### Localizzazione al Caricamento (Option A)
Per mantenere il codice semplice, i file `navigator_destinations.json` contengono le sole chiavi i18n (es. `"nav.map.farm"`). `DestinationRegistry.cs` risolve e sostituisce queste chiavi con le stringhe localizzate corrispondenti a runtime all'interno del metodo `ResolveCoordinates()`, eseguito ad ogni caricamento di partita (`SaveLoaded`).

### In-Game Update Checking
All'avvio del gioco, se `CheckForUpdatesOnStartup` è attivo, `ModEntry.cs` esegue una chiamata asincrona non bloccante via `Task.Run` all'API GitHub per recuperare l'ultima release. In caso di aggiornamento disponibile, viene notificato al giocatore in-game sia visivamente (HUDMessage) che acusticamente (`NavigatorSpeaker.Say`).

---

## 6. Git workflow e release management

### Convenzioni Commit
Format obbligatorio dei messaggi di commit:
`<tipo>: <descrizione>`
Tipi supportati: `feat`, `fix`, `docs`, `refactor`, `perf`, `chore`.

### Flusso di Release Obbligatorio
Per pubblicare una nuova versione del mod, attenersi rigorosamente alla seguente sequenza:

1. **Bump Versione**: Aggiornare il campo `"Version"` nel file `StardewNavigator/manifest.json`.
2. **Build Release**: Eseguire il build in configurazione Release per generare lo zip del pacchetto:
   `dotnet build StardewNavigator.sln --configuration Release`
   Il file zip verrà posizionato in `StardewNavigator/bin/Release/net6.0/StardewNavigator [Versione].zip`.
3. **Commit Bump**: Committare la modifica del manifest (`git commit -m "chore: bump version to vX.Y.Z"`).
4. **Tagging**: Creare il tag Git corrispondente (`git tag vX.Y.Z`).
5. **Push branch**: Pushare il branch main (`git push origin main`).
6. **Push tag**: Pushare il tag Git (`git push origin vX.Y.Z`).
7. **Release GitHub**: Creare la release con GitHub CLI:
   `gh release create vX.Y.Z --title "vX.Y.Z — Titolo" --notes "Note di rilascio"`
8. **Upload ZIP**: Allegare lo zip generato in precedenza alla release creata:
   `gh release upload vX.Y.Z "StardewNavigator/bin/Release/net6.0/StardewNavigator [Versione].zip"`

---

## 7. Stato attuale e prossimi passi

- **Versione Corrente**: `1.0.1` (tracciata nel manifest.json).
- **Prossimi punti di attenzione**:
  - Validare il funzionamento dell'installer PowerShell su diverse configurazioni di Windows ExecutionPolicy.
  - Monitorare la reattività del check aggiornamenti asincrono all'avvio in condizioni di scarsa connettività di rete.
