import io, os
from fastapi import FastAPI, UploadFile, File
from fastapi.responses import JSONResponse
from PIL import Image
import pytesseract
from markitdown import MarkItDown

app = FastAPI(title="MarkItDown OCR Service", version="1.0.0")

@app.get("/health")
def health():
    return {"status": "ok"}

@app.post("/markdown")
async def to_markdown(file: UploadFile = File(...)):
    content = await file.read()
    image = Image.open(io.BytesIO(content)).convert("RGB")

    data = pytesseract.image_to_data(image, output_type=pytesseract.Output.DICT, lang=os.getenv("OCR_LANG", "eng+ita"))
    words = []
    n = len(data["text"])
    for i in range(n):
        txt = data["text"][i]
        if txt and txt.strip():
            words.append({
                "text": txt.strip(),
                "left": int(data["left"][i]),
                "top": int(data["top"][i]),
                "width": int(data["width"][i]),
                "height": int(data["height"][i])
            })

    md = MarkItDown().convert(image).text_content

    return JSONResponse({ "markdown": md, "ocr": { "words": words, "count": len(words) } })
