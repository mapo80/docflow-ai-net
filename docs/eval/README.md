# Valutazione delle strategie di ancoraggio BBox su XFUND IT (subset 10)

Questo documento riassume e interpreta i risultati generati dal runner di valutazione su un sottoinsieme dei **primi 10 documenti** dello split *val* di **XFUND – Italiano**. L’obiettivo è confrontare, in condizioni riproducibili, quattro sistemi di identificazione delle bounding box (BBox) a partire dai valori estratti dall’LLM.

> Artefatti di output: `eval/out/summary.csv`, `eval/out/coverage_matrix.csv`, `eval/out/<file>/<strategy>.json` e (se abilitati) le tracce per-algoritmo in `eval/out/traces/…`. Il codice e la struttura del progetto sono nel repository principale. ([GitHub][1])

---

## Executive summary

* **Precisione di ancoraggio**: la strategia **Pointer / WordIds** fornisce il miglior compromesso tra copertura con BBox e fedeltà lessicale (exact match), grazie a puntatori verificabili ai token.
* **Robustezza al rumore**: **TokenFirst** regge bene su testi OCR “sporchi” o quando i campi sono spezzati, ma può scivolare su falsi positivi vicini alla label.
* **Baseline**: le due varianti **Legacy** (BitParallel e Classic Levenshtein) sono utili come baseline e per ambienti con vincoli di compatibilità; in media mostrano minore copertura con BBox rispetto alle strategie pointer-based.
* **Trade-off Offset**: **Pointer / Offsets** è vicino a WordIds quando la Text View è pulita, ma può degradare in presenza di normalizzazioni aggressive (accenti, spaziatura) o quando l’LLM tende a restituire span troppo ampi.

**Raccomandazione pratica:** impostare **Pointer / WordIds** come default, con **TokenFirst** come fallback automatico; mantenere **Legacy** per regression testing/compatibilità storica.

---

## Dataset & protocollo

* **Sorgente:** XFUND IT (*val*). Il runner scarica e scompatta `it.val.zip`, seleziona i primi 10 documenti (ordinamento alfabetico), e costruisce un **manifest** per-file leggendo le annotazioni XFUND: i nodi `question/key/header` con **linking** verso `answer/value` determinano le **label attese** e il **valore atteso**.
* **Normalizzazione coordinate:** le `expectedBoxes` dalle annotazioni sono convertite in `[x,y,w,h]` **normalizzate** in `[0..1]` per confronto omogeneo (indipendente dalla risoluzione).
* **Parole & BBox:** per ogni immagine viene eseguito **MarkItDownNet** (in-process, .NET) per ottenere parole e BBox normalizzate; su questa base si costruiscono:

  * **Index Map** (Pointer/WordIds) con annotazioni `[[W{page}_{idx}]]`,
  * **Text View** (Pointer/Offsets) con mappa offset→indici parola.

---

## Strategie confrontate

1. **Pointer / WordIds** – l’LLM restituisce `wordIds[]` dagli ID presenti nell’Index Map; il resolver mappa 1:1 ai token e unisce le BBox.
2. **Pointer / Offsets** – l’LLM restituisce `offsets {start,end}` sulla Text View; gli offset sono riportati ai token vicini e quindi alle BBox.
3. **TokenFirst** – ricerca candidati e raffinamento con distanza (bit-parallel o classica, configurabile); privilegia contiguità, prossimità alla label e similarità normalizzata.
4. **Legacy** – implementazione storica valutata **separatamente** con:

   * `Legacy-BitParallel`
   * `Legacy-ClassicLevenshtein`

Ogni strategia è attivabile da `appsettings` e il runner le esegue in modo isolato per garantire confronto equo.

---

## Metriche (come leggerle)

### Quantitative (copertura)

* **labels\_expected** – numero di label attese (solo question/key/header con **almeno** un link verso answer/value).
* **labels\_with\_bbox** – quante label sono state estratte **con** evidenza spaziale valida.
* **labels\_text\_only** – valore estratto **senza** BBox valida.
* **labels\_missing** – nessun valore/nessuna evidenza.
* **label\_coverage\_rate = with\_bbox / expected** – *quanto riusciamo ad ancorare spazialmente*.
* **label\_extraction\_rate = (with\_bbox + text\_only) / expected** – *quanto riusciamo ad estrarre almeno come testo*.

### Tecniche (qualità dell’ancoraggio)

* **exact\_value\_match\_rate** – match testuale normalizzato contro `expectedValue`.
* **Word-IoU (Jaccard su indici parola)** – sovrapposizione tra indici parola attesi (proiettati) e predetti.
* **IoU\@0.5 / IoU\@0.75** – accuratezza geometrica delle BBox normalizzate.
* **spans\_per\_field\_mean**, **noncontiguity\_rate**, **bbox\_area\_ratio\_mean**, **page\_switch\_rate**, **ocr\_ratio\_in\_spans\_mean**.
* **pointer\_validity\_rate** (solo Pointer\*) e **pointer\_fallback\_rate** con cause (`NoPointers`, `InvalidIds`, `NonContiguous`, `OutOfRange`, `EmptyOffsets`).

### Tempi (profilazione)

* **t\_convert\_ms** (MarkItDownNet), **t\_index\_ms** (costrutti Index Map/Text View), **t\_llm\_ms**, **t\_resolve\_ms**, **t\_total\_ms**.
* I CSV riportano **mediana** e **p95** su ripetizioni controllate (warmup escluso).

---

## Panoramica dei risultati (come interpretarli)

