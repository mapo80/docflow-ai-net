# BBox Resolver Integration Tests

This document summarizes the execution of the **TokenFirst** and **PointerStrategy** approaches of the `BBoxResolver` on `sample_invoice.pdf` and `sample_invoice.png`.

## TokenFirst

Results are stored in:
- [`test-pdf-boxsolver2/`](test-pdf-boxsolver2)
- [`test-png-boxsolver2/`](test-png-boxsolver2)

For each file the two available distance algorithms were executed: `BitParallel` and `ClassicLevenshtein`.

## PDF
- File: `dataset/sample_invoice.pdf`
- Output LLM originale: [`test-pdf/llm_response.json`](test-pdf/llm_response.json)
- Output resolver:
  - [`test-pdf-boxsolver2/bitparallel.json`](test-pdf-boxsolver2/bitparallel.json)
  - [`test-pdf-boxsolver2/classiclevenshtein.json`](test-pdf-boxsolver2/classiclevenshtein.json)

| Algorithm | Time (ms) | Fields resolved / total | Avg confidence |
|-----------|-----------:|-----------------------:|---------------:|
| BitParallel | 266 | 6 / 8 | 0.92 |
| ClassicLevenshtein | 276 | 6 / 8 | 0.92 |

Both algorithms correctly anchor "ACME S.p.A.", the lines "Prodotto A"/"Prodotto B", and the monetary values. `invoice_date` and `invoice_number` remain without evidence.

Example record with span:

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
- Original LLM output: [`test-png/llm_response.json`](test-png/llm_response.json)
- Resolver output (parsed from the existing MarkItDownNet JSON):
  - [`test-png-boxsolver2/bitparallel.json`](test-png-boxsolver2/bitparallel.json)
  - [`test-png-boxsolver2/classiclevenshtein.json`](test-png-boxsolver2/classiclevenshtein.json)

| Algorithm | Time (ms) | Fields resolved / total | Avg confidence |
|-----------|-----------:|-----------------------:|---------------:|
| BitParallel | 277 | 2 / 4 | 0.93 |
| ClassicLevenshtein | 279 | 2 / 4 | 0.93 |

Example record:

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

Results are stored in:
- [`test-pdf-boxsolver-pointerstrategy/result.json`](test-pdf-boxsolver-pointerstrategy/result.json)
- [`test-png-boxsolver-pointerstrategy/result.json`](test-png-boxsolver-pointerstrategy/result.json)

### PDF
- File: `dataset/sample_invoice.pdf`
- LLM output with pointers: [`test-pdf/llm_response.json`](test-pdf/llm_response.json)
- Resolver output: [`test-pdf-boxsolver-pointerstrategy/result.json`](test-pdf-boxsolver-pointerstrategy/result.json)

| Strategy | Fields resolved / total | Avg confidence |
|-----------|-----------------------:|---------------:|
| PointerStrategy | 8 / 8 | 1.00 |

Example record:

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
- LLM output with pointers: [`test-png/llm_response.json`](test-png/llm_response.json)
- Resolver output: [`test-png-boxsolver-pointerstrategy/result.json`](test-png-boxsolver-pointerstrategy/result.json)

| Strategy | Fields resolved / total | Avg confidence |
|-----------|-----------------------:|---------------:|
| PointerStrategy | 4 / 4 | 1.00 |

Example record:

```json
{
  "FieldName": "document_type",
  "Value": "invoice",
  "Confidence": 1.0,
  "Spans": [ { "Page": 0, "WordIndices": [4], "BBox": { "X": 0.1661, "Y": 0.2661, "W": 0.0830, "H": 0.0385 }, "Text": "Invoice", "Score": 1.0 } ],
  "Pointer": { "Mode": "WordIds", "WordIds": ["W0_4"] }
}
```

## Comparison with `test-pdf` and `test-png`
The original outputs (`test-pdf` and `test-png`) contained only the fields extracted by the LLM without spatial information. The **TokenFirst** strategy provides bbox evidence for 6 fields in the PDF and 2 fields in the PNG, while **PointerStrategy** reaches 100% of fields (8/8 for the PDF and 4/4 for the PNG) with full confidence thanks to the LLM pointers.

## Notes
- Timing includes TokenFirst indexing and resolution; PNG parsing uses the existing MarkItDownNet JSON because OCR dependencies are missing.
