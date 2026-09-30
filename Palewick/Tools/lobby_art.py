import os, sys, math, random
import numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
OUT = sys.argv[1]
BG = sys.argv[2]
os.makedirs(OUT, exist_ok=True)
FONT = os.path.join(HERE, '..', 'UI_Lobby', 'Creepster.ttf')
random.seed(7)
def noise(seed, w, h, scale):
    rng = np.random.default_rng(seed)
    n = rng.random((h // scale + 2, w // scale + 2)).astype(np.float32)
    im = Image.fromarray((n * 255).astype(np.uint8), 'L').resize((w, h), Image.BICUBIC)
    return np.asarray(im).astype(np.float32) / 255.0
def tile_noise(seed, w, h, cells):
    rng = np.random.default_rng(seed)
    cx, cy = cells
    g = rng.random((cy + 1, cx + 1)).astype(np.float32)
    g[:, -1] = g[:, 0]
    im = Image.fromarray((g * 255).astype(np.uint8), 'L').resize((w + w // cx, h + h // cy), Image.BICUBIC)
    a = np.asarray(im).astype(np.float32)[: h, : w] / 255.0
    return a
def bg():
    im = Image.open(BG).convert('RGB').resize((1920, 1080), Image.LANCZOS)
    im.save(os.path.join(OUT, 'lobby_bg.jpg'), quality=90)
def fog():
    w, h = 1024, 256
    f = np.zeros((h, w), np.float32)
    for i, (c, amp) in enumerate([((4, 1), 0.5), ((8, 2), 0.3), ((16, 4), 0.2)]):
        f += tile_noise(10 + i, w, h, c) * amp
    yy = np.linspace(0, 1, h)[:, None]
    band = np.clip(np.sin(yy * math.pi), 0, 1) ** 1.5
    a = np.clip((f - 0.35) * 1.8, 0, 1) * band
    rgba = np.dstack([np.full_like(a, 200), np.full_like(a, 212), np.full_like(a, 220), a * 200]).astype(np.uint8)
    Image.fromarray(rgba, 'RGBA').save(os.path.join(OUT, 'lobby_fog.png'))
def ember():
    s = 64
    yy, xx = np.mgrid[0:s, 0:s].astype(np.float32)
    d = np.sqrt((xx - s / 2 + 0.5) ** 2 + (yy - s / 2 + 0.5) ** 2) / (s / 2)
    core = np.clip(1 - d * 3.2, 0, 1)
    glow = np.clip(1 - d, 0, 1) ** 2.2
    r = np.clip(255 * (glow + core), 0, 255)
    g = np.clip(90 * glow + 200 * core, 0, 255)
    b = np.clip(20 * glow + 150 * core, 0, 255)
    a = np.clip(glow * 255 + core * 255, 0, 255)
    Image.fromarray(np.dstack([r, g, b, a]).astype(np.uint8), 'RGBA').save(os.path.join(OUT, 'lobby_ember.png'))
def drip_shape(d, x, y0, L, w0, col):
    d.polygon([(x - w0, y0), (x + w0, y0), (x + w0 * 0.55, y0 + L), (x - w0 * 0.55, y0 + L)], fill=col)
    rd = w0 * 1.2
    d.ellipse([x - rd, y0 + L - rd * 0.7, x + rd, y0 + L + rd * 1.3], fill=col)
def blood_top():
    w, h = 1920, 220
    K = 2
    im = Image.new('RGBA', (w * K, h * K), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    n = noise(3, w * K, 1, 12)[0]
    top = []
    for x in range(0, w * K, 4):
        top.append((x, (18 + n[x] * 34) * K))
    d.polygon([(0, 0)] + top + [(w * K, top[-1][1]), (w * K, 0)], fill=(96, 2, 4, 255))
    rng = random.Random(5)
    for i in range(46):
        x = rng.uniform(0, w) * K
        L = rng.choice([rng.uniform(10, 40), rng.uniform(40, 150)]) * K
        w0 = rng.uniform(3, 9) * K
        drip_shape(d, x, 20 * K, L, w0, (96, 2, 4, 255))
    arr = np.asarray(im).astype(np.float32)
    hl = Image.fromarray(arr[:, :, 3].astype(np.uint8), 'L').filter(ImageFilter.GaussianBlur(3 * K))
    sh = np.asarray(hl).astype(np.float32) / 255.0
    gr = noise(8, w * K, h * K, 6)
    arr[:, :, 0] = np.clip(arr[:, :, 0] * (0.7 + gr * 0.6) + (1 - sh) * 60 * (arr[:, :, 3] > 0), 0, 255)
    im = Image.fromarray(arr.astype(np.uint8), 'RGBA')
    shadow = Image.new('RGBA', im.size, (0, 0, 0, 0))
    shadow.putalpha(im.getchannel('A').filter(ImageFilter.GaussianBlur(8 * K)).point(lambda v: int(v * 0.7)))
    out = Image.new('RGBA', im.size, (0, 0, 0, 0))
    out.alpha_composite(shadow, (0, 6 * K))
    out.alpha_composite(im)
    out.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, 'lobby_blood_top.png'))
def drip():
    w, h = 32, 128
    K = 4
    im = Image.new('RGBA', (w * K, h * K), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    drip_shape(d, w * K / 2, 0, (h - 22) * K, 7 * K, (110, 3, 6, 255))
    d.ellipse([w * K / 2 - 4 * K, (h - 18) * K, w * K / 2 - 1 * K, (h - 12) * K], fill=(210, 70, 70, 170))
    im.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, 'lobby_drip.png'))
def vignette():
    s = 512
    yy, xx = np.mgrid[0:s, 0:s].astype(np.float32)
    dx = (xx - s / 2) / (s / 2)
    dy = (yy - s / 2) / (s / 2)
    d = np.sqrt(dx * dx * 0.8 + dy * dy)
    a = np.clip((d - 0.55) / 0.75, 0, 1) ** 1.6
    rgba = np.dstack([np.full_like(a, 70), np.zeros_like(a), np.full_like(a, 4), a * 255]).astype(np.uint8)
    Image.fromarray(rgba, 'RGBA').save(os.path.join(OUT, 'lobby_vignette.png'))
def title():
    K = 2
    w, h = 1100, 300
    font = ImageFont.truetype(FONT, 190 * K)
    txt = 'PALEWICK'
    m = Image.new('L', (w * K, h * K), 0)
    d = ImageDraw.Draw(m)
    bb = d.textbbox((0, 0), txt, font=font)
    tx = (w * K - (bb[2] - bb[0])) / 2 - bb[0]
    ty = 20 * K - bb[1]
    d.text((tx, ty), txt, font=font, fill=255)
    rng = random.Random(11)
    arr = np.asarray(m)
    cols = np.where(arr.max(axis=0) > 0)[0]
    for i in range(16):
        x = int(rng.choice(cols))
        ys = np.where(arr[:, x] > 0)[0]
        y0 = ys.max() - 4 * K
        drip_shape(d, x, y0, rng.uniform(15, 70) * K, rng.uniform(3, 6) * K, 255)
    mf = np.asarray(m).astype(np.float32) / 255.0
    yy = np.linspace(0, 1, h * K)[:, None]
    gn = noise(4, w * K, h * K, 5)
    r = np.clip(120 + 110 * (1 - yy) + (gn - 0.5) * 60, 0, 255) * np.ones_like(mf)
    g = np.clip(6 + 18 * (1 - yy) + (gn - 0.5) * 10, 0, 255) * np.ones_like(mf)
    b = np.clip(8 + 14 * (1 - yy), 0, 255) * np.ones_like(mf)
    txtim = Image.fromarray(np.dstack([r, g, b, mf * 255]).astype(np.uint8), 'RGBA')
    glow = Image.new('RGBA', m.size, (200, 0, 0, 0))
    glow.putalpha(m.filter(ImageFilter.GaussianBlur(14 * K)).point(lambda v: int(v * 0.9)))
    shadow = Image.new('RGBA', m.size, (0, 0, 0, 0))
    shadow.putalpha(m.filter(ImageFilter.GaussianBlur(4 * K)))
    edge = m.filter(ImageFilter.FIND_EDGES).filter(ImageFilter.GaussianBlur(1))
    edim = Image.new('RGBA', m.size, (255, 180, 170, 0))
    edim.putalpha(edge.point(lambda v: int(v * 0.35)))
    out = Image.new('RGBA', m.size, (0, 0, 0, 0))
    out.alpha_composite(glow)
    out.alpha_composite(shadow, (0, 6 * K))
    out.alpha_composite(txtim)
    out.alpha_composite(edim)
    out.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, 'lobby_title.png'))
