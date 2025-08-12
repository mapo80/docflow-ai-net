import sys
from pathlib import Path

import pytest
from fastapi.testclient import TestClient

# ensure root path on sys.path to import service
ROOT = Path(__file__).resolve().parents[2]
sys.path.append(str(ROOT))

from python.markitdown_service.main import app  # noqa: E402

client = TestClient(app)
DATASET = ROOT / "dataset"


def test_markdown_from_pdf():
    pdf_file = DATASET / "sample_invoice.pdf"
    with pdf_file.open("rb") as f:
        res = client.post("/markdown", files={"file": (pdf_file.name, f, "application/pdf")})
    assert res.status_code == 200
    data = res.json()
    assert "Invoice Number: INV-2025-001" in data["markdown"]


def test_markdown_from_png():
    png_file = DATASET / "sample_invoice.png"
    with png_file.open("rb") as f:
        res = client.post("/markdown", files={"file": (png_file.name, f, "image/png")})
    assert res.status_code == 200
    data = res.json()
    assert data["ocr"]["count"] > 0
    words = " ".join(w["text"] for w in data["ocr"]["words"])
    assert "Invoice" in words


def test_unsupported_extension_returns_400():
    res = client.post(
        "/markdown", files={"file": ("note.txt", b"test", "text/plain")}
    )
    assert res.status_code == 400


def test_empty_file_returns_400():
    res = client.post(
        "/markdown", files={"file": ("empty.pdf", b"", "application/pdf")}
    )
    assert res.status_code == 400
