import sys, os, wave
import numpy as np
OUT = sys.argv[1]
SR = 22050
rng = np.random.default_rng(3)
def lp_fast(x, cutoff):
    n = len(x)
    f = np.fft.rfftfreq(n, 1 / SR)
    X = np.fft.rfft(x)
    X *= 1 / (1 + (f / cutoff) ** 4)
    return np.fft.irfft(X, n)
def bp_fast(x, lo, hi):
    n = len(x)
    f = np.fft.rfftfreq(n, 1 / SR)
    X = np.fft.rfft(x)
    X *= (1 / (1 + (f / hi) ** 4)) * (1 - 1 / (1 + (f / lo) ** 4))
    return np.fft.irfft(X, n)
def save(name, x):
    x = x / (np.max(np.abs(x)) + 1e-9) * 0.89
    d = (x * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name), 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(d.tobytes())
def reverb(x, sizes=(1557, 1617, 1491, 1422, 1277, 1356), fb=0.84):
    out = np.zeros(len(x))
    for s in sizes:
        c = np.copy(x)
        k = 1
        g = fb
        while k * s < len(x) and g > 0.01:
            c[k * s:] += x[:-k * s] * g
            k += 1
            g *= fb
        out += c
    return out / len(sizes)
def music():
    L = 48.0
    n = int(L * SR)
    t = np.arange(n) / SR
    x = np.zeros(n)
    sw = 0.5 + 0.5 * np.sin(2 * np.pi * t / 24.0 - np.pi / 2)
    sw2 = 0.5 + 0.5 * np.sin(2 * np.pi * t / 16.0)
    for f, a in [(55.0, 0.5), (55.35, 0.4), (82.41, 0.18), (58.27, 0.12)]:
        x += a * np.sin(2 * np.pi * f * t + 0.3 * np.sin(2 * np.pi * 0.1 * t))
    x += 0.22 * sw * np.sin(2 * np.pi * 110.0 * t) * (0.6 + 0.4 * np.sin(2 * np.pi * 3 * t / L))
    x += 0.10 * sw2 * np.sin(2 * np.pi * 116.54 * t)
    wind = lp_fast(rng.standard_normal(n), 500) * (0.25 + 0.2 * np.sin(2 * np.pi * t / 12.0) ** 2)
    x += wind * 1.2
    notes = [(2.0, 440.0), (5.0, 523.25), (8.0, 493.88), (11.5, 415.30), (18.0, 440.0), (21.0, 349.23), (24.0, 329.63), (30.0, 440.0), (33.0, 523.25), (36.0, 622.25), (39.5, 587.33), (42.5, 440.0)]
    bell = np.zeros(n)
    for st, f in notes:
        i0 = int(st * SR)
        m = min(n - i0, int(4 * SR))
        tt = np.arange(m) / SR
        env = np.exp(-tt * 1.4) * np.clip(tt * 200, 0, 1)
        tone = np.sin(2 * np.pi * f * tt) + 0.35 * np.sin(2 * np.pi * f * 2.76 * tt) * np.exp(-tt * 3) + 0.2 * np.sin(2 * np.pi * f * 5.4 * tt) * np.exp(-tt * 6)
        bell[i0:i0 + m] += tone * env * 0.16
    bell = reverb(bell)
    x += bell
    for st in [14.0, 38.0]:
        i0 = int(st * SR)
        m = int(3 * SR)
        tt = np.arange(m) / SR
        env = np.sin(np.pi * tt / 3) ** 2
        f0 = 900 + 400 * np.sin(2 * np.pi * 0.7 * tt)
        ph = 2 * np.pi * np.cumsum(f0) / SR
        x[i0:i0 + m] += 0.05 * env * np.sin(ph) * (0.5 + 0.5 * np.sin(2 * np.pi * 6 * tt))
    hb = np.zeros(n)
    for k in range(int(L / 1.6)):
        for off, amp in [(0.0, 1.0), (0.28, 0.7)]:
            i0 = int((k * 1.6 + off) * SR)
            m = min(n - i0, int(0.25 * SR))
            if m <= 0: continue
            tt = np.arange(m) / SR
            hb[i0:i0 + m] += amp * np.sin(2 * np.pi * 48 * tt) * np.exp(-tt * 22)
    x += hb * 0.5 * (0.3 + 0.7 * sw)
    fade = int(2.0 * SR)
    tail = x[-fade:].copy()
    ramp = np.linspace(0, 1, fade)
    x = x[:-fade].copy()
    x[:fade] = x[:fade] * ramp + tail * (1 - ramp)
    save('lobby_music.wav', x)
def thunder():
    L = 6.0
    n = int(L * SR)
    t = np.arange(n) / SR
    crack = bp_fast(rng.standard_normal(n), 300, 5000) * np.exp(-t * 9) * np.clip(t * 400, 0, 1)
    rum = lp_fast(rng.standard_normal(n), 120) * 6
    mod = lp_fast(np.abs(rng.standard_normal(n)), 3) * 8
    env = np.clip(t * 6, 0, 1) * np.exp(-t * 0.65)
    rum = rum * env * (0.5 + np.clip(mod, 0, 2))
    x = crack * 0.9 + rum
    x *= np.clip((L - t) * 2, 0, 1)
    save('lobby_thunder.wav', x)
music()
thunder()