def plate(name, w, h, base, edge, seed, drips=True, radius=14):
    K = 3
    W, H = w * K, h * K
    rng = random.Random(seed)
    m = Image.new('L', (W, H), 0)
    d = ImageDraw.Draw(m)
    pts = []
    steps = 40
    j = 5 * K
    for i in range(steps + 1):
        pts.append((j + (W - 2 * j) * i / steps, j + rng.uniform(-2, 2) * K))
    for i in range(1, steps):
        pts.append((W - j + rng.uniform(-2, 2) * K, j + (H - 2 * j) * i / steps))
    for i in range(steps, -1, -1):
        pts.append((j + (W - 2 * j) * i / steps, H - j * 3 + rng.uniform(-2, 2) * K))
    for i in range(steps - 1, 0, -1):
        pts.append((j + rng.uniform(-2, 2) * K, j + (H - 4 * j) * i / steps))
    d.polygon(pts, fill=255)
    if drips:
        for i in range(5):
            x = rng.uniform(0.08, 0.92) * W
            drip_shape(d, x, H - j * 3 - 2 * K, rng.uniform(3, 11) * K, rng.uniform(2, 4) * K, 255)
    mf = np.asarray(m).astype(np.float32) / 255.0
    yy = np.linspace(0, 1, H)[:, None]
    gn = noise(seed, W, H, 7 * K) - 0.5
    gn2 = noise(seed + 1, W, H, 2 * K) - 0.5
    shade = 1.15 - yy * 0.45
    r = np.clip((base[0] + gn * 40 + gn2 * 25) * shade, 0, 255)
    g = np.clip((base[1] + gn * 8 + gn2 * 6) * shade, 0, 255)
    b = np.clip((base[2] + gn * 8 + gn2 * 6) * shade, 0, 255)
    er = m.filter(ImageFilter.MinFilter(2 * (2 * K) + 1))
    ring = np.clip(mf - np.asarray(er).astype(np.float32) / 255.0, 0, 1)
    r = r * (1 - ring) + edge[0] * ring
    g = g * (1 - ring) + edge[1] * ring
    b = b * (1 - ring) + edge[2] * ring
    body = Image.fromarray(np.dstack([r, g, b, mf * 255]).astype(np.uint8), 'RGBA')
    glow = Image.new('RGBA', (W, H), (190, 0, 0, 0))
    glow.putalpha(Image.fromarray((ring * 255).astype(np.uint8), 'L').filter(ImageFilter.GaussianBlur(4 * K)).point(lambda v: int(v * 0.7)))
    out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    out.alpha_composite(body)
    out.alpha_composite(glow)
    dd = ImageDraw.Draw(out)
    for i in range(6):
        x0, y0 = rng.uniform(0.05, 0.95) * W, rng.uniform(0.15, 0.7) * H
        l = rng.uniform(0.03, 0.09) * W
        th = rng.uniform(-0.5, 0.5)
        dd.line([(x0, y0), (x0 + l * math.cos(th), y0 + l * math.sin(th))], fill=(0, 0, 0, 70), width=K)
    out.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, name + '.png'))
