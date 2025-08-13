# BBox Resolver Integration Test

Questo documento riassume l'esecuzione di **BBoxResolver** sui file `sample_invoice.pdf` e `sample_invoice.png`.
I risultati sono stati salvati in:
- [`test-pdf-boxsolver/`](test-pdf-boxsolver)
- [`test-png-boxsolver/`](test-png-boxsolver)

Per ciascun file sono stati eseguiti i due algoritmi di distanza disponibili: `BitParallel` e `ClassicLevenshtein`.

## PDF
- File: `dataset/sample_invoice.pdf`
- Output LLM originale: [`test-pdf/llm_response.json`](test-pdf/llm_response.json)
- Nuovi output resolver:
  - [`test-pdf-boxsolver/bitparallel.json`](test-pdf-boxsolver/bitparallel.json)
  - [`test-pdf-boxsolver/classiclevenshtein.json`](test-pdf-boxsolver/classiclevenshtein.json)

| Algoritmo | Tempo (ms) | Campi risolti / totali | Confidenza media |
|-----------|-----------:|-----------------------:|-----------------:|
| BitParallel | 69.41 | 0 / 8 | 0.8375 |
| ClassicLevenshtein | 1.87 | 0 / 8 | 0.8375 |

Nessun campo ha prodotto evidenze (`Spans` vuoto).

Esempio di record nel JSON:

```json
{
  "FieldName": "company_name",
  "Value": "ACME S.p.A.",
  "Confidence": 0.9,
  "Spans": []
}
```

## PNG
- File: `dataset/sample_invoice.png`
- Output LLM originale: [`test-png/llm_response.json`](test-png/llm_response.json)
- Nuovi output resolver:
  - [`test-png-boxsolver/bitparallel.json`](test-png-boxsolver/bitparallel.json)
  - [`test-png-boxsolver/classiclevenshtein.json`](test-png-boxsolver/classiclevenshtein.json)

| Algoritmo | Tempo (ms) | Campi risolti / totali | Confidenza media |
|-----------|-----------:|-----------------------:|-----------------:|
| BitParallel | 0.86 | 0 / 4 | 0.90 |
| ClassicLevenshtein | 0.38 | 0 / 4 | 0.90 |

Anche in questo caso nessun campo è stato ancorato a bounding box.

## Confronto con `test-pdf` e `test-png`
Gli output originali (`test-pdf` e `test-png`) contenevano solo i campi estratti dall'LLM senza informazioni spaziali. Le esecuzioni con `BBoxResolver` non hanno aggiunto evidenze, pertanto i valori rimangono invariati.

## Note
- La presenza di caratteri non ASCII nei valori (es. simbolo €) ha comportato il fallback automatico all'algoritmo classico per evitare errori nell'implementazione bit-parallel.
- Il motore bit-parallel risulta più lento su questo dataset (≈37× sul PDF, ≈2× sul PNG) a causa del fallback e dell'overhead di setup.
- Ulteriori ottimizzazioni dell'indice e della tokenizzazione sono necessarie per ottenere evidenze utili.
