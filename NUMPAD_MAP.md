# StardewNavigator - Mappatura Tastierino Numerico (Numpad)

Questo documento descrive in dettaglio la mappatura completa dei comandi assegnati al tastierino numerico da StardewNavigator. Questi comandi sono attivi solo quando il **NumLock è ATTIVO** e il giocatore è libero (non sta digitando in chat e, generalmente, non è all'interno di un menu, ad eccezione del menu di costruzione dove alcuni controlli sono consentiti).

La mappatura è strutturata in livelli ("layers") accessibili tramite l'uso di tasti modificatori (`LeftCtrl`, `LeftShift`, `LeftAlt`).

---

## 1. Livello Base (Nessun modificatore)

Comandi di locomozione, interazione primaria e lettura ambientale di base.

| Tasto Numpad | Azione | Note / Emulazione |
| :--- | :--- | :--- |
| **8, 2, 4, 6** | Movimento a griglia (Nord, Sud, Ovest, Est) | Delega a `stardew-access` se presente, altrimenti fallback locale. |
| **1** | Usa Attrezzo | Emula la pressione del tasto `X`. |
| **3** | Azione / Interazione | Emula la pressione del tasto `C`. |
| **5** | Leggi Tile di fronte | Lettura del blocco davanti al giocatore. |
| **7** | Slot precedente hotbar | Seleziona lo slot attivo precedente della toolbar (wrapping circolare su 12 slot). |
| **0** | Leggi Coordinate | Emula il tasto `K` di stardew-access. |
| **9** | Slot successivo hotbar | Seleziona lo slot attivo successivo della toolbar (wrapping circolare su 12 slot). |

---

## 2. Comandi Scanner e Utility (Nessun modificatore)

Tasti operativi e navigazione categorie scanner.

| Tasto Numpad | Azione | Note / Emulazione |
| :--- | :--- | :--- |
| **`*` (Moltiplica)** | Apri Inventario | Apre il `GameMenu` nativo (tab Inventario), speculare a `E` o `Esc`. |
| **`/` (Dividi)** | Apri Menu Navigatore | Alternativa ergonomica al tasto `9` e `G`. |
| **`+` (Più)** | Ciclo Scanner: Gruppo Oggetti (Su) | Speculare a `PageUp`. |
| **`-` (Meno)** | Ciclo Scanner: Gruppo Oggetti (Giù) | Speculare a `PageDown`. |

---

## 3. Livello LeftCtrl (Micro-movimento e Azioni di Sistema)

Livello dedicato al controllo fisico fluido e alla gestione del navigatore.

| Tasto Numpad | Azione | Note / Emulazione |
| :--- | :--- | :--- |
| **Ctrl + 8, 2, 4, 6** | Micro-movimento (fluido) | Bypass del GridMovement, permette al gioco di gestire l'input nativamente pixel-per-pixel. |
| **Ctrl + 5** | Auto-Walk al target | Avvia il movimento automatico verso il target corrente (emula `LeftCtrl + Home`, alias di Ctrl+0). |
| **Ctrl + 9** | Annulla Navigazione | Interrompe il routing BFS attivo. |
| **Ctrl + 0** | Auto-Walk al target | Avvia il movimento automatico verso il target corrente (emula `LeftCtrl + Home`). |
| **Ctrl + `+`** | Ciclo Scanner: Categoria (Su) | Speculare a `LeftCtrl + PageUp`. |
| **Ctrl + `-`** | Ciclo Scanner: Categoria (Giù) | Speculare a `LeftCtrl + PageDown`. |

---

## 4. Livello LeftShift (Esplorazione Spaziale)

Livello dedicato all'esplorazione virtuale tramite TileViewer, senza muovere il personaggio.

| Tasto Numpad | Azione | Note / Emulazione |
| :--- | :--- | :--- |
| **Shift + 8, 2, 4, 6** | Muovi cursore TileViewer | Sposta il cursore di esplorazione di 64px nella direzione scelta. |
| **Shift + 5** | Leggi Coordinate | Rilegge posizione e display name. |
| **Shift + 9** | Stato Navigazione | Annuncia lo stato della route in corso. |
| **Shift + `+`** | Ciclo Scanner: Oggetto in Gruppo (Su)| Speculare a `LeftShift + PageUp`. |
| **Shift + `-`** | Ciclo Scanner: Oggetto in Gruppo (Giù)| Speculare a `LeftShift + PageDown`. |

---

## 5. Livello LeftAlt (Stato Vitale, Oggetto Selezionato & Tile sotto i piedi)

| Tasto Numpad | Azione | Note / Emulazione |
| :--- | :--- | :--- |
| **Alt + 5** | Leggi Tile sotto i piedi | Emula il comando `LeftAlt + J` di stardew-access. |
| **Alt + 0** | Leggi Salute / Energia | Assegnazione ergonomica alternativa per le statistiche vitali. |
| **Alt + 7** | Leggi Oggetto Impugnato | Annuncia l'oggetto attualmente selezionato nella hotbar (alias). |
| **Alt + 9** | Leggi Oggetto Impugnato | Annuncia l'oggetto attualmente selezionato nella hotbar (alias). |

---

## 6. Tasti Volutamente Non Assegnati o Rimossi

Al fine di evitare collisioni e mantenere la configurazione snella, le seguenti combinazioni **non intercettano input** e lasciano passare il comando al motore di gioco (o ad altre mod):

* **Shift + 0**: Rimosso intenzionalmente. La funzione Auto-Walk è stata spostata esclusivamente su `Ctrl + 0`.
* **Ctrl/Shift + 7**: Nessuna assegnazione. `Ctrl+7` e `Shift+7` non sono gestiti.
* **Modificatori (Ctrl/Alt/Shift) + `*` (Moltiplica)**: Nessuna assegnazione. `Multiply` e `Divide` senza modificatori aprono rispettivamente Inventario e Menu Navigatore — funzioni standalone non dipendenti da stardew-access.
* **Modificatori (Ctrl/Alt/Shift) + `/` (Dividi)**: Nessuna assegnazione.
* **Altre combinazioni non elencate**: Se non specificato sopra, la combinazione non è gestita da NumpadController.

---
*Documentazione generata conformemente all'implementazione reale nel file `NumpadController.cs`.*
