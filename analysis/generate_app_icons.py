"""Generate PixelBar.App Assets from master logo PNG."""

from __future__ import annotations

import os
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
MASTER = ROOT / "assets" / "pixelbar-logo-master.png"
PROCESSED = ROOT / "assets" / "pixelbar-logo-square.png"
ASSETS = ROOT / "src" / "PixelBar.App" / "Assets"
LOCAL_BADGE = ROOT / "assets" / "edifier-official-wordmark.png"
TEMPOHUB_BADGE = (
    Path(os.environ.get("TEMPOHUB_DIR", r"D:\Program Files (x86)\EDIFIER TempoHub"))
    / "_internal"
    / "resource"
    / "image"
    / "startup_app_logo.png"
)

DARK_THRESHOLD = 36
COLORFUL_PADDING = 2
FILL_RATIO = 0.98
BADGE_WIDTH_RATIO = 0.30
BADGE_MARGIN_RATIO = 0.05


def key_dark_to_transparent(img: Image.Image, threshold: int = DARK_THRESHOLD) -> Image.Image:
    result = img.convert("RGBA")
    pixels = result.load()
    width, height = result.size
    for y in range(height):
        for x in range(width):
            red, green, blue, alpha = pixels[x, y]
            if alpha == 0:
                continue
            if red + green + blue <= threshold:
                pixels[x, y] = (0, 0, 0, 0)
    return result


def is_colorful_pixel(red: int, green: int, blue: int, alpha: int) -> bool:
    if alpha < 48:
        return False
    peak = max(red, green, blue)
    if peak < 96:
        return False
    return peak - min(red, green, blue) >= 36


def colorful_bbox(img: Image.Image) -> tuple[int, int, int, int]:
    pixels = img.load()
    width, height = img.size
    min_x, min_y = width, height
    max_x = max_y = 0
    for y in range(height):
        for x in range(width):
            red, green, blue, alpha = pixels[x, y]
            if is_colorful_pixel(red, green, blue, alpha):
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)
    if max_x <= min_x or max_y <= min_y:
        return 0, 0, width - 1, height - 1
    return min_x, min_y, max_x, max_y


def content_bbox(img: Image.Image) -> tuple[int, int, int, int]:
    pixels = img.load()
    width, height = img.size
    min_x, min_y = width, height
    max_x = max_y = 0
    for y in range(height):
        for x in range(width):
            _, _, _, alpha = pixels[x, y]
            if alpha > 16:
                min_x = min(min_x, x)
                min_y = min(min_y, y)
                max_x = max(max_x, x)
                max_y = max(max_y, y)
    if max_x <= min_x or max_y <= min_y:
        return 0, 0, width - 1, height - 1
    return min_x, min_y, max_x, max_y


def keep_colorful_content(img: Image.Image) -> Image.Image:
    result = img.copy()
    pixels = result.load()
    width, height = result.size
    for y in range(height):
        for x in range(width):
            red, green, blue, alpha = pixels[x, y]
            if is_colorful_pixel(red, green, blue, alpha):
                continue
            pixels[x, y] = (0, 0, 0, 0)
    return result


