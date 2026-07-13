# StardewNavigator - Mappatura Tastierino Numerico (Numpad)

Questo documento descrive in dettaglio la mappatura completa dei comandi assegnati al tastierino numerico da StardewNavigator. Questi comandi sono attivi solo quando il **NumLock è ATTIVO** e il giocatore è libero (non sta digitando in chat e, generalmente, non è all'interno di un menu, ad eccezione del menu di costruzione dove alcuni controlli sono consentiti).

La mappatura è strutturata in livelli ("layers") accessibili tramite l'uso di tasti modificatori (`LeftCtrl` e `LeftAlt`, oltre a `LeftShift` per i soli controlli dello scanner).

---

## 1. Livello Base (Nessun modificatore)

Comandi di locomozione, interazione primaria e lettura ambientale di base.

| Tasto Numpad | Azione | Note / Emulazione |
| :--- | :--- | :--- |
| **8, 2, 4, 6** | Movimento a griglia (Nord, Sud, Ovest, Est) | Delega a `stardew-access` se presente, altrimenti fallback locale (`GridMovement`). |
| **1** | Usa Attrezzo | Simula la pressione fisica del tasto configurato in gioco (default: `C` / Left Click) con cooldown 20 ticks (~333ms). Valido per tutti i giocatori (con/senza stardew-access, vedenti e non vedenti) e supporta le armi da mischia (spade). |
| **3** | Azione / Interazione | Simula la pressione fisica di `SButton.X` sulla tile di fronte (apre bauli, parla con NPC, interagisce con macchine). Evita le intercettazioni bloccanti dei chest. |
| **5** | Leggi Tile di fronte | Lettura del blocco davanti al giocatore (tramite `stardew-access` o fallback locale `TileInspector`). |
| **7** | Slot precedente hotbar | Seleziona lo slot attivo precedente della toolbar (wrapping circolare su 12 slot). |
| **0** | Leggi Coordinate | Emula il tasto `K` di stardew-access (fallback locale TTS). |
| **9** | Slot successivo hotbar | Seleziona lo slot attivo successivo della toolbar (wrapping circolare su 12 slot). |
| **`.` (Decimal)** | Alias di Enter (tutti i contesti) | Simula `Enter` nel mondo, nell'inventario, nel Menu Navigatore e in qualsiasi altro menu. Non richiede modificatori. `SButton.Decimal` ≠ `OemPeriod` (tastiera alfanumerica). |

---

## 2. Comandi Scanner e Utility (Nessun modificatore)

Tasti operativi e navigazione categorie scanner.

| Tasto Numpad | Azione | Note / Emulazione |
| :--- | :--- | :--- |
| **`*` (Moltiplica)** | Apri Inventario | Apre il `GameMenu` nativo (tab Inventario), speculare a `E` or `Esc`. |
| **`/` (Dividi)** | Apri Menu Navigatore | Alternativa ergonomica al tasto `9` e `G`. *(Nota: la voce "Configura Tastierino" è stata rimossa da questo menu ed è ora accessibile direttamente tramite la scorciatoia `LeftAlt + T`)*. |
| **`+` (Più)** | Ciclo Scanner: Gruppo Oggetti (Su) | Speculare a `PageUp`. |
| **`-` (Meno)** | Ciclo Scanner: Gruppo Oggetti (Giù) | Speculare a `PageDown`. |

---

## 2b. Tasti numerici attivi nel Menu Navigatore

Quando il Menu Navigatore è aperto (con `G` o `/`), i seguenti tasti del tastierino numerici sono attivi per navigare e confermare senza usare la tastiera principale.

| Tasto Numpad | Azione | Note |
| :--- | :--- | :--- |
| **8** | Voce precedente | Sposta il cursore verso l'alto nella lista destinazioni o POI. |
| **2** | Voce successiva | Sposta il cursore verso il basso nella lista destinazioni o POI. |
| **Ctrl + 5** | Conferma selezione | Conferma la destinazione/POI selezionato e avvia il percorso automatico. |
| **`LeftAlt + T`** | Configura Tastierino | **Scorciatoia globale** in-game per aprire il Numpad Configuration Menu a tre livelli (disponibile nel mondo quando il giocatore è libero). Permette di mappare i tasti ad azioni o disattivarli assegnandoli a `None`. |

---

## 2c. `LeftCtrl + 5` in qualsiasi menu aperto (inventario, negozi, dialoghi, ecc.)

`LeftCtrl + NumPad5` è un binding **context-aware**: il suo comportamento cambia in base al contesto attivo.

| Contesto | Comportamento |
| :--- | :--- |
| **Menu Navigatore aperto** | Conferma la destinazione/POI corrente e avvia il percorso automatico |
| **Qualsiasi altro menu aperto** (GameMenu, negozi, dialoghi…) | Alias di `LeftCtrl + Enter` → conferma la selezione attiva nel menu |
| **Nessun menu (nel mondo)** | Avvia Auto-Walk verso l'oggetto selezionato nell'Object Tracker (`≡ LeftCtrl + Home`) |

---

## 3. Livello LeftCtrl (Micro-movimento e Azioni di Sistema)

Livello dedicato al controllo fisico fluido e alla gestione del navigatore.

| Tasto Numpad | Azione | Note / Emulazione |
| :--- | :--- | :--- |
| **Ctrl + 8, 2, 4, 6** | Micro-movimento (fluido) | Bypass del GridMovement, permette al gioco di gestire l'input nativamente pixel-per-pixel. |
| **Ctrl + 5** | Auto-Walk / Conferma menu | Nel mondo: avvia Auto-Walk verso l'oggetto Object Tracker (`≡ LeftCtrl + Home`). In qualsiasi menu aperto: alias di `LeftCtrl + Enter`. |
| **Ctrl + 9** | Annulla Navigazione | Interrompe il routing BFS attivo. |
| **Ctrl + 0** | Alias LeftCtrl + Enter | Emula la pressione di `LeftCtrl + Enter` per eseguire Left Click nei menu e Auto-Walk nel mondo. |
| **Ctrl + `+`** | Ciclo Scanner: Categoria (Su) | Speculare a `LeftCtrl + PageUp`. |
| **Ctrl + `-`** | Ciclo Scanner: Categoria (Giù) | Speculare a `LeftCtrl + PageDown`. |

---

## 4. Livello LeftShift (Scanner)

Livello utilizzato esclusivamente per lo scanner dell'Object Tracker:

| Tasto Numpad | Azione | Note / Emulazione |
| :--- | :--- | :--- |
| **Shift + `+`** | Ciclo Scanner: Oggetto in Gruppo (Su)| Speculare a `LeftShift + PageUp`. |
| **Shift + `-`** | Ciclo Scanner: Oggetto in Gruppo (Giù)| Speculare a `LeftShift + PageDown`. |

> [!IMPORTANT]
> I precedenti comandi del livello `LeftShift + NumPad1..9` sono stati migrati al livello `LeftAlt` per evitare conflitti con gli shortcut di lettura dello screen reader NVDA.

---

## 5. Livello LeftAlt (TileViewer, Coordinate, Stato Vitale, Oggetto Selezionato & Tile sotto i piedi)

Questo livello è stato riorganizzato per includere i comandi del TileViewer e delle coordinate (migrati da Shift), risolvendo i conflitti con NVDA.

| Tasto Numpad | Azione | Note / Emulazione |
| :--- | :--- | :--- |
| **Alt + 8, 2, 4, 6** | Muovi cursore TileViewer | Sposta il cursore di esplorazione di 64px nella direzione scelta. |
| **Alt + 3** | Leggi Tile sotto i piedi | Emula il comando `LeftAlt + J` di stardew-access (spostato da Alt+5 per evitare conflitti). |
| **Alt + 5** | Leggi Coordinate | Rilegge posizione e display name (spostato da Shift+5). |
| **Alt + 7** | Leggi Oggetto Impugnato | Annuncia l'oggetto attualmente selezionato nella hotbar (unico binding rimasto). |
| **Alt + 9** | Stato Navigazione | Annuncia lo stato della route in corso (spostato da Shift+9; il precedente binding Alt+9 per l'oggetto impugnato è stato rimosso). |
| **Alt + 0** | Leggi Salute / Energia | Assegnazione ergonomica alternativa per le statistiche vitali. |

---

## 6. Tasti Volutamente Non Assegnati o Rimossi

Al fine di evitare collisioni e mantenere la configurazione snella, le seguenti combinazioni **non intercettano input** e lasciano passare il comando al motore di gioco (o ad altre mod):

* **Shift + 0**: Rimosso intenzionalmente. La funzione Auto-Walk è stata spostata esclusivamente su `Ctrl + 0`.
* **Ctrl/Shift + 7**: Nessuna assegnazione. `Ctrl+7` e `Shift+7` non sono gestiti.
* **Modificatori (Ctrl/Alt/Shift) + `*` (Moltiplica)**: Nessuna assegnazione. `Multiply` e `Divide` senza modificatori aprono rispettivamente Inventario e Menu Navigatore — funzioni standalone non dipendenti da stardew-access.
* **Modificatori (Ctrl/Alt/Shift) + `/` (Dividi)**: Nessuna assegnazione.
* **Altre combinazioni non elencate**: Se non specificato sopra, la combinazione non è gestita dal controller.

---
*Documentazione allineata all'architettura reale implementata in `NumpadController.cs`, `NumpadActionCatalog.cs`, `TileInspector.cs` e `GridMovement.cs`.*
