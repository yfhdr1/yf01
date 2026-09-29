import json, os, math, random, sys, subprocess, glob
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter, ImageChops
HERE = os.path.dirname(os.path.abspath(__file__))
CACHE = '/home/user/gen'
FB = os.path.join(CACHE, 'qta/qtawesome/fonts/')
if not os.path.isdir(FB):
    os.makedirs(CACHE + '/pkgs', exist_ok=True)
    subprocess.run(['pip', 'download', '--no-deps', '-q', 'qtawesome', '-d', CACHE + '/pkgs'], check=True)
    whl = glob.glob(CACHE + '/pkgs/qtawesome-*.whl')[0]
    subprocess.run([sys.executable, '-m', 'zipfile', '-e', whl, CACHE + '/qta'], check=True)
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(CACHE, 'out/scary')
os.makedirs(OUT, exist_ok=True)
FONTS = {'mdi': ('materialdesignicons6-webfont-6.9.96.ttf', 'materialdesignicons6-webfont-charmap-6.9.96.json'),
         'fa': ('fontawesome6-solid-webfont-6.7.2.ttf', 'fontawesome6-solid-webfont-charmap-6.7.2.json')}
MAPS = {}
S = 256
K = 4
W = S * K

def glyph_mask(key, px, rot=0, flip=False):
    fam, name = key.split(':')
    ttf, js = FONTS[fam]
    if fam not in MAPS:
        MAPS[fam] = json.load(open(FB + js))
    ch = chr(int(MAPS[fam][name], 16))
    font = ImageFont.truetype(FB + ttf, int(px))
    im = Image.new('L', (px * 2, px * 2), 0)
    ImageDraw.Draw(im).text((px // 2, px // 2), ch, font=font, fill=255)
    im = im.crop(im.getbbox())
    if flip:
        im = im.transpose(Image.FLIP_LEFT_RIGHT)
    if rot:
        im = im.rotate(rot, resample=Image.BICUBIC, expand=True)
        im = im.crop(im.getbbox())
    s = px / max(im.size)
    return im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.LANCZOS)

def noise(seed, scale):
    rng = np.random.default_rng(seed)
    n = rng.random((W // scale + 2, W // scale + 2)).astype(np.float32)
    im = Image.fromarray((n * 255).astype(np.uint8), 'L').resize((W, W), Image.BICUBIC)
    return np.asarray(im).astype(np.float32) / 255.0

def make(name, key, size=0.44, rot=0, flip=False, cy=0.47, extra=None, seed=1):
    random.seed(seed)
    yy, xx = np.mgrid[0:W, 0:W].astype(np.float32)
    cx, ccy = W * 0.5, W * 0.46
    R = W * 0.38
    dx, dy = xx - cx, yy - ccy
    dist = np.sqrt(dx * dx + dy * dy)
    ang = np.arctan2(dy, dx)
    n1 = noise(seed, 64)
    n2 = noise(seed + 7, 8)
    n3 = noise(seed + 3, 3)
    wob = (np.sin(ang * 7 + seed) * 0.006 + np.sin(ang * 13 + seed * 2) * 0.004) * W + (n1 - 0.5) * W * 0.012
    edge = R + wob
    disk = np.clip((edge - dist) / 3.0 + 0.5, 0, 1)
    t = np.clip(dist / R, 0, 1)
    base_r = 46 * (1 - t) + 14 * t
    base_g = 4 * (1 - t) + 1 * t
    base_b = 6 * (1 - t) + 2 * t
    grit = (n2 - 0.5) * 26 + (n3 - 0.5) * 18
    r = np.clip(base_r + grit, 0, 255)
    g = np.clip(base_g + grit * 0.15, 0, 255)
    b = np.clip(base_b + grit * 0.2, 0, 255)
    a = disk * 215
    ring_w = W * 0.028
    ring = np.clip(1 - np.abs(dist - (edge - ring_w * 0.6)) / ring_w, 0, 1)
    gaps = (noise(seed + 11, 40) > 0.78).astype(np.float32)
    ring = ring * (1 - gaps * 0.85) * (0.75 + n3 * 0.5)
    rr = np.clip(r * (1 - ring) + 150 * ring, 0, 255)
    rg = np.clip(g * (1 - ring) + 8 * ring, 0, 255)
    rb = np.clip(b * (1 - ring) + 10 * ring, 0, 255)
    ra = np.clip(np.maximum(a, ring * 255), 0, 255)
    img = Image.fromarray(np.dstack([rr, rg, rb, ra]).astype(np.uint8), 'RGBA')
    glow_m = Image.fromarray((np.clip(ring, 0, 1) * 255).astype(np.uint8), 'L').filter(ImageFilter.GaussianBlur(W * 0.025))
    glow = Image.new('RGBA', (W, W), (170, 0, 0, 0))
    glow.putalpha(glow_m.point(lambda v: int(v * 0.8)))
    out = Image.new('RGBA', (W, W), (0, 0, 0, 0))
    out.alpha_composite(glow)
    out.alpha_composite(img)
    d = ImageDraw.Draw(out)
    for i in range(3):
        a0 = math.radians(random.uniform(60, 120))
        x = cx + (R - ring_w) * math.cos(a0)
        y0 = ccy + (R - ring_w) * math.sin(a0)
        L = random.uniform(0.06, 0.12) * W
        w0 = random.uniform(0.012, 0.02) * W
        col = (120, 4, 6, 245)
        d.polygon([(x - w0, y0), (x + w0, y0), (x + w0 * 0.55, y0 + L), (x - w0 * 0.55, y0 + L)], fill=col)
        rd = w0 * 1.25
        d.ellipse([x - rd, y0 + L - rd * 0.6, x + rd, y0 + L + rd * 1.4], fill=col)
        d.ellipse([x - rd * 0.35, y0 + L - rd * 0.1, x + rd * 0.05, y0 + L + rd * 0.5], fill=(200, 60, 60, 160))
    for i in range(10):
        a0 = random.uniform(0, 2 * math.pi)
        r0 = random.uniform(0.1, 0.85) * R
        x0, y0 = cx + r0 * math.cos(a0), ccy + r0 * math.sin(a0)
        l = random.uniform(0.05, 0.16) * W
        th = random.uniform(0, math.pi)
        d.line([(x0, y0), (x0 + l * math.cos(th), y0 + l * math.sin(th))], fill=(0, 0, 0, 90), width=int(W * 0.004))
    gm = glyph_mask(key, int(W * size), rot, flip)
    if extra:
        gm = extra(gm)
    gx = int(cx - gm.width / 2)
    gy = int(W * cy - gm.height / 2)
    full = Image.new('L', (W, W), 0)
    full.paste(gm, (gx, gy))
    cuts = Image.new('L', (W, W), 255)
    cd = ImageDraw.Draw(cuts)
    for i in range(3):
        x0 = random.uniform(0.3, 0.7) * W
        y0 = random.uniform(0.25, 0.65) * W
        l = random.uniform(0.12, 0.22) * W
        th = math.radians(random.uniform(20, 70))
        cd.line([(x0, y0), (x0 + l * math.cos(th), y0 + l * math.sin(th))], fill=0, width=int(W * 0.007))
    full = ImageChops.multiply(full, cuts)
    gn = noise(seed + 21, 6)
    fa = np.asarray(full).astype(np.float32) / 255.0
    fa = fa * np.clip(0.93 + (gn - 0.5) * 0.25, 0, 1)
    fm = Image.fromarray((fa * 255).astype(np.uint8), 'L')
    sh = Image.new('RGBA', (W, W), (90, 0, 0, 0))
    sh.putalpha(fm.filter(ImageFilter.GaussianBlur(W * 0.02)).point(lambda v: min(255, int(v * 1.3))))
    out.alpha_composite(sh)
    shade = (0.35 + 0.65 * np.clip(1 - (yy - W * 0.2) / (W * 0.8), 0, 1))
    gr = 232 * (0.85 + 0.15 * shade)
    gg = 214 * (0.8 + 0.2 * shade)
    gb = 196 * (0.8 + 0.2 * shade)
    gl = Image.fromarray(np.dstack([gr, gg, gb, fa * 255]).astype(np.uint8), 'RGBA')
    out.alpha_composite(gl)
    out = out.resize((S, S), Image.LANCZOS)
    out.save(os.path.join(OUT, name + '.png'))
    return out

def jump_extra(gm):
    im = Image.new('L', (gm.width, int(gm.height * 1.25)), 0)
    im.paste(gm, (0, 0))
    d = ImageDraw.Draw(im)
    y = int(gm.height * 1.15)
    h = max(3, int(gm.height * 0.06))
    d.rectangle([int(gm.width * 0.05), y, int(gm.width * 0.4), y + h], fill=255)
    d.rectangle([int(gm.width * 0.55), y, int(gm.width * 0.95), y + h], fill=255)
    return im

def make_frame(name='hud_minimap_frame', seed=17):
    random.seed(seed)
    yy, xx = np.mgrid[0:W, 0:W].astype(np.float32)
    cx, ccy = W * 0.5, W * 0.46
    R = W * 0.38
    dx, dy = xx - cx, yy - ccy
    dist = np.sqrt(dx * dx + dy * dy)
    ang = np.arctan2(dy, dx)
    n1 = noise(seed, 64)
    n3 = noise(seed + 3, 3)
    wob = (np.sin(ang * 7 + seed) * 0.004 + np.sin(ang * 13 + seed * 2) * 0.003) * W + (n1 - 0.5) * W * 0.008
    edge = R + wob
    ring_w = W * 0.032
    ring = np.clip(1 - np.abs(dist - (edge - ring_w * 0.6)) / ring_w, 0, 1)
    gaps = (noise(seed + 11, 40) > 0.86).astype(np.float32)
    ring = ring * (1 - gaps * 0.85) * (0.85 + n3 * 0.3)
    img = Image.fromarray(np.dstack([np.full_like(ring, 150), np.full_like(ring, 8), np.full_like(ring, 10), ring * 255]).astype(np.uint8), 'RGBA')
    glow_m = Image.fromarray((ring * 255).astype(np.uint8), 'L').filter(ImageFilter.GaussianBlur(W * 0.018))
    glow = Image.new('RGBA', (W, W), (170, 0, 0, 0))
    glow.putalpha(glow_m.point(lambda v: int(v * 0.6)))
    out = Image.new('RGBA', (W, W), (0, 0, 0, 0))
    out.alpha_composite(glow)
    out.alpha_composite(img)
    d = ImageDraw.Draw(out)
    for i in range(4):
        a0 = math.radians(random.uniform(55, 125))
        x = cx + (R - ring_w * 1.2) * math.cos(a0)
        y0 = ccy + (R - ring_w * 1.2) * math.sin(a0)
        L = random.uniform(0.05, 0.1) * W
        w0 = random.uniform(0.009, 0.015) * W
        col = (120, 4, 6, 245)
        d.polygon([(x - w0, y0), (x + w0, y0), (x + w0 * 0.55, y0 + L), (x - w0 * 0.55, y0 + L)], fill=col)
        rd = w0 * 1.25
        d.ellipse([x - rd, y0 + L - rd * 0.6, x + rd, y0 + L + rd * 1.4], fill=col)
    out = out.resize((S * 2, S * 2), Image.LANCZOS)
    out.save(os.path.join(OUT, name + '.png'))
    return out

ITEMS = [
    ('hud_sprint', 'fa:person-running', 0.42, 0, False, None, 3),
    ('hud_jump', 'fa:person-running', 0.36, 18, False, jump_extra, 5),
    ('hud_settings', 'mdi:cog', 0.42, 0, False, None, 7),
    ('hud_view', 'mdi:autorenew', 0.44, 0, False, None, 9),
    ('hud_flashlight', 'mdi:flashlight', 0.42, -45, False, None, 11),
    ('hud_door', 'mdi:door-open', 0.4, 0, False, None, 13),
    ('hud_autorun', 'mdi:run-fast', 0.46, 0, False, None, 19),
]

if __name__ == '__main__':
    ims = []
    for n, k, s, r, f, e, sd in ITEMS:
        ims.append(make(n, k, s, r, f, extra=e, seed=sd))
    sheet = Image.new('RGBA', (len(ims) * 266 + 10, 276), (70, 72, 60, 255))
    for i, im in enumerate(ims):
        sheet.alpha_composite(im, (10 + i * 266, 10))
    sheet.save(os.path.join(OUT, '_sheet.png'))
    make_frame()
