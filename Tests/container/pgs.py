# pgs.py <file> <line>...: writes a PGS subtitle that shows the lines from second 1 to 3, for the OCR of the ui suite
import struct
import sys
from PIL import Image, ImageDraw, ImageFont

FONT = ImageFont.truetype('/usr/share/fonts/TTF/DejaVuSans.ttf', 48)
W, H = 1920, 1080


# the palette indexes of the text: 0 transparent, 1 white, 2 the black outline
def render(lines):
    draw = ImageDraw.Draw(Image.new('RGBA', (10, 10)))
    w = max(draw.textbbox((0, 0), line, font=FONT, stroke_width=3)[2] for line in lines) + 20
    h = 64 * len(lines) + 20
    image = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)

    for i, line in enumerate(lines):
        draw.text((10, 10 + 64 * i), line, font=FONT, fill=(255, 255, 255, 255), stroke_width=3,
                  stroke_fill=(0, 0, 0, 255))

    pixels = image.load()
    indexes = [[0] * w for _ in range(h)]

    for y in range(h):
        for x in range(w):
            r, g, b, a = pixels[x, y]
            if a >= 128:
                indexes[y][x] = 1 if (r + g + b) / 3 > 128 else 2

    return w, h, indexes


def rle(w, h, indexes):
    out = bytearray()

    for row in indexes:
        x = 0
        while x < w:
            color = row[x]
            n = 1
            while x + n < w and row[x + n] == color and n < 16383:
                n += 1
            if color != 0 and n < 3:
                out += bytes([color]) * n
            elif color == 0:
                out += bytes([0, n]) if n < 64 else bytes([0, 0x40 | (n >> 8), n & 0xFF])
            else:
                out += bytes([0, 0x80 | n, color]) if n < 64 else bytes([0, 0xC0 | (n >> 8), n & 0xFF, color])
            x += n
        out += b'\x00\x00'

    return bytes(out)


def segment(pts, kind, data):
    return b'PG' + struct.pack('>IIBH', pts * 90000, 0, kind, len(data)) + data


def composition(pts, number, state, position=None):
    data = struct.pack('>HHBHBBBB', W, H, 0x10, number, state, 0, 0, 1 if position else 0)
    if position:
        data += struct.pack('>HBBHH', 0, 0, 0, *position)
    return segment(pts, 0x16, data)


def window(pts, x, y, w, h):
    return segment(pts, 0x17, struct.pack('>BBHHHH', 1, 0, x, y, w, h))


def palette(pts):
    return segment(pts, 0x14, bytes([0, 0, 0, 16, 128, 128, 0, 1, 235, 128, 128, 255, 2, 16, 128, 128, 255]))


def image(pts, w, h, indexes):
    data = rle(w, h, indexes)
    first = struct.pack('>HBB', 0, 0, 0xC0) + struct.pack('>I', len(data) + 4)[1:] + struct.pack('>HH', w, h)
    return segment(pts, 0x15, first + data)


def end(pts):
    return segment(pts, 0x80, b'')


w, h, indexes = render(sys.argv[2:])
x, y = (W - w) // 2, H - h - 50
out = composition(1, 1, 0x80, (x, y)) + window(1, x, y, w, h) + palette(1) + image(1, w, h, indexes) + end(1)
out += composition(3, 2, 0x00) + window(3, x, y, w, h) + end(3)

with open(sys.argv[1], 'wb') as file:
    file.write(out)