def panel():
    s = 256
    K = 2
    W = s * K
    im = Image.new('RGBA', (W, W), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([4 * K, 4 * K, W - 4 * K, W - 4 * K], 18 * K, fill=(12, 6, 8, 236), outline=(140, 8, 10, 255), width=3 * K)
    d.rounded_rectangle([10 * K, 10 * K, W - 10 * K, W - 10 * K], 13 * K, outline=(60, 4, 6, 255), width=K)
    im.resize((s, s), Image.LANCZOS).save(os.path.join(OUT, 'lobby_panel.png'))
def ring():
    s = 256
    K = 2
    W = s * K
    yy, xx = np.mgrid[0:W, 0:W].astype(np.float32)
    dx, dy = xx - W / 2, yy - W / 2
    dist = np.sqrt(dx * dx + dy * dy)
    ang = (np.arctan2(dy, dx) + math.pi) / (2 * math.pi)
    band = np.clip(1 - np.abs(dist - W * 0.4) / (W * 0.035), 0, 1)
    a = band * ang ** 1.5
    rgba = np.dstack([np.full_like(a, 190), np.full_like(a, 10), np.full_like(a, 12), a * 255]).astype(np.uint8)
    Image.fromarray(rgba, 'RGBA').resize((s, s), Image.LANCZOS).save(os.path.join(OUT, 'lobby_spinner.png'))
def glow_pad():
    s = 256
    yy, xx = np.mgrid[0:s, 0:s].astype(np.float32)
    dx = (xx - s / 2) / (s / 2)
    dy = (yy - s / 2) / (s / 2) * 3.2
    d = np.sqrt(dx * dx + dy * dy)
    a = np.clip(1 - d, 0, 1) ** 1.8
    rgba = np.dstack([np.full_like(a, 200), np.full_like(a, 10), np.full_like(a, 12), a * 255]).astype(np.uint8)
    Image.fromarray(rgba, 'RGBA').save(os.path.join(OUT, 'lobby_floor_glow.png'))
def bars():
    import numpy as np
    w, h = 1024, 40
    K = 2
    im = Image.new('RGBA', (w * K, h * K), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, 0, w * K - 1, h * K - 1], 10 * K, fill=(14, 6, 8, 235), outline=(130, 8, 10, 255), width=3 * K)
    im.resize((w, h), Image.LANCZOS).save(os.path.join(OUT, 'lobby_bar_bg.png'))
    fw, fh = 1000, 26
    yy = np.linspace(0, 1, fh)[:, None] * np.ones((1, fw))
    gn = noise(12, fw, fh, 4)
    r = np.clip(200 - yy * 90 + (gn - 0.5) * 50, 0, 255)
    g = np.clip(14 - yy * 8 + (gn - 0.5) * 10, 0, 255)
    b = np.clip(16 - yy * 8, 0, 255)
    a = np.full_like(r, 255)
    a[:, :] = 255
    fill = Image.fromarray(np.dstack([r, g, b, a]).astype(np.uint8), 'RGBA')
    m = Image.new('L', (fw, fh), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, fw - 1, fh - 1], 8, fill=255)
    fill.putalpha(m)
    hl = Image.new('RGBA', (fw, fh), (255, 140, 130, 0))
    hm = Image.new('L', (fw, fh), 0)
    ImageDraw.Draw(hm).rectangle([6, 3, fw - 7, 6], fill=90)
    hl.putalpha(hm)
    fill.alpha_composite(hl)
    fill.save(os.path.join(OUT, 'lobby_bar_fill.png'))
def icons():
    import importlib
    os.environ.setdefault('X', '1')
    sys.argv = [sys.argv[0], OUT]
    hb = importlib.import_module('hud_button_style')
    hb.OUT = OUT
    hb.make('lobby_sound_on', 'mdi:volume-high', 0.46, seed=31)
    hb.make('lobby_sound_off', 'mdi:volume-off', 0.46, seed=31)
    hb.make('lobby_exit', 'mdi:exit-run', 0.44, seed=37)
    hb.make('lobby_avatar', 'mdi:account', 0.5, seed=41)
    hb.make('lobby_close', 'mdi:close-thick', 0.42, seed=43)
    hb.make('lobby_headphones', 'mdi:headphones', 0.46, seed=47)
if __name__ == '__main__':
    bg(); fog(); ember(); blood_top(); drip(); vignette(); title(); panel(); ring(); glow_pad()
    plate('lobby_btn_start', 560, 170, (120, 6, 8), (220, 40, 30), 3)
    plate('lobby_btn_side', 440, 104, (34, 6, 8), (150, 10, 12), 5, drips=False)
    plate('lobby_nameplate', 520, 120, (22, 6, 8), (130, 8, 10), 9, drips=False)
    bars()
    icons()