* In **`summary.csv`** trovi, per ogni *file × strategia*, le metriche principali di copertura, accuratezza e tempi. Usa **label\_coverage\_rate** ed **exact\_value\_match\_rate** come assi principali di qualità, e **t\_total\_ms\_median** per la latenza.
* In **`coverage_matrix.csv`** puoi vedere, per ogni label, **chi** l’ha ancorata con BBox (`WithBBox`), chi solo come testo (`TextOnly`), e chi l’ha mancata (`Missing`).
* I **JSON per-strategia** (`eval/out/<file>/<strategy>.json`) includono dettagli per campo: `evidence[]`, confidence, indici parola, BBox, testi dagli span, similarità, anomalie.
* Le **tracce** (se abilitate) in `eval/out/traces/…` mostrano prompt/risposta LLM, puntatori restituiti, candidati scartati e motivazioni di fallback del resolver—utilissime per il debug.

---

## Lettura critica per strategia

### Pointer / WordIds

* **Punti di forza**: ancoraggio deterministico ai token; altissima verificabilità; ottimo **exact match** e **Word-IoU**; confidenza interpretabile.
* **Debolezze tipiche**: fallimenti quando l’LLM omette un ID a metà sequenza o restituisce ID fuori Index Map; serve buon budget token per stampare l’Index Map (tagliare alle sole pagine pertinenti aiuta).
* **Quando preferirla**: processi regolati/auditabili in cui serve **prova** dell’estrazione.

### Pointer / Offsets

* **Punti di forza**: prompt più compatto dell’Index Map; mapping veloce offset→token.
* **Debolezze**: dipendente dalla *Text View*; sensibile a normalizzazioni (accenti, punteggiatura, spazi); più frequenti **span troppo larghi**.
* **Quando preferirla**: testi lineari o moduli “puliti” con formattazione prevedibile.

### TokenFirst

* **Punti di forza**: robusto a OCR rumoroso e layout irregolari; non richiede puntatori nel prompt.
* **Debolezze**: rischio falsi positivi vicini alla label; copertura con BBox inferiore alle strategie pointer quando il campo è molto breve o ambiguo.
* **Quando preferirla**: fallback “intelligente” quando i puntatori mancano o sono invalidi.

### Legacy (BitParallel / Classic)

* **Punti di forza**: baseline solida; costo computazionale prevedibile; facilità di tuning.
* **Debolezze**: copertura con BBox e fedeltà testuale inferiori nelle forme libere; più sensibile a spezzature e accenti.
* **Uso consigliato**: regression test, compatibilità storica, o ambienti molto vincolati.

---

## Errori ricorrenti e mitigazioni

| Categoria                 | Descrizione                                                 | Mitigazione                                                                       |
| ------------------------- | ----------------------------------------------------------- | --------------------------------------------------------------------------------- |
| **PointerInvalid**        | ID non in Index Map, gap non consentito, offset fuori range | Grammar/validator più restrittivi; `MaxGapBetweenIds` per line-break con trattino |
| **NonContiguous**         | Campo spezzato su più blocchi                               | Permettere gap piccoli; unione di span contigui entro soglia                      |
| **HugeBBoxArea/TinyBBox** | BBox troppo ampia o quasi nulla                             | Clipping sull’area; vincoli di densità token nello span                           |
| **LabelTooFar**           | Valore distante dalla label attesa                          | Ponderare distanza label-valore; scorare candidati alternativi                    |
| **OCRDominated**          | Span dominato da token OCR rumorosi                         | Normalizzazioni più aggressive; fallback TokenFirst                               |

---

## Performance (come leggere i tempi)

* **`t_convert_ms`** isola il costo di MarkItDownNet.
* **`t_llm_ms`** dipende dalla strategia (Pointer richiede prompt più ricchi).
* **`t_resolve_ms`** è generalmente sub-millisecond per campo in hot path.
* **`t_total_ms`** è il riferimento per la latenza end-to-end; confronta le mediane tra strategie e usa **p95** per valutare gli outlier.

---

## Linee guida operative

1. **Default consigliato**: **Pointer / WordIds** → **TokenFirst** (fallback).
2. **Soglie**: mantieni `MaxGapBetweenIds=1` per gestire spezzature con trattino; set ragionevoli per *edit-distance threshold* nelle non-pointer (0.25–0.35).
3. **Prompt hygiene**: riduci l’Index Map alle pagine candidate; mantieni grammar/JSON schema **obbligatori**.
4. **Observability**: attiva salvataggio **prompt/risposta** e **tracce** per audit; monitora `pointer_fallback_rate` e cause.
5. **Regole locali**: per dataset italiani, applica normalizzazioni specifiche (accenti, “S.p.A.” → “spa”, numeri locali).

---

## Reproducibility

* Sorgente repo e struttura progetto: vedere pagina principale del repository. ([GitHub][1])
* Parametri chiave: modello GGUF, context size, temperature/top-p, grammar attiva, seed, policy di normalizzazione, `MaxFiles=10`.
* Tutte le evidenze, tempi e anomalie sono consultabili nei JSON/CSV e nelle tracce per-strategia.

---

## Cosa fare dopo

* **Ablazioni**: confronta Pointer/WordIds con/ senza grammar ID enumerata vs pattern libero + validazione server.
* **Scaling**: estendi da 10 a 50 documenti e verifica stabilità di **coverage** e **exact match**.
* **Fine-tuning dei puntatori**: aggiungi few-shot per sigle frequenti e pattern numerici (CF, P.IVA, CAP), riducendo `PointerInvalid`.

---

> Per dettagli “a prova di audit”, consulta `eval/out/coverage_matrix.csv` (head-to-head per label) e i file di traccia in `eval/out/traces/…`, che riportano prompt, risposta LLM, puntatori, candidati e motivazioni di scarto caso per caso.

---

[1]: https://github.com/mapo80/docflow-ai-net "GitHub - mapo80/docflow-ai-net"
