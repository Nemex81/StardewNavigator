# Localizzazione e Gestione delle Stringhe (i18n)

Questo documento descrive il sistema di localizzazione di `StardewNavigator`, le convenzioni operative e le regole di manutenzione che l'agente deve seguire quando aggiunge o modifica stringhe localizzate.

---

## 1. Architettura del Sistema (Contratto del Progetto)

La localizzazione usa il sistema i18n nativo di SMAPI, basato su file JSON nella cartella `i18n/`.

### File presenti e ruoli

| File | Lingua | Ruolo |
|:---|:---|:---|
| `i18n/default.json` | Inglese (EN) | **Lingua di riferimento**. Contiene tutte le chiavi ufficiali del progetto. È la fonte di verità per struttura, token e naming. |
| `i18n/it.json` | Italiano (IT) | **Traduzione paritetica**. Deve contenere esattamente le stesse chiavi di `default.json`, nello stesso ordine di sezioni. |

### Lingua di riferimento e fallback

`default.json` è la lingua di riferimento del progetto. SMAPI ricade silenziosamente su `default.json` per qualsiasi chiave assente nel file della lingua attiva dell'utente. Questo meccanismo è una rete di sicurezza per errori accidentali, **non un meccanismo intenzionale**: qualsiasi chiave mancante in `it.json` produce output in inglese per gli utenti italiani, interrompendo il flusso vocale. Per questa ragione la parità è obbligatoria (vedi Sezione 5).

### Supporto a lingue aggiuntive

Il sistema è già predisposto per lingue ulteriori. Aggiungere una terza lingua richiede esclusivamente la creazione di un file `i18n/<locale>.json` con le stesse chiavi di `default.json`. Non sono necessarie modifiche al codice C#. Le lingue ufficialmente mantenute dal progetto sono attualmente EN e IT.

---

## 2. Tassonomia delle Chiavi

Le chiavi sono organizzate per prefisso. Ogni prefisso identifica un ambito funzionale distinto.

### `menu-navigator-*`
Stringhe dell'interfaccia del menu di navigazione in-game (`NavigatorMenu.cs`).
- Chiavi con suffisso `_speak`: testo destinato allo screen reader (letto da `NavigatorSpeaker.Say`). Spesso differiscono dalle chiavi visive per essere più lineari e prive di simboli come `:` o `[...]`.
- Chiavi senza suffisso `_speak`: testo visivo mostrato nel menu testuale.

Esempi: `menu-navigator-choose_map`, `menu-navigator-choose_map_speak`, `menu-navigator-arrived_at_poi`.

### `nav.map.*` e `nav.poi.*`
Nomi localizzati di mappe e punti di interesse. Usati esclusivamente in `navigator_destinations.json` come riferimenti simbolici, risolti a runtime (vedi Sezione 3).

Esempi: `nav.map.farm`, `nav.poi.seedshop`.

### `config.*`
Etichette e tooltip esposti nel menu GMCM (Generic Mod Config Menu). Convenzionalmente in coppia: `config.<campo>.name` e `config.<campo>.tooltip`.

Esempi: `config.navigator-menu-key.name`, `config.hud-duration.tooltip`.

### `numpad-*`
Stringhe di feedback vocale ed HUD generate ed utilizzate dal sottosistema del tastierino numerico (`NumpadController.cs`, `TileInspector.cs`, `GridMovement.cs`, `NumpadActionCatalog.cs`, `NumpadConfigMenu.cs`). Includono sia messaggi di stato che stringhe di lettura tile ed output di coordinate.

Esempi: `numpad-coords`, `numpad-read-tile-water`, `numpad-health-stamina`.

### Chiavi singole (non prefissate per categoria)
- `update-available`: notifica di aggiornamento disponibile (usata in `ModEntry.cs`).

---

## 3. Ruolo di `navigator_destinations.json` e Risoluzione Lazy

Il file `assets/navigator_destinations.json` **non contiene mai testo localizzato diretto**. Contiene esclusivamente chiavi i18n come valori dei campi `MapDisplayName` e `DisplayName`:

```json
{
  "MapDisplayName": "nav.map.farm",
  "MapLocationName": "Farm",
  "PointsOfInterest": [
    { "DisplayName": "nav.poi.farmhouse", "TargetLocationName": "FarmHouse" }
  ]
}
```

`DestinationRegistry.ResolveCoordinates()` chiama `Helper.Translation.Get()` ad ogni evento `SaveLoaded` per sostituire queste chiavi con le stringhe localizzate nella lingua corrente del giocatore. Questo garantisce che il cambio lingua in-game si rifletta nelle destinazioni senza riavvio del gioco.

**Vincolo**: Non inserire mai testo in inglese o italiano direttamente in `navigator_destinations.json`. L'unica eccezione consentita è `MapLocationName`, che è il nome tecnico interno della location SMAPI (non tradotto, usato per il pathfinding).

---

