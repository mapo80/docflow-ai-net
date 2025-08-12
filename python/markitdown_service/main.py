import io
import logging
import os
import pathlib
from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.responses import JSONResponse
from PIL import Image
import pytesseract
from markitdown import MarkItDown

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="MarkItDown OCR Service", version="1.0.0")

@app.get("/health")
def health():
    logger.debug("Health check requested")
    return {"status": "ok"}

@app.post("/markdown")
async def to_markdown(file: UploadFile = File(...)):
    try:
        logger.info("Processing file: %s", file.filename)
        content = await file.read()
        if not content:
            logger.error("Empty file uploaded")
            raise HTTPException(status_code=400, detail="Empty file")

        ext = pathlib.Path(file.filename or "").suffix.lower()
        logger.info("Detected extension: %s", ext)

        words: list[dict] = []
        md_result = None

        if ext == ".pdf":
            import tempfile
            try:
                with tempfile.NamedTemporaryFile(suffix=".pdf") as tmp:
                    tmp.write(content)
                    tmp.flush()
                    md_result = MarkItDown().convert(tmp.name)
            except Exception as e:
                logger.exception("Failed to convert PDF")
                raise HTTPException(status_code=500, detail="PDF conversion failed") from e
        elif ext in {".png", ".jpg", ".jpeg", ".bmp", ".tiff", ".gif"}:
            try:
                image = Image.open(io.BytesIO(content)).convert("RGB")
                data = pytesseract.image_to_data(
                    image,
                    output_type=pytesseract.Output.DICT,
                    lang=os.getenv("OCR_LANG", "eng+ita"),
                )
                n = len(data["text"])
                for i in range(n):
                    txt = data["text"][i]
                    if txt and txt.strip():
                        words.append(
                            {
                                "text": txt.strip(),
                                "left": int(data["left"][i]),
                                "top": int(data["top"][i]),
                                "width": int(data["width"][i]),
                                "height": int(data["height"][i]),
                            }
                        )
                md_result = MarkItDown().convert(io.BytesIO(content))
            except Exception as e:
                logger.exception("Failed to OCR image")
                raise HTTPException(status_code=500, detail="Image OCR failed") from e
        else:
            logger.error("Unsupported file extension: %s", ext)
            raise HTTPException(status_code=400, detail=f"Unsupported file extension: {ext}")

        markdown = md_result.text_content if md_result else ""
        logger.info(
            "Generated markdown with %d characters and %d OCR words",
            len(markdown),
            len(words),
        )
        return JSONResponse({"markdown": markdown, "ocr": {"words": words, "count": len(words)}})

    except HTTPException:
        raise
    except Exception as e:
        logger.exception("Unexpected error processing file: %s", file.filename)
        raise HTTPException(status_code=500, detail="Unexpected error") from e
