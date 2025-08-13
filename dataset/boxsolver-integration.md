# BBox Resolver Integration Test

Questo documento riassume l'esecuzione di **BBoxResolver** sui file `sample_invoice.pdf` e `sample_invoice.png`.
I risultati sono salvati in:
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
| BitParallel | 44.79 | 6 / 8 | 0.92 |
| ClassicLevenshtein | 0.75 | 6 / 8 | 0.92 |

Entrambi gli algoritmi ancorano correttamente "ACME S.p.A.", le due righe "Prodotto A"/"Prodotto B" e i valori monetari.
`invoice_date` e `invoice_number` restano senza evidenze.

Esempio di record con span:

```json
{
  "FieldName": "company_name",
  "Value": "ACME S.p.A.",
  "Confidence": 0.96,
  "Spans": [
    {
      "Page": 0,
      "WordIndices": [0,1],
      "BBox": { "X": 0.4084, "Y": 0.0551, "W": 0.1831, "H": 0.0162 },
      "Text": "ACME S.p.A.",
      "Score": 1.0
    }
  ]
}
```

## PNG
- File: `dataset/sample_invoice.png`
- Output LLM originale: [`test-png/llm_response.json`](test-png/llm_response.json)
- Nuovi output resolver: **non generati**. L'esecuzione di MarkItDownNet su PNG fallisce per la mancanza della libreria native `libleptonica-1.82.0.so` richiesta da Tesseract.
- Per riferimento rimangono i precedenti JSON:
  - [`test-png-boxsolver/bitparallel.json`](test-png-boxsolver/bitparallel.json)
  - [`test-png-boxsolver/classiclevenshtein.json`](test-png-boxsolver/classiclevenshtein.json)

## Confronto con `test-pdf` e `test-png`
Gli output originali (`test-pdf` e `test-png`) contenevano solo i campi estratti dall'LLM senza informazioni spaziali. Il resolver ora fornisce evidenze bbox su 6 campi del PDF, mentre il PNG resta non elaborato a causa di dipendenze mancanti.

## Note
- Il motore `BitParallel` è ~60× più lento di `ClassicLevenshtein` su questo PDF (44.79ms vs 0.75ms) pur producendo le stesse evidenze.
- Sono necessari ulteriori setup di librerie native per elaborare i PNG.
