# MarkItDownNet Integration Test

Questo documento riassume l'estrazione effettuata su `sample_invoice.pdf` e `sample_invoice.png` tramite la libreria **MarkItDownNet**.

## PDF
- File: `dataset/sample_invoice.pdf`
- Output JSON: [`test-pdf-markitdownnet/result.json`](test-pdf-markitdownnet/result.json)
- Confronto: il markdown estratto coincide con `dataset/test-pdf/markitdown.txt`.
- Box: 36 parole individuate.

### Estratto JSON
```json
{
  "Markdown": "ACME S.p.A.\n/ Invoice Fattura\nNumber: INV-2025-001 Invoice 2025-08-09 Date:\nQ.t\u00E0 Descrizione Prezzo Totale\nProdotto 10,00 20,00 A 2 \u20AC \u20AC Prodotto 15,00 15,00 B 1 \u20AC \u20AC TOTALE 35,00 \u20AC\nGrazie per acquisto! il tuo",
  "Pages": [
    {
      "Number": 1,
      "Width": 595.2756,
      "Height": 841.8898
    }
  ],
  "Boxes": [
    {
      "Page": 1,
      "X": 243.1338,
      "Y": 46.39200000000005,
      "Width": 52.99200000000002,
      "Height": 13.607999999999945,
      "XNorm": 0.40843904907239603,
      "YNorm": 0.055104599200513,
      "WidthNorm": 0.08902095096792144,
      "HeightNorm": 0.016163635668231098,
      "Text": "ACME"
    },
    {
      "Page": 1,
      "X": 301.12980000000005,
      "Y": 46.39200000000005,
      "Width": 51.012,
      "Height": 13.607999999999945,
      "XNorm": 0.5058661903830763,
      "YNorm": 0.055104599200513,
      "WidthNorm": 0.0856947605445276,
      "HeightNorm": 0.016163635668231098,
      "Text": "S.p.A."
    },
    {
      "Page": 1,
      "X": 42,
      "Y": 97.94799999999998,
      "Width": 47.446,
      "Height": 10.052000000000021,
      "XNorm": 0.07055555443562611,
      "YNorm": 0.11634954335939434,
      "WidthNorm": 0.07971020322004791,
      "HeightNorm": 0.011938283115944843,
      "Text": "/"
    }
  ]
}
```

## PNG
- File: `dataset/sample_invoice.png`
- Output JSON: [`test-png-markitdownnet/result.json`](test-png-markitdownnet/result.json)
- Confronto: numero di parole/box (`31`) identico a `dataset/test-png/markitdown.txt`.

### Estratto JSON
```json
{
  "Markdown": "ACME S.p.A. Fattura / Invoice Invoice Number: INV-2025-001 Date: 2025-08-09 Descrizione Q.ta Prezzo Totale Prodotto A 2 \u20AC 10,00 \u20AC 20,00 Prodotto B 1 \u20AC 15,00 \u20AC 15,00 TOTALE \u20AC 35,00",
  "Pages": [
    {
      "Number": 1,
      "Width": 1156,
      "Height": 545
    }
  ],
  "Boxes": [
    {
      "Page": 1,
      "X": 60,
      "Y": 69,
      "Width": 149,
      "Height": 37,
      "XNorm": 0.05190311418685121,
      "YNorm": 0.12660550458715597,
      "WidthNorm": 0.12889273356401384,
      "HeightNorm": 0.06788990825688074,
      "Text": "ACME"
    },
    {
      "Page": 1,
      "X": 233,
      "Y": 69,
      "Width": 152,
      "Height": 46,
      "XNorm": 0.20155709342560554,
      "YNorm": 0.12660550458715597,
      "WidthNorm": 0.1314878892733564,
      "HeightNorm": 0.08440366972477065,
      "Text": "S.p.A."
    }
  ]
}
```
