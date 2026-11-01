"""Bereitet die Bilder der Serie Barista Life für die Plattform vor (src/StoryPlatform.Api/SeedMedia).

Quelle sind ausschließlich die Szenenbilder der drei Folgenordner:
    Part-01-19-Uhr-58/bilder, Part-02-Der-Anruf/bilder, Part-03-Die-Falle/bilder

    python tools/prepare_series_media.py "X:\\Packages\\BaristaLife-Storyclips\\Folgen"

Die Originale (1024×1536, 2:3) bleiben unverändert; hier entstehen web-optimierte JPEGs.
"""
import sys
from pathlib import Path

from PIL import Image

OUT = Path(__file__).resolve().parent.parent / "src" / "StoryPlatform.Api" / "SeedMedia"

# Ziel-Datei -> (Folge, Bild, Art)
PLAN = {
    "barista-life-cover.jpg": ("Part-01-19-Uhr-58", "P03.png", "cover"),    # Der Beobachter vor der Scheibe
    "barista-life-hero.jpg": ("Part-01-19-Uhr-58", "P01.png", "hero"),      # Türglocke um 19:58
    "barista-life-social.jpg": ("Part-01-19-Uhr-58", "P01.png", "social"),  # Vorschaubild für Messenger/Social Media
    "barista-life-part-1.jpg": ("Part-01-19-Uhr-58", "P02.png", "thumb"),   # Die Bitte
    "barista-life-part-2.jpg": ("Part-02-Der-Anruf", "B03.png", "thumb"),   # Der Anruf
    "barista-life-part-3.jpg": ("Part-03-Die-Falle", "F01.png", "thumb"),   # FBI! OPEN UP!
}


def crop_to(img, size, focus=(0.5, 0.5)):
    tw, th = size
    w, h = img.size
    scale = max(tw / w, th / h)
    img = img.resize((round(w * scale), round(h * scale)), Image.LANCZOS)
    w, h = img.size
    x = round((w - tw) * focus[0]); y = round((h - th) * focus[1])
    return img.crop((x, y, x + tw, y + th))


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    root = Path(sys.argv[1])
    OUT.mkdir(parents=True, exist_ok=True)
    for name, (episode, file, kind) in PLAN.items():
        img = Image.open(root / episode / "bilder" / file).convert("RGB")
        if kind == "cover":
            img = img.resize((800, 1200), Image.LANCZOS)
        elif kind == "hero":
            img = img.resize((1024, 1536), Image.LANCZOS)
        elif kind == "social":
            img = crop_to(img, (1200, 630), (0.5, 0.18))
        else:
            img = img.resize((600, 900), Image.LANCZOS)
        img.save(OUT / name, "JPEG", quality=82, optimize=True, progressive=True)
        print(f"  {name:28} <- {episode}/bilder/{file}")


if __name__ == "__main__":
    main()