## 4. Convenzioni di Naming delle Chiavi

- **Separatore**: `-` (trattino) per i componenti del nome. Il trattino basso `_` è ammesso solo in casi preesistenti per compatibilità (es. `menu-navigator-choose_map`).
- **Struttura**: `<prefisso>-<ambito>-<azione_o_stato>` (es. `numpad-read-tile-water`, `menu-navigator-arrived_at_poi`).
- **Lowercase**: le chiavi sono sempre in lowercase.
- **Chiavi secondarie per composizione**: quando una stringa è costruita combinando parti (es. il tipo di albero iniettato in una stringa più grande), la parte secondaria ha una propria chiave stand-alone. Il codice C# la risolve separatamente e ne inietta il valore come token nella stringa principale.

  Esempio:
  ```csharp
  var resolvedTreeName = helper.Translation.Get($"numpad-read-tile-tree-type-{tree.treeType.Value}");
  return helper.Translation.Get("numpad-read-tile-tree", new { tree_name = resolvedTreeName, stage = ... });
  ```

### Localizzazione di annunci dinamici e stati
Non comporre mai nel codice C# messaggi vocali unendo lemmi o pluralizzazioni hardcoded (ad esempio unendo il numero al suffisso `"items."` o impostando lo stato `"Empty."` come stringa letterale). 
Tutte le risposte vocali composte e gli stati dinamici devono fare uso di chiavi i18n configurate con token (es. `numpad-config-item-count` con il parametro `{{count}}`) o chiavi statiche dedicate (es. `numpad-config-empty`).

---

## 5. Token di Interpolazione `{{...}}`

SMAPI usa la sintassi `{{token_name}}` per i parametri variabili nelle stringhe.

### Regole sui token

- **I nomi dei token non si traducono**: `{{map_name}}`, `{{poi_name}}`, `{{version}}` ecc. sono identici in `default.json` e in tutti i file di traduzione.
- **I token fanno parte del contratto**: aggiungere, rinominare o rimuovere un token da una chiave in `default.json` richiede la modifica corrispondente in:
  1. Il codice C# che chiama `Helper.Translation.Get("chiave", new { token_name = value })`.
  2. `it.json` (il token deve apparire nella stringa tradotta nella posizione appropriata).
- **Token inutilizzati**: SMAPI ignora silenziosamente i token passati via codice ma non presenti nella stringa del file JSON. È un comportamento atteso per le varianti regionali che scelgono di non usare un'interpolazione.

---

## 6. Regola di Parità Obbligatoria EN/IT

**Ogni chiave presente in `default.json` deve esistere anche in `it.json`.**

Questa è una regola assoluta, senza eccezioni previste, motivata dalla natura dell'utenza:
- I giocatori non vedenti con sistema operativo in italiano si affidano a SMAPI con la lingua impostata su IT.
- Un fallback silenzioso su EN produce una stringa in inglese nel mezzo di una sequenza vocale NVDA in italiano, interrompendo il flusso cognitivo dell'utente.
- Il fallback di SMAPI è una rete di sicurezza per errori di manutenzione accidentali, non un meccanismo di design.

**Meccanismo di verifica**: Dopo ogni modifica ai file i18n, `git diff i18n/` deve mostrare lo stesso numero di chiavi aggiunte o modificate in `default.json` e in `it.json`.

---

## 7. Workflow Obbligatorio per Aggiungere una Nuova Stringa

1. **Aggiungere la chiave in `default.json`** con il valore in inglese, nella sezione tematica pertinente.
2. **Aggiungere immediatamente la stessa chiave in `it.json`** con la traduzione italiana, nella stessa posizione relativa.
3. **Usare la chiave nel codice C#** tramite `Helper.Translation.Get("chiave")` o `Helper.Translation.Get("chiave", new { token = value })` e chiamare `.ToString()` sul risultato.
4. **Se la chiave verrà usata in `navigator_destinations.json`**, inserirla solo come valore simbolico stringa; non aggiungere codice aggiuntivo — la risoluzione è automatica in `DestinationRegistry`.
5. **Verificare prima del commit**: `git diff i18n/` deve mostrare aggiunte bilanciate in entrambi i file.

---

## 8. Checklist di Verifica per l'Agente

Prima di completare qualsiasi task che tocca stringhe localizzate:

1. **Parità chiavi**: il numero di chiavi in `default.json` e `it.json` è identico.
2. **Token coerenti**: ogni token usato in una chiave di `default.json` appare anche nella corrispondente stringa di `it.json`.
3. **Nessun testo diretto in `navigator_destinations.json`**: i campi `MapDisplayName` e `DisplayName` contengono solo chiavi i18n.
4. **Nessuna stringa hardcoded nel codice C#**: ogni testo user-facing passa da `Helper.Translation.Get()`.
5. **Build pulita**: `dotnet build --configuration Release` non produce avvisi relativi a chiavi i18n mancanti o mal formate.