def crop_colorful_fill_square(img: Image.Image, padding: int = COLORFUL_PADDING) -> Image.Image:
    color_left, color_top, color_right, color_bottom = colorful_bbox(img)

    left = max(0, color_left - padding)
    top = max(0, color_top - padding)
    right = min(img.width - 1, color_right + padding)
    bottom = min(img.height - 1, color_bottom + padding)

    crop = keep_colorful_content(img.crop((left, top, right + 1, bottom + 1)))
    tight_left, tight_top, tight_right, tight_bottom = content_bbox(crop)
    crop = crop.crop((tight_left, tight_top, tight_right + 1, tight_bottom + 1))

    color_height = tight_bottom - tight_top + 1
    color_width = tight_right - tight_left + 1
    target = max(256, max(color_width, color_height))
    scale = max(
        (target * FILL_RATIO) / color_width,
        (target * FILL_RATIO) / color_height,
    )
    resized = crop.resize(
        (max(1, int(crop.width * scale)), max(1, int(crop.height * scale))),
        Image.Resampling.LANCZOS,
    )

    left = max(0, (resized.width - target) // 2)
    top = max(0, (resized.height - target) // 2)
    return resized.crop((left, top, left + target, top + target))


def resolve_badge_source() -> Path:
    if LOCAL_BADGE.is_file():
        return LOCAL_BADGE
    if TEMPOHUB_BADGE.is_file():
        LOCAL_BADGE.parent.mkdir(parents=True, exist_ok=True)
        LOCAL_BADGE.write_bytes(TEMPOHUB_BADGE.read_bytes())
        return LOCAL_BADGE
    raise SystemExit(
        "EDIFIER badge not found. Copy startup_app_logo.png to assets/edifier-official-wordmark.png"
    )


def prepare_edifier_badge(source: Path) -> Image.Image:
    return key_dark_to_transparent(Image.open(source).convert("RGBA"), threshold=40)


def overlay_edifier_badge(base: Image.Image, badge: Image.Image) -> Image.Image:
    result = base.copy()
    side = base.width
    badge_width = max(1, int(side * BADGE_WIDTH_RATIO))
    badge_height = max(1, int(badge.height * badge_width / badge.width))
    scaled = badge.resize((badge_width, badge_height), Image.Resampling.LANCZOS)

    margin = max(2, int(side * BADGE_MARGIN_RATIO))
    pad_x = max(2, int(badge_width * 0.10))
    pad_y = max(2, int(badge_height * 0.18))
    backing_w = badge_width + pad_x * 2
    backing_h = badge_height + pad_y * 2
    x = side - backing_w - margin
    y = side - backing_h - margin

    backing = Image.new("RGBA", (backing_w, backing_h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(backing)
    radius = max(3, int(min(backing_w, backing_h) * 0.22))
    draw.rounded_rectangle(
        (0, 0, backing_w - 1, backing_h - 1),
        radius=radius,
        fill=(8, 10, 16, 168),
    )
    result.alpha_composite(backing, (x, y))
    result.alpha_composite(scaled, (x + pad_x, y + pad_y))
    return result


def apply_rounded_mask(img: Image.Image, radius_ratio: float = 0.18) -> Image.Image:
    width, height = img.size
    radius = max(4, int(min(width, height) * radius_ratio))
    mask = Image.new("L", (width, height), 0)
    draw = ImageDraw.Draw(mask)
    draw.rounded_rectangle((0, 0, width - 1, height - 1), radius=radius, fill=255)

    result = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    result.paste(img, (0, 0), mask)
    return result


def prepare_logo(source: Path) -> Image.Image:
    raw = Image.open(source).convert("RGBA")
    keyed = key_dark_to_transparent(raw)
    filled = crop_colorful_fill_square(keyed)
    badged = overlay_edifier_badge(filled, prepare_edifier_badge(resolve_badge_source()))
    return apply_rounded_mask(badged, radius_ratio=0.14)


def save_square(img: Image.Image, path: Path, size: int) -> None:
    resized = img.resize((size, size), Image.Resampling.LANCZOS)
    resized.save(path, "PNG")


def main() -> int:
    if not MASTER.is_file():
        raise SystemExit(f"Master logo not found: {MASTER}")

    ASSETS.mkdir(parents=True, exist_ok=True)
    img = prepare_logo(MASTER)
    img.save(PROCESSED, "PNG")

    save_square(img, ASSETS / "StoreLogo.png", 50)
    save_square(img, ASSETS / "Square44x44Logo.targetsize-24_altform-unplated.png", 24)
    save_square(img, ASSETS / "Square44x44Logo.targetsize-48_altform-lightunplated.png", 48)
    save_square(img, ASSETS / "Square44x44Logo.scale-200.png", 88)
    save_square(img, ASSETS / "Square150x150Logo.scale-200.png", 300)
    save_square(img, ASSETS / "AppLogo.png", 128)

    wide = Image.new("RGBA", (620, 300), (0, 0, 0, 0))
    logo = img.resize((200, 200), Image.Resampling.LANCZOS)
    wide.paste(logo, ((620 - 200) // 2, (300 - 200) // 2), logo)
    wide.save(ASSETS / "Wide310x150Logo.scale-200.png", "PNG")
    wide.save(ASSETS / "SplashScreen.scale-200.png", "PNG")

    ico = img.resize((256, 256), Image.Resampling.LANCZOS)
    ico.save(
        ASSETS / "AppIcon.ico",
        format="ICO",
        sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
    )

    print(f"Processed square logo: {PROCESSED}")
    print(f"Generated assets in {ASSETS}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
