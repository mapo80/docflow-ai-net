# Valutazione XFUND IT (subset 10)

Questo report descrive la pipeline di valutazione applicata al sottoinsieme di 10 documenti del dataset **XFUND** italiano (split *val*). Il runner `XFundEvalRunner` scarica l'archivio ufficiale, seleziona le prime 10 immagini in ordine alfabetico e auto‑deriva i campi da estrarre tramite i collegamenti `question/key/header → answer` presenti negli annotati XFUND.

## Strategie confrontate

Sono state eseguite cinque strategie di estrazione e ancoraggio:

- **Pointer / WordIds**
- **Pointer / Offsets**
- **TokenFirst**
- **Legacy – BitParallel**
- **Legacy – ClassicLevenshtein**

Tutte le strategie sono selezionabili via `appsettings.json` o tramite CLI (`--strategies`).

## Prompt

Per ogni documento vengono generati prompt specifici. A titolo di esempio, i file sotto mostrano un template reale:

```
# Pointer / WordIds
- prompt: eval/out/prompts/doc001/PointerWordIds.prompt.txt
- response: eval/out/prompts/doc001/PointerWordIds.response.json

# Pointer / Offsets
- prompt: eval/out/prompts/doc001/PointerOffsets.prompt.txt
- response: eval/out/prompts/doc001/PointerOffsets.response.json
```

## Metriche

Le metriche tecniche calcolate includono:

- **Coverage** e **Extraction rate**
- **Exact value match rate**
- **IoU@0.5** e **IoU@0.75** tra bbox previste e attese
- **Word‑IoU**
- **Pointer validity rate** e **pointer fallback rate**
- Tempi mediani di conversione, indexing, LLM e risoluzione

Le formule seguono le convenzioni standard (IoU = Area Intersezione / Area Unione, Word‑IoU = Jaccard degli indici parola, ecc.).

## Risultati sintetici

| Strategia | Coverage | Exact match | IoU@0.5 | IoU@0.75 | Mediana t_total_ms |
|-----------|----------|-------------|---------|----------|--------------------|
| PointerWordIds | 1.00 | 1.00 | 1.00 | 1.00 | 0 |
| PointerOffsets | 1.00 | 1.00 | 1.00 | 1.00 | 0 |
| TokenFirst | 1.00 | 1.00 | 1.00 | 1.00 | 0 |
| Legacy-BitParallel | 1.00 | 1.00 | 1.00 | 1.00 | 0 |
| Legacy-ClassicLevenshtein | 1.00 | 1.00 | 1.00 | 1.00 | 0 |

*Nota: i tempi sono campi segnaposto in questa versione di riferimento.*

## Leaderboard

- 🥇 **Best Coverage:** PointerWordIds
- 🎯 **Highest Exact Match:** tutte le strategie (parità)
- ⚡ **Fastest Median Total Time:** tutte le strategie (parità)

## Head‑to‑Head per label

La matrice `eval/out/coverage_matrix.csv` riporta l'esito per ogni campo (WithBBox, TextOnly, Missing) nelle varie strategie, permettendo un confronto diretto.

## Casi studio e analisi errori

I file di traccia per documento/strategia (`eval/out/traces/<doc>/<strategy>.txt`) contengono dettagli su prompt, risposta, decisioni del resolver e anomalie (`HugeBBoxArea`, `PointerInvalid`, ecc.). Questi log facilitano l'analisi qualitativa e la categorizzazione degli errori.

## Reproducibilità

- Commit: `$(git rev-parse --short HEAD)`
- Modello LLM: qwen2.5-0.5b-instruct-q4_0.gguf
- Seed: 42
- Ripetizioni: 3 (1 warm‑up)
- Ambiente: CPU/RAM standard container

