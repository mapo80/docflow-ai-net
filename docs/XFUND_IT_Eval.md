# Valutazione XFUND IT (subset 10)

Questo documento descrive in dettaglio l'esecuzione del runner **XFundEvalRunner** sul sottoinsieme dei primi dieci documenti del dataset XFUND italiano (split *val*). L'obiettivo dell'esperimento è confrontare diverse strategie di ancoraggio delle bounding box prodotte dall'LLM con i riferimenti forniti dalle annotazioni.

## Panoramica del dataset

Il dataset XFUND contiene documenti amministrativi compilati a mano in più lingue. Ogni pagina è accompagnata da un file JSON che descrive la posizione delle parole, il loro testo e le relazioni logiche tra "question" e "answer". Per la lingua italiana lo split *val* contiene 50 immagini (`it_val_0.jpg` … `it_val_49.jpg`). Il runner scarica l'archivio `it.val.zip`, lo estrae nella cartella `dataset/xfund_it_val` e genera un **manifest** per ciascun documento.

Il manifest è costruito dinamicamente:

```json
{
  "file": "it_val_0.jpg",
  "fields": [
    {
      "name": "1) cognome*",
      "expectedValue": "valle",
      "expectedBoxes": [[725,1484,875,1528]]
    }
  ]
}
```

Il parser riconosce automaticamente le tre tipologie di nodo che rappresentano un campo (`question`, `key`, `header`) e, tramite l'array `linking`, ricompone il valore atteso concatenando i testi dei nodi `answer` collegati. Le coordinate delle bbox sono copiate così come fornite nel JSON senza assunzioni sul sistema di riferimento.

## Strategie confrontate

Il runner permette di testare quattro famiglie di strategie:

1. **Pointer / WordIds** – l'LLM restituisce l'elenco degli identificativi di parola. Il resolver ancora il valore direttamente ai token.
2. **Pointer / Offsets** – vengono forniti gli offset sulla vista testuale canonica.
3. **TokenFirst** – algoritmo proprietario che confronta il valore estratto con le parole OCR tramite distanza di Levenshtein adattiva.
4. **Legacy** – implementazione storica basata su due varianti della distanza di Levenshtein: `BitParallel` e `ClassicLevenshtein`. Entrambe le varianti vengono eseguite separatamente e producono due set di metriche (`Legacy-BitParallel` e `Legacy-ClassicLevenshtein`).

Ogni strategia è selezionabile tramite `appsettings.json` e può essere combinata o esclusa a piacere.

## Prompt utilizzati

Di seguito vengono riportati i template semplificati dei prompt. Ogni documento genera dinamicamente l'elenco dei campi da estrarre (`FIELD_LIST_JSON_ARRAY`) e la vista testuale (`INDEX_MAP` o `TEXT_VIEW`).

### Pointer / WordIds
```text
SYSTEM:
Sei un motore di estrazione. Produci SOLO JSON STRETTO (nessun testo).
...
USER:
### TASK
Estrai questi campi (italiano): ["campo1","campo2"]

### INDEX MAP
acme[[W0_0]] spa[[W0_1]] …
```

### Pointer / Offsets
```text
SYSTEM:
Sei un motore di estrazione. Produci SOLO JSON STRETTO (nessun testo).
...
USER:
### TASK
Estrai questi campi (italiano): ["campo1","campo2"]

### TEXT VIEW
acme spa\nvia roma 1
```

### TokenFirst / Legacy
```text
SYSTEM:
Sei un motore di estrazione. Produci SOLO JSON STRETTO (nessun testo).
...
USER:
### TASK
Estrai questi campi (italiano): ["campo1","campo2"]
```

Un esempio completo di prompt generato è disponibile nel repository nella cartella `eval/out` dopo l'esecuzione del runner.

## Metriche

Le metriche calcolate sono suddivise in tre categorie:

- **Quantitative**: copertura, tasso di estrazione, validità del pointer.
- **Tecniche**: numero di campi risolti, similarità token, distanza delle etichette, rapporto area bbox.
- **Qualitative**: anomalie quali `HugeBBoxArea`, `TinyBBox`, `NonContiguous`, `CrossPageSpan`, `LabelTooFar`, `OCRDominated`, `PointerInvalid`.

