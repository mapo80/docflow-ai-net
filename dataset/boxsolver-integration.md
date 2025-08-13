# BBox Resolver Integration Tests

Questo documento riassume l'esecuzione delle strategie **TokenFirst** e **PointerStrategy** del `BBoxResolver` sui file `sample_invoice.pdf` e `sample_invoice.png`.

## TokenFirst

Risultati salvati in:
- [`test-pdf-boxsolver2/`](test-pdf-boxsolver2)
- [`test-png-boxsolver2/`](test-png-boxsolver2)

Per ciascun file sono stati eseguiti i due algoritmi di distanza disponibili: `BitParallel` e `ClassicLevenshtein`.

## PDF
- File: `dataset/sample_invoice.pdf`
- Output LLM originale: [`test-pdf/llm_response.json`](test-pdf/llm_response.json)
- Output resolver:
  - [`test-pdf-boxsolver2/bitparallel.json`](test-pdf-boxsolver2/bitparallel.json)
  - [`test-pdf-boxsolver2/classiclevenshtein.json`](test-pdf-boxsolver2/classiclevenshtein.json)

| Algoritmo | Tempo (ms) | Campi risolti / totali | Confidenza media |
|-----------|-----------:|-----------------------:|-----------------:|
| BitParallel | 266 | 6 / 8 | 0.92 |
| ClassicLevenshtein | 276 | 6 / 8 | 0.92 |

Entrambi gli algoritmi ancorano correttamente "ACME S.p.A.", le due righe "Prodotto A"/"Prodotto B" e i valori monetari. `invoice_date` e `invoice_number` restano senza evidenze.

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
- Output resolver (parsing dal JSON MarkItDownNet preesistente):
  - [`test-png-boxsolver2/bitparallel.json`](test-png-boxsolver2/bitparallel.json)
  - [`test-png-boxsolver2/classiclevenshtein.json`](test-png-boxsolver2/classiclevenshtein.json)

| Algoritmo | Tempo (ms) | Campi risolti / totali | Confidenza media |
|-----------|-----------:|-----------------------:|-----------------:|
| BitParallel | 277 | 2 / 4 | 0.93 |
| ClassicLevenshtein | 279 | 2 / 4 | 0.93 |

Esempio di record:

```json
{
  "FieldName": "company_name",
  "Value": "ACME S.p.A.",
  "Confidence": 0.96,
  "Spans": [
    {
      "Page": 0,
      "WordIndices": [0,1],
      "BBox": { "X": 0.0519, "Y": 0.1266, "W": 0.2811, "H": 0.0844 },
      "Text": "ACME S.p.A.",
      "Score": 1.0
    }
  ]
}
```

## PointerStrategy

Risultati salvati in:
- [`test-pdf-boxsolver-pointerstrategy/result.json`](test-pdf-boxsolver-pointerstrategy/result.json)
- [`test-png-boxsolver-pointerstrategy/result.json`](test-png-boxsolver-pointerstrategy/result.json)

### PDF
- File: `dataset/sample_invoice.pdf`
- Output LLM con puntatori: [`test-pdf/llm_response.json`](test-pdf/llm_response.json)
- Output resolver: [`test-pdf-boxsolver-pointerstrategy/result.json`](test-pdf-boxsolver-pointerstrategy/result.json)

| Strategia | Campi risolti / totali | Confidenza media |
|-----------|-----------------------:|-----------------:|
| PointerStrategy | 8 / 8 | 1.00 |

Esempio di record:

```json
{
  "FieldName": "invoice_number",
  "Value": "INV-2025-001",
  "Confidence": 1.0,
  "Spans": [ { "Page": 0, "WordIndices": [7], "BBox": { "X": 0.1975, "Y": 0.1577, "W": 0.1046, "H": 0.0086 }, "Text": "INV-2025-001", "Score": 1.0 } ],
  "Pointer": { "Mode": "WordIds", "WordIds": ["W0_7"] }
}
```

### PNG
- File: `dataset/sample_invoice.png`
- Output LLM con puntatori: [`test-png/llm_response.json`](test-png/llm_response.json)
- Output resolver: [`test-png-boxsolver-pointerstrategy/result.json`](test-png-boxsolver-pointerstrategy/result.json)

| Strategia | Campi risolti / totali | Confidenza media |
|-----------|-----------------------:|-----------------:|
| PointerStrategy | 4 / 4 | 1.00 |

Esempio di record:

```json
{
  "FieldName": "document_type",
  "Value": "invoice",
  "Confidence": 1.0,
  "Spans": [ { "Page": 0, "WordIndices": [4], "BBox": { "X": 0.1661, "Y": 0.2661, "W": 0.0830, "H": 0.0385 }, "Text": "Invoice", "Score": 1.0 } ],
  "Pointer": { "Mode": "WordIds", "WordIds": ["W0_4"] }
}
```

## Confronto con `test-pdf` e `test-png`
Gli output originali (`test-pdf` e `test-png`) contenevano solo i campi estratti dall'LLM senza informazioni spaziali. La strategia **TokenFirst** fornisce evidenze bbox su 6 campi del PDF e su 2 campi del PNG, mentre **PointerStrategy** raggiunge il 100% dei campi (8/8 per il PDF e 4/4 per il PNG) con confidenza piena grazie ai puntatori dell'LLM.

## Note
- Il tempo include l'indicizzazione e la risoluzione TokenFirst; il parsing PNG utilizza il JSON MarkItDownNet già presente a causa di dipendenze OCR mancanti.
