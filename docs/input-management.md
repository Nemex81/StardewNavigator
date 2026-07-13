# Gestione degli Input, Modificatori e Prevenzione Conflitti

Questo documento descrive in dettaglio le convenzioni tecniche per l'elaborazione dell'input di `StardewNavigator`, la gestione dei tasti modificatori, il comportamento dell'input simulato e le protezioni contro le collisioni con i lettori di schermo esterni.

---

## 1. Gestione dei Modificatori ed Evitamento Collisioni

L'assegnazione e la combinazione dei tasti modificatori (`LeftCtrl`, `LeftAlt`, `LeftShift`) in `NumpadController.cs` risponde a precisi requisiti fisici e software di accessibilità:

### A. AltGr / RightAlt (Contratto del Progetto)
* **Regola**: Il mod riconosce ed elabora come modificatore di ispezione **esclusivamente** il tasto `LeftAlt` (`SButton.LeftAlt`).
* **Vincolo Tecnico**: I tasti `RightAlt` ed `AltGr` (`SButton.RightAlt`) devono essere intenzionalmente ignorati e lasciati passare inalterati al sistema operativo. Questo previene collisioni con tastiere internazionali ed europee (inclusa quella italiana) che utilizzano la combinazione `AltGr` per comporre caratteri speciali (come parentesi, chiocciole, ecc.).

### B. Gestione Conflitti LeftShift e NVDA (Regola di Sviluppo)
* **Contesto e Storico**: Inizialmente i comandi del cursore di ispezione e le letture ambientali secondarie risiedevano sul livello `LeftShift + NumPad`. Questi sono stati migrati al livello `LeftAlt + NumPad`.
* **Motivazione NVDA**: Il lettore dello schermo NVDA (ampiamente utilizzato da giocatori non vedenti) riserva per default le combinazioni `Shift + NumPad` per il controllo del proprio "review cursor" a livello di sistema operativo. Tenere premuto Shift sul tastierino in-game creava conflitti di input bloccanti.
* **Obbligo di Analisi**: Non vi è un divieto assoluto di utilizzo del modificatore `Shift + NumPad`. Tuttavia, prima di assegnare o ripristinare in futuro qualsiasi binding facente uso di `Shift + NumPad`, è tassativo:
  1. Eseguire un'analisi formale dei potenziali conflitti con le scorciatoie predefinite di NVDA e JAWS.
  2. Valutare la raggiungibilità ergonomica a una sola mano per i giocatori con disabilità motorie o visive.
  3. Effettuare test di validazione in-game con una sessione NVDA attiva in background.

---

## 2. Cattura dell'Input e Soppressione Proattiva

Per garantire che le funzioni di accessibilità non provochino movimenti involontari del personaggio, StardewNavigator adotta una tecnica a due livelli:

### A. Priorità dell'Evento (Contratto del Progetto)
In `NavigatorFeature.cs`, l'evento di SMAPI `ButtonPressed` è decorato con l'attributo `[EventPriority(EventPriority.High)]`. Questo assicura che il gestore di StardewNavigator esamini l'input per primo e possa richiamare `Helper.Input.Suppress(e.Button)` per cancellare l'input prima che altre mod (con priorità standard o bassa) o il gioco stesso reagiscano ad esso.

### B. Soppressione Proattiva a livello di Frame (Comportamento Osservato)
* **Comportamento**: In `NumpadController.OnUpdateTicked`, se `LeftAlt` è tenuto premuto, viene invocata preventivamente la soppressione su base frame (`Suppress`) dei tasti direzionali (`NumPad8/2/4/6` e `Up/Down/Left/Right`).
* **Motivazione**: Poiché `stardew-access` viene caricato da SMAPI prima di `StardewNavigator`, il suo gestore `ButtonPressed` nativo viene talvolta notificato prima del nostro, catturando l'evento e avviando il movimento. Sopprimendo proattivamente i tasti a livello di tick prima che l'evento SMAPI venga distribuito, si azzera lo stato dell'input a livello globale per qualsiasi altra mod.

### C. Accoppiamento implicito con `GridMovementOverrideKey` (Comportamento Osservato)
* **Contesto**: Il micro-movimento fluido del personaggio (`LeftCtrl + NumPad 8/2/4/6`) funziona perché, nella configurazione di default di stardew-access, `GridMovementOverrideKey` è impostato su `LeftControl`. Finché `LeftCtrl` è premuto, stardew-access disattiva il grid snap, lasciando che il gioco gestisca il movimento nativo pixel-per-pixel.
* **Natura dell'accoppiamento**: Questo è un accoppiamento osservato sulla **configurazione** di stardew-access, non sul suo codice interno. Non è accessibile o controllabile via reflection da StardewNavigator.
* **Guardia anti-regressione**: Se l'utente modifica `GridMovementOverrideKey` nel `config.json` di stardew-access, il comportamento percepito del micro-movimento cambierà silenziosamente: il personaggio si muoverà a griglia invece che in modo fluido, senza alcun messaggio di errore. Questa dipendenza non deve essere trattata come una garanzia stabile. Qualsiasi modifica futura al livello `LeftCtrl` di StardewNavigator deve includere una verifica esplicita della coesistenza con il grid movement.

