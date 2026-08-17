#!/usr/bin/env python3
"""#138 투사체 스프라이트 2종 생성기.

의존성: 표준 라이브러리(zlib, struct)뿐. 결정적(난수 없음) — 다시 돌리면 같은 PNG.
출력: Assets/Art/Projectiles/PlayerBullet.png (19x19), EnemyBullet.png (22x22)
규격: ADR-0001 — PPU 64 기준 0.297 / 0.344 유닛.
"""
import math, os, struct, zlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "Assets", "Art", "Projectiles")

def write_png(path, w, h, rgba_rows):
    def chunk(tag, data):
        return (struct.pack(">I", len(data)) + tag + data
                + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))
    raw = b"".join(b"\x00" + row for row in rgba_rows)
    png = (b"\x89PNG\r\n\x1a\n"
           + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
           + chunk(b"IDAT", zlib.compress(raw, 9))
           + chunk(b"IEND", b""))
    with open(path, "wb") as f:
        f.write(png)

def render(w, h, grid, palette):
    rows = []
    for y in range(h):
        row = bytearray()
        for x in range(w):
            row += bytes(palette.get(grid.get((x, y), "."), (0, 0, 0, 0)))
        rows.append(bytes(row))
    return rows

def neighbors(p):
    x, y = p
    return [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)]

def player_bullet():
    """+X로 볼록한 초승달 칼날. Projectile.SetDirection이 +X 기준으로 회전시킨다."""
    W = H = 19
    O, RO = (9.0, 9.5), 8.8   # 바깥 원
    I, RI = (4.6, 9.5), 7.0   # 왼쪽에서 파내는 원 → 초승달
    d = lambda p, c: math.hypot(p[0] - c[0], p[1] - c[1])
    F = {(x, y) for y in range(H) for x in range(W)
         if d((x + 0.5, y + 0.5), O) <= RO and d((x + 0.5, y + 0.5), I) > RI}
    outline = {p for p in F if any(q not in F for q in neighbors(p))}
    grid = {}
    for p in F - outline:
        pc = (p[0] + 0.5, p[1] + 0.5)
        if d(pc, O) >= 7.1 and p[0] >= 11: grid[p] = "H"   # 바깥 림 하이라이트
        elif d(pc, I) <= 8.2: grid[p] = "S"                # 안쪽 절개면 음영
        else: grid[p] = "B"
    for p in F - outline:                                   # 칼날 중앙 코어
        if 13 <= p[0] <= 14 and 7 <= p[1] <= 11: grid[p] = "C"
    for p in outline: grid[p] = "O"
    pal = {"O": (43, 53, 80, 255), "S": (157, 184, 221, 255), "B": (217, 235, 254, 255),
           "H": (238, 246, 255, 255), "C": (255, 255, 255, 255)}
    return W, H, grid, pal

def enemy_bullet():
    """8방향 가시 화염구. EnemyProjectile은 회전하지 않으므로 방사대칭."""
    W = H = 22
    C, RB = (11.0, 11.0), 6.6
    spikes = [(a * math.pi / 4, 10.6 if a % 2 == 0 else 9.6, 0.55 if a % 2 == 0 else 0.5)
              for a in range(8)]
    F = set()
    for y in range(H):
        for x in range(W):
            pc = (x + 0.5, y + 0.5)
            dx, dy = pc[0] - C[0], pc[1] - C[1]
            r = math.hypot(dx, dy)
            if r <= RB:
                F.add((x, y)); continue
            for ang, tip, wf in spikes:
                ax, ay = math.cos(ang), math.sin(ang)
                along = dx * ax + dy * ay
                perp = abs(-dx * ay + dy * ax)
                if 0 < along <= tip and perp <= max(0.75, (tip - along) * wf) and r <= tip:
                    F.add((x, y)); break
    outline = {p for p in F if any(q not in F for q in neighbors(p))}
    grid = {}
    for p in F - outline:
        pc = (p[0] + 0.5, p[1] + 0.5)
        r = math.hypot(pc[0] - C[0], pc[1] - C[1])
        if r <= 2.2: grid[p] = "C"                                     # 골드 코어
        elif r <= 4.4: grid[p] = "I"                                   # 주황 속
        elif pc[0] < C[0] and pc[1] > C[1] and r > 5.0: grid[p] = "S"  # 좌하단 음영
        else: grid[p] = "B"
    for p in outline: grid[p] = "O"
    pal = {"O": (79, 31, 21, 255), "S": (194, 58, 47, 255), "B": (255, 89, 77, 255),
           "I": (255, 138, 61, 255), "C": (255, 212, 147, 255)}
    return W, H, grid, pal

def main():
    os.makedirs(OUT, exist_ok=True)
    for name, fn in [("PlayerBullet", player_bullet), ("EnemyBullet", enemy_bullet)]:
        w, h, grid, pal = fn()
        print(f"{name} {w}x{h}")
        for y in range(h):
            print("".join(grid.get((x, y), ".") for x in range(w)))
        write_png(os.path.join(OUT, name + ".png"), w, h, render(w, h, grid, pal))
        print()

if __name__ == "__main__":
    main()
