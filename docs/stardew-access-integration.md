# Integrazione con stardew-access via Reflection

Questo documento descrive l'architettura tecnica, i vincoli e le strategie anti-regressione per l'integrazione a runtime con la mod `stardew-access`.

---

## 1. Architettura del Bridge (`StardewAccessBridge.cs`)

L'integrazione con `stardew-access` (Unique ID: `shoaib.stardewaccess` o `stardew.access`) è implementata come **dipendenza opzionale (soft dependency)**. La mod `StardewNavigator` deve rimanere interamente funzionante anche in assenza di quest'ultima.

### Caching dei Metadati e Risoluzione Dinamica
Per bilanciare le prestazioni del gioco ed evitare riferimenti ad oggetti obsoleti (ad esempio dopo un ricaricamento del salvataggio o un cambio di sessione), il bridge adotta una strategia ibrida:
1. **Cache dei metadati stabili**: Campi statici privati (es. `Assembly`, `Type`, `MethodInfo`, `PropertyInfo`, `FieldInfo`) vengono risolti in modo lazy e memorizzati in cache al primo utilizzo.
2. **Risoluzione dinamica delle istanze**: Le istanze runtime delle classi di `stardew-access` (es. `MainClass.ScreenReader`, `ObjectTracker.Instance`, `TileViewer.Instance`) vengono recuperate dinamicamente tramite `GetValue(null)` ad ogni chiamata. Questo previene bug dovuti a istanze transienti o ricreate da SMAPI.
3. **Fail-safe robusto**: Tutte le chiamate via reflection sono protette da blocchi `try-catch` generali per intercettare eventuali eccezioni di reflection (`TargetInvocationException`, `NullReferenceException`) e loggarle come avvisi (`Log.Warn`), impedendo al gioco di crashare.

---

## 2. Contratti e Comportamenti Osservati

Nello sviluppo dell'integrazione è fondamentale distinguere il livello di garanzia dei comportamenti e delle API esterne:

### A. Contratto del Progetto (Garanzie di StardewNavigator)
* **Funzionamento Standalone**: Se `IsModLoaded` restituisce `false`, StardewNavigator non deve mai tentare accessi in reflection. Le letture ambientali (coordinate, tile, oggetti impugnati) devono ripiegare su implementazioni locali e mostrare l'output testuale tramite messaggi temporanei a schermo (`Game1.addHUDMessage`).
* **Intercettazione Prioritaria**: Gli eventi di pressione dei tasti in `NavigatorFeature.cs` sono registrati con priorità alta (`[EventPriority(EventPriority.High)]`) per permettere a StardewNavigator di sopprimere gli input prima che stardew-access li processi.

### B. Comportamento Osservato di stardew-access (v1.7.0-beta.2)
* **Blocco di `checkAction`**: Se proviamo ad attivare un baule o un oggetto interattivo a livello logico chiamando direttamente `Game1.currentLocation.checkAction(...)`, stardew-access intercetta la chiamata a monte e restituisce `false` (bloccando l'apertura). Di conseguenza, per interagire è necessario simulare la pressione fisica del tasto nativo di interazione del gioco (`SButton.X`).
* **Persistenza dell'Input Soppresso nel GridMovement**: `stardew-access` gestisce il movimento continuo del giocatore in `GridMovement.Update` verificando se l'ultimo tasto premuto (`LastGridMovementButtonPressed`) è fisicamente premuto *oppure* soppresso:
  `bool isButtonDown = MainClass.ModHelper.Input.IsDown(button) || MainClass.ModHelper.Input.IsSuppressed(button);`
  Se StardewNavigator sopprime un tasto direzionale (es. `NumPad2`) mentre l'utente preme `LeftAlt` per muovere il cursore, per stardew-access il tasto risulta comunque "Down" (in quanto soppresso), inducendo il personaggio a camminare.

### C. Dettaglio Implementativo Non Garantito (Dipendenza Esterna)
* I dettagli sotto indicati appartengono alla struttura interna di `stardew-access` e potrebbero cambiare nelle versioni successive:
  * L'esistenza dell'assembly `"stardew-access"`.
  * La classe `stardew_access.MainClass` con la proprietà statica `ScreenReader` (che espone `Say(string text, bool interrupt)`).
  * La classe `stardew_access.Features.GridMovement` con dei campi statici privati `LastGridMovementButtonPressed` (di tipo `InputButton?`) e `LastGridMovementDirection` (di tipo `int?`).
  * La classe `stardew_access.Features.TileViewer` con la proprietà statica `Instance` e i metodi `cursorMoveInput(Vector2 delta, bool sound)` e `startAutoWalking()`.
  * La classe `stardew_access.Features.ObjectTracker` con il metodo `moveToCurrentlySelectedObject()`.
  * La classe `stardew_access.Features.ReadTile` con la proprietà statica `Instance` e il metodo `Run(bool manuallyTriggered, bool playersPosition)` (firma osservata in v1.7.0-beta.2). StardewNavigator lo invoca tramite reflection come `Run(true, standing)`, dove il primo argomento indica un'attivazione manuale esplicita e il secondo seleziona la tile sotto i piedi del giocatore (`true`) anziché quella di fronte (`false`).

---

## 3. Logica di Reset di GridMovement

Per ovviare al comportamento osservato in cui i tasti soppressi muovono comunque il personaggio, abbiamo implementato il metodo `StardewAccessBridge.TryResetGridMovementState()`.

### Dettaglio Tecnico
Il metodo accede tramite reflection ai campi privati statici di `stardew_access.Features.GridMovement`:
* `LastGridMovementButtonPressed`
* `LastGridMovementDirection`

E li forza a `null` (`SetValue(null, null)`).
In questo modo, quando stardew-access esegue il suo ciclo `Update()`, incontra la condizione di guardia `if (!LastGridMovementButtonPressed.HasValue) return;` ed interrompe immediatamente la computazione del movimento a griglia.

---

## 4. Controlli Anti-Regressione per l'Agente

Prima di completare modifiche relative all'integrazione con stardew-access, l'agente deve verificare la stabilità tramite i seguenti passi:

1. **Compilazione pulita**: Eseguire `dotnet build --configuration Release` e assicurarsi che non vi siano avvisi di reflection o errori di compilazione nel bridge.
2. **Test Standalone**: Disabilitare temporaneamente la cartella di `stardew-access` all'interno della directory `Mods/` del gioco. Avviare Stardew Valley e verificare che l'HUDMessage di fallback descriva accuratamente le tile e le coordinate senza generare eccezioni o blocchi.
3. **Test di Reset del Movimento**: In-game con stardew-access attivo, camminare in una direzione (es. verso il basso), fermarsi, e premere immediatamente `LeftAlt + NumPad2`. Verificare che il personaggio resti fermo sul posto e che si muova unicamente il cursore di esplorazione acustica.
4. **Verifica log**: Ispezionare la console SMAPI all'avvio e durante il gioco per confermare l'assenza di avvisi del tipo `[StardewAccessBridge] Errore...`.