---

## 3. Simulazione di Input e Cooldown

Per innescare le azioni di gioco tramite il tastierino, simuliamo fisicamente gli input in modo controllato:

### A. Interazione / Azione (NumPad3)
* **Comportamento Osservato (stardew-access v1.7.0-beta.2)**: stardew-access intercetta le chiamate dirette a `Game1.currentLocation.checkAction` o `checkForAction` eseguite da mod esterne e restituisce `false` (bloccando l'apertura di casse/bauli).
* **Soluzione (Contratto del Progetto)**: In `NumpadActionCatalog.cs`, l'azione di `NumPad3` (`Interact`) simula la pressione del pulsante fisico nativo `SButton.X` tramite `ModEntry.Helper.Input.Press(SButton.X)`. Questo aggira il blocco e permette al ciclo nativo di gioco di eseguire l'azione in modo analogo a un click fisico.

### B. Cooldown e Simulazione su Uso Attrezzi (NumPad1)
* **Contratto del Progetto**: L'uso dell'attrezzo tramite `NumPad1` deve possedere un cooldown integrato per prevenire attivazioni a raffica incontrollate (rapid-fire) che consumerebbero l'energia del giocatore in pochi istanti.
* **Comportamento Unificato**: Il cooldown logico di **20 ticks** (~333 ms a 60 FPS, tempo stimato per un'animazione singola dell'attrezzo) memorizzato in `_lastUseToolTick` viene applicato **sempre** a livello di dispatcher in `NumpadController.cs`, sia con stardew-access che in standalone.
* **Simulazione Pressione Fisica per Spade**: Anziché chiamare il metodo di gioco logico `Game1.pressUseToolButton()` (che non funziona per le armi da mischia come la spada, in quanto non ereditano da `Tool`), l'azione `UseTool` in `NumpadActionCatalog.cs` rileva dinamicamente quale tasto è mappato nelle opzioni del gioco per l'uso dello strumento (`Game1.options.useToolButton`, di default `C` o click sinistro) e ne simula la reale pressione fisica tramite SMAPI.

### C. Personalizzazione Input e Keymapper (UI-2)
* **Configurazione dinamica**: Tramite la scorciatoia globale **`LeftAlt + T`** (catturata ad alta priorità in `NavigatorFeature.cs` quando il giocatore è libero) si accede in-game a un menu a tre livelli (`NumpadConfigMenu`) che consente di impostare override dinamici memorizzati in `ModConfig.NumpadOverrides`.
* **Disattivazione con `None`**: È possibile mappare un tasto all'azione `None` (valore `0` dell'enum `NumpadActionId`). Se il tasto fa parte dei 17 fisici del tastierino numerico, il controller sopprimerà l'input impedendo azioni involontarie, mentre se è un tasto esterno (es. frecce direzionali) lo lascerà passare al gioco in pass-through.
* **Protezione Azioni Critiche**: Il resolver centralizzato `NumpadProfileRegistry` impedisce la disattivazione o la rimozione totale delle 5 azioni critiche obbligatorie per un giocatore non vedente (`GridMoveUp`, `GridMoveDown`, `GridMoveLeft`, `GridMoveRight`, `OpenNavigatorMenu`), forzando il ripristino di fabbrica se non c'è almeno un tasto associato ad esse.
* **Regola di Cardinalità 1:1**: Tutte le azioni semantiche (ad eccezione delle quattro direzioni di `TileViewerMove*`) seguono una mappatura 1:1. L'associazione di un'azione a un tasto disattiva implicitamente il binding predefinito originale del profilo per quell'azione. Questa logica è risolta a runtime da `NumpadProfileRegistry.IsActionOverriddenElsewhere`.
* **Invariante di Validazione Configurazione**: La classe `NumpadConfigMenu` non consente di confermare le modifiche se `NumpadProfileRegistry.ValidateOverrides` restituisce `false`. Qualsiasi tentativo di salvare un mapping invalido attiva un feedback vocale di errore (`numpad-err-critical-unmapped`).

---

## 4. Checklist di Verifica dell'Input

L'agente deve convalidare ogni modifica ai binding fisici con questi controlli:

1. **AltGr Passthrough**: Avviare il gioco con layout di tastiera italiano. Verificare che la pressione di `AltGr` non provochi comportamenti imprevisti di ispezione e che il tasto agisca normalmente (es. se digitando in chat).
2. **Coesistenza NVDA**: Avviare NVDA sul PC. Verificare che i comandi `LeftAlt + NumPad` vengano intercettati dalla mod e non scatenino i controlli del review cursor di NVDA.
3. **Coerenza Standalone**: Rimuovere stardew-access. Verificare che `NumPad1` (Usa attrezzo) rispetti il cooldown di 20 tick e non consenta l'uso rapido continuo del tool tenendo premuto il tasto.