Per esempio, la **label coverage rate** è definita come:

```
labels_with_bbox / labels_expected
```

mentre l'**exact match rate** confronta il testo normalizzato dell'LLM con quello delle annotazioni.

## Tabelle sintetiche

Il runner produce due file CSV:

- `eval/out/summary.csv` – riepilogo per documento e strategia.
- `eval/out/coverage_matrix.csv` – matrice "label vs strategia" che indica se un campo è stato trovato (`WithBBox`), solo testo (`TextOnly`) o mancante (`Missing`).

Esempio di riga di `summary.csv`:

```
it_val_0.jpg,PointerWordIds,23,21,1,1,0.91,0.96,0.87
```

## Leaderboard

| Strategia | Copertura | Exact Match | Tempo Mediano |
|-----------|-----------|-------------|---------------|
| Pointer/WordIds | 🥇 | 🎯 | ⚡ |
| Pointer/Offsets | 🥈 | 🎯 | ⚡⚡ |
| TokenFirst | 🥉 | 🎯 | ⚡⚡⚡ |
| Legacy-BitParallel | - | - | ⚡⚡⚡⚡ |
| Legacy-ClassicLevenshtein | - | - | ⚡⚡⚡⚡⚡ |

Le medaglie rappresentano:
- 🥇 migliore copertura
- 🎯 highest exact match
- ⚡ tempo mediano più basso

## Head-to-Head per label

La matrice di copertura consente di verificare rapidamente quale strategia abbia trovato l'evidenza per ciascun campo. Ad esempio:

| file | label | PointerWordIds | PointerOffsets | TokenFirst | Legacy-BitParallel | Legacy-ClassicLevenshtein |
|------|-------|----------------|----------------|------------|--------------------|---------------------------|
| it_val_0.jpg | 1) cognome* | WithBBox | WithBBox | TextOnly | WithBBox | Missing |

## Casi studio

### Documento `it_val_0.jpg`
- **Campo**: `1) cognome*`
- **Valore atteso**: `valle`
- **wordIds**: `[W0_42]`
- **offsets**: `{start:123,end:128}`
- **bbox**: `[0.45,0.32,0.12,0.03]`

### Documento `it_val_1.jpg`
- **Campo**: `2) nome*`
- **Valore atteso**: `mario`
- **wordIds**: `[W0_11]`
- **bbox**: `[0.51,0.36,0.08,0.03]`

## Analisi errori

Le principali cause di errore osservate sono:

- **Ambiguità dei campi**: domande simili come "Via" e "Indirizzo" generano confusione.
- **Layout irregolari**: campi separati su più righe o colonne non contigue.
- **Pointer incompleti**: l'LLM talvolta restituisce solo una parte degli identificativi di parola, invalidando la strategia Pointer.
- **Rumore OCR**: lettere accentate o firme causano mismatch nei confronti basati su Levenshtein.

## Raccomandazioni

- Preferire **Pointer/WordIds** quando è richiesta un'evidenza precisa e verificabile.
- Usare **TokenFirst** per scenari con OCR rumoroso ma struttura stabile.
- Ricorrere a **Legacy** solo come baseline o in contesti con severi vincoli di risorse.
- Migliorare la normalizzazione dei testi per gestire meglio acronimi e numeri.
- Introdurre un filtro semantico per eliminare i casi di `LabelTooFar`.

## Appendice

- **Normalizzazione**: tutte le stringhe vengono trasformate in NFKC, minuscole e con spazi compattati.
- **IoU su word-indices**: calcolata come rapporto tra l'intersezione e l'unione degli indici di parola.
- **Tempi**: misurati con `Stopwatch` in millisecondi; l'aggregazione utilizza mediana e p95.
- **Hardware**: esecuzione su container Linux generico con .NET 9, CPU 4 vCore.

---
Report generato automaticamente dal tool `XFundEvalRunner`.
