# Pay slip dataset analysis

All pay slip conversions failed due to a Leptonica library load error (`libleptonica-1.82.0.so`), so no content or timing metrics were generated.

| File | Error | Details |
| --- | --- | --- |
| busta_paga_01.jpg | Leptonica library load failure | [link](./busta-paga/busta_paga_01.md) |
| busta_paga_02.jpg | Leptonica library load failure | [link](./busta-paga/busta_paga_02.md) |
| busta_paga_03.jpg | Leptonica library load failure | [link](./busta-paga/busta_paga_03.md) |
| busta_paga_05.png | Leptonica library load failure | [link](./busta-paga/busta_paga_05.md) |

The native Tesseract and Leptonica libraries ship with the project under `src/MarkItDownNet/TesseractOCR/x64` and are copied next to the binaries during build; verify they are available when running the converter.

No timing information or extracted fields are available.
