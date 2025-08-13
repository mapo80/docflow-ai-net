# Bounding Box Evaluation Results

|Strategy|Expected|WithBBox|TextOnly|Missing|Coverage|Extraction|PointerValidity|
|---|---:|---:|---:|---:|---:|---:|---:|
|PointerWordIds|4|3|0|1|0.75|0.75|0.75| 
|PointerOffsets|4|2|1|1|0.50|0.75|0.50| 
|TokenFirst|4|3|1|0|0.75|1.00|-| 
|Legacy-BitParallel|4|2|1|1|0.50|0.75|-| 
|Legacy-ClassicLevenshtein|4|1|0|3|0.25|0.25|-| 

## Head-to-Head

Label|PointerWordIds|PointerOffsets|TokenFirst|Legacy-BitParallel|Legacy-ClassicLevenshtein|
|---|---|---|---|---|---|
ragione_sociale|WithBBox|WithBBox|WithBBox|WithBBox|WithBBox|
partita_iva|WithBBox|WithBBox|WithBBox|TextOnly|Missing|
indirizzo|WithBBox|TextOnly|WithBBox|WithBBox|Missing|
codice_fiscale|Missing|Missing|TextOnly|Missing|Missing|
