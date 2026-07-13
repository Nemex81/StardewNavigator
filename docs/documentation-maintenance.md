# Manutenzione Documentale e Conformità Codice-Istruzioni

Questo documento stabilisce il processo di audit permanente per garantire che il sistema di istruzioni del progetto (`GEMINI.md` e la documentazione in `docs/`) rimanga coerente con il codice C# a seguito di qualsiasi modifica significativa a runtime.

---

## 1. Trigger di Audit Obbligatorio

L'agente deve avviare una sessione di verifica e conformità documentale subito dopo aver completato modifiche riguardanti uno dei seguenti ambiti:
*   **Refactoring strutturali** o estrazioni di nuove classi/moduli.
*   **Riassegnazione o ridistribuzione delle responsabilità** tra componenti del tastierino numerico o del navigatore.
*   **Cambiamenti nel flusso degli input** (intercettazioni, repeat loop, cooldown, invarianti del keymapper).
*   **Aggiunte o modifiche alla localizzazione (i18n)** o al sistema di sintesi vocale.
*   **Integrazioni esterne**, reflection e coesistenza con `stardew-access`.
*   **Nuove regole o invarianti di comportamento** del mod (configurazione, menu, ecc.).

---

## 2. Procedura di Verifica e Allineamento

Dopo il completamento e la compilazione con successo del codice modificato, l'agente deve:

### A. Ispezione dei File
*   Leggere i file di documentazione pertinenti ed individuare descrizioni fisiche obsolete (es. riferimenti a classi rimosse, firme di metodi superate, flussi non più coerenti).
*   Confrontare le assunzioni teoriche dei documenti con la reale implementazione del codice C#.

### B. Gestione delle Discrepanze
*   **Nessuna discrepanza**: Se la documentazione descrive accuratamente il codice modificato, chiudere la verifica senza applicare modifiche.
*   **Discrepanza individuata**: Se un testo descrive comportamenti obsoleti, non più implementati o incompleti:
    1.  **Non modificare i file markdown** in modo automatico.
    2.  **Presentare un report all'utente** descrivendo il problema specifico, spiegando il potenziale rischio di regressione per futuri agenti, e indicando la correzione minima necessaria.
    3.  **Richiedere approvazione esplicita** prima di procedere alla scrittura/sostituzione dei file `.md`.

---

## 3. Criteri di Qualità Documentale

*   **Minimo intervento**: Correggere o aggiungere solo le sezioni strettamente disallineate, evitando riscritture massive o alterazioni di stile dei documenti esistenti.
*   **Nessuna feature nascosta**: Qualsiasi limitazione tecnica, workaround (es. emulazione di `useToolButton` o rimozione di sottomenu) o invariante di sicurezza implementata nel codice deve essere descritta in modo chiaro e conciso nella documentazione.
*   **Coerenza terminologica**: Utilizzare gli stessi nomi e identità di classi e metodi usati nel codice del progetto (es. `TileInspector`, `GridMovement`, `NumpadActionCatalog`).
