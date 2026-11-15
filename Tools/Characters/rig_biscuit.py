"""Rigs Biscuit's static Meshy mesh with a dog skeleton (Round 10).

Meshy's own rig for him (Biscuit_meshy_rigged.*) is a humanoid skeleton dropped onto a quadruped: its "arms" and
"feet" sit outside the body, so no animation on it can look right. This script instead:
  1. straightens the mesh (Meshy posed him about 30 degrees off the axis) so he faces +Z with his paws at y = 0;
  2. places a dog skeleton from the mesh itself (leg centres measured slice by slice, the head from the nose);
  3. weights every vertex to it: legs by distance to the leg's own column (blended into the body near the top), the
     body, neck, head, tail and ears by distance to each bone's segment, the tongue by its pink texture colour;
  4. checks for stray stretched faces (and drops any);
  5. writes Assets/_Project/Art/Characters/Biscuit/biscuit_rig.bytes (Unity space: x mirrored, winding flipped) for the
     editor step TeedOff.EditorTools.DogImport, plus a weight plot in Builds/screens/biscuit/.

python Tools/Characters/rig_biscuit.py
"""
import os
import struct
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from glb_load import load  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, 'Art', 'Characters', 'incoming', 'Biscuit')
OUT = os.path.join(ROOT, 'Assets', '_Project', 'Art', 'Characters', 'Biscuit')
PLOTS = os.path.join(ROOT, 'Builds', 'screens', 'biscuit')


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def seg_dist(P, a, b):
    ab = b - a
    t = np.clip(((P - a) @ ab) / max(ab @ ab, 1e-9), 0, 1)
    return np.linalg.norm(P - (a + t[:, None] * ab), axis=1)


def read_normals(path, j):
    b = open(path, 'rb').read()
    l = struct.unpack('<I', b[12:16])[0]
    prim = j['meshes'][0]['primitives'][0]
    acc = j['accessors'][prim['attributes']['NORMAL']]
    bv = j['bufferViews'][acc['bufferView']]
    off = 20 + l + 8 + bv.get('byteOffset', 0) + acc.get('byteOffset', 0)
    return np.frombuffer(b, dtype=np.float32, count=acc['count'] * 3, offset=off).reshape(-1, 3)


def main():
    glb = os.path.join(SRC, 'Biscuit_meshy.glb')
    j, P, I, UV = load(glb)
    N = read_normals(glb, j)

    # 1. Straighten: the axis from the back paws' centre to the front paws'.
    F = P[P[:, 1] < P[:, 1].min() + 0.13]
    guess = np.array([0.49, 0, 0.86])
    fr = F[F @ guess > 0]
    bk = F[F @ guess <= 0]
    ax = fr[:, [0, 2]].mean(0) - bk[:, [0, 2]].mean(0)
    yaw = np.arctan2(ax[0], ax[1])
    c, s = np.cos(-yaw), np.sin(-yaw)
    R = np.array([[c, 0, s], [0, 1, 0], [-s, 0, c]])
    Q = P @ R.T
    Nn = N @ R.T
    mid = R @ ((fr.mean(0) + bk.mean(0)) / 2)
    Q[:, 0] -= mid[0]
    Q[:, 2] -= mid[2]
    Q[:, 1] -= Q[:, 1].min()
    print(f'straightened by {np.degrees(yaw):.1f} deg; size {np.round(Q.max(0) - Q.min(0), 3)}')

    im = np.asarray(Image.open(os.path.join(SRC, 'Biscuit_meshy_basecolor.png')).convert('RGB')).astype(float) / 255
    h, w, _ = im.shape
    col = im[np.clip((UV[:, 1] * h).astype(int), 0, h - 1), np.clip((UV[:, 0] * w).astype(int), 0, w - 1)]

    # 2. The skeleton, from the mesh.
    def leg_centre(y, front, side, band=0.03):
        m = (abs(Q[:, 1] - y) < band) & ((Q[:, 2] > 0) == front) & (np.sign(Q[:, 0]) == side)
        if y > 0.3:  # above the paws the slice includes the chest / belly: keep the leg's own column
            lo = leg_centre(0.25, front, side)
            m &= np.linalg.norm(Q[:, [0, 2]] - lo[[0, 2]], axis=1) < 0.14
        return Q[m].mean(0)

    dark = (col.max(1) < 0.15) & (Q[:, 1] > 0.9) & (Q[:, 2] > 0.6)
    nose = Q[dark].mean(0)
    headc = np.array([nose[0] * 0.15, 1.06, 0.70])
    bones = []  # name, parent, start, end, radius

    def bone(name, parent, a, t, r):
        bones.append([name, parent, np.array(a, float), np.array(t, float), r])
        return len(bones) - 1

    root = bone('Root', -1, [0, 0, 0], [0, 0.3, 0], 0.01)
    hips = bone('Hips', root, [0, 0.70, -0.40], [0, 0.72, -0.05], 0.26)
    spine = bone('Spine', hips, [0, 0.72, -0.05], [0, 0.73, 0.30], 0.27)
    chest = bone('Chest', spine, [0, 0.73, 0.30], [0, 0.86, 0.52], 0.26)
    neck = bone('Neck', chest, [0, 0.86, 0.52], [headc[0] * 0.5, 0.98, 0.62], 0.2)
    head = bone('Head', neck, [headc[0] * 0.5, 0.98, 0.62], nose, 0.17)
    hd = nose - headc
    hd[1] = 0
    hd /= np.linalg.norm(hd)
    perp = np.array([hd[2], 0, -hd[0]])  # the head's right
    ears = []
    for sgn, nm in [(1, 'EarA'), (-1, 'EarB')]:
        a = headc + perp * sgn * 0.17 + np.array([0, 0.08, 0]) - hd * 0.04
        t = headc + perp * sgn * 0.24 + np.array([0, -0.14, 0]) - hd * 0.02
        ears.append(bone(nm, head, a, t, 0.07))
    tongue_root = headc + hd * 0.18 + np.array([0, -0.08, 0])
    tongue = bone('Tongue', head, tongue_root, tongue_root + hd * 0.08 + np.array([0, -0.1, 0]), 0.05)
    bone('Nose', head, nose, nose + hd * 0.05, 0.01)  # no weights: marks where the nose points
    tail_pts = [[0, 0.80, -0.55], [0, 0.98, -0.80], [0, 1.20, -0.82], [0, 1.32, -0.66], [0, 1.33, -0.52]]
    tails = []
    p = hips
    for k in range(4):
        p = bone(f'Tail{k}', p, tail_pts[k], tail_pts[k + 1], 0.13)
        tails.append(p)
    legs = []
    for front, nm, parent in [(True, 'Front', chest), (False, 'Back', hips)]:
        for side in (1, -1):
            # glTF +x is Unity -x (mirrored at export): name by the Unity side, +x = right.
            sn = 'L' if side > 0 else 'R'
            lo = leg_centre(0.05, front, side)
            up = leg_centre(0.45, front, side)
            top = up.copy()
            top[1] = 0.64
            knee = leg_centre(0.33 if front else 0.36, front, side)
            ankle = leg_centre(0.12, front, side)
            paw = lo.copy()
            paw[1] = 0.03
            if front:
                paw[2] += 0.05
            u = bone(f'{nm}Upper{sn}', parent, top, knee, 0.1)
            m_ = bone(f'{nm}Lower{sn}', u, knee, ankle, 0.08)
            f_ = bone(f'{nm}Paw{sn}', m_, ankle, paw, 0.08)
            legs.append((front, side, [u, m_, f_], [top, knee, ankle, paw]))

    nb = len(bones)
    nv = len(Q)
    W = np.zeros((nv, nb))

    # 3a. Body, neck, head, tail, ears: inverse distance to each segment (in units of the bone's radius).
    body_set = [hips, spine, chest, neck, head] + tails + ears
    D = np.stack([seg_dist(Q, bones[k][2], bones[k][3]) / bones[k][4] for k in body_set], 1)
    allow = np.ones_like(D, bool)
    for i, k in enumerate(body_set):
        if k in tails:
            allow[:, i] = (Q[:, 2] < -0.42) & (Q[:, 1] > 0.55)
        if k == head:
            allow[:, i] = Q[:, 2] > 0.45
        if k in ears:
            off = (Q - headc) @ perp * (1 if k == ears[0] else -1)
            allow[:, i] = (off > 0.13) & (Q[:, 1] < headc[1] + 0.12) & (Q[:, 2] > 0.45)
    Wb = np.where(allow, 1.0 / (D + 0.05) ** 4, 0)
    in_tail = (Q[:, 2] < -0.62) & (Q[:, 1] > 0.6)
    for i, k in enumerate(body_set):
        if k not in tails:
            Wb[in_tail, i] *= 0.02
    Wb /= Wb.sum(1, keepdims=True)
    for i, k in enumerate(body_set):
        W[:, k] = Wb[:, i]

    # 3b. Legs: closeness to the leg's own column, faded into the body by the top of the leg.
    def axis_xz(pts, y):
        ys = np.array([q[1] for q in pts])[::-1]
        xs = np.array([q[0] for q in pts])[::-1]
        zs = np.array([q[2] for q in pts])[::-1]
        return np.stack([np.interp(y, ys, xs), np.interp(y, ys, zs)], 1)

    best = np.zeros(nv)
    which = np.full(nv, -1)
    legW = []
    for li, (front, side, ids, pts) in enumerate(legs):
        a = axis_xz(pts, Q[:, 1])
        hdist = np.linalg.norm(Q[:, [0, 2]] - a, axis=1)
        top_fade = 0.58 if front else 0.62
        k = smooth(0.17, 0.11, hdist) * smooth(top_fade, 0.40, Q[:, 1]) * ((Q[:, 2] > 0) == front)
        sel = k > best
        best = np.where(sel, k, best)
        which = np.where(sel, li, which)
        Dl = np.stack([seg_dist(Q, bones[i][2], bones[i][3]) for i in ids], 1)
        Wl = 1.0 / (Dl + 0.01) ** 4
        legW.append(Wl / Wl.sum(1, keepdims=True))
    for li, (front, side, ids, pts) in enumerate(legs):
        m = which == li
        W[m] *= (1 - best[m])[:, None]
        for c_, i in enumerate(ids):
            W[m, i] += best[m] * legW[li][m, c_]

    # 3c. Tongue: the pink in the mouth.
    r_, g_, b_ = col[:, 0], col[:, 1], col[:, 2]
    pink = (r_ > 0.6) & (g_ < 0.6) & (r_ - g_ > 0.22) & (b_ > 0.3) & (b_ > g_ * 0.8) & (np.linalg.norm(Q - headc, axis=1) < 0.45) & (Q[:, 1] < headc[1] + 0.05)
    W[pink] *= 0.15
    W[pink, tongue] += 0.85
    print(f'tongue vertices {pink.sum()}')

    idx = np.argsort(-W, 1)[:, :4]
    wt = np.take_along_axis(W, idx, 1)
    wt /= wt.sum(1, keepdims=True)

    # 4. Stray stretched faces.
    T = I.reshape(-1, 3).astype(np.int64)
    e = np.stack([np.linalg.norm(Q[T[:, a]] - Q[T[:, b]], axis=1) for a, b in [(0, 1), (1, 2), (2, 0)]], 1).max(1)
    med = np.median(e)
    stray = e > 8 * med
    print(f'triangles {len(T)}; edge median {med:.3f}, longest {e.max():.3f}; stray (> 8x median): {stray.sum()}')
    T = T[~stray]

    os.makedirs(PLOTS, exist_ok=True)
    try:
        import matplotlib
        matplotlib.use('Agg')
        import matplotlib.pyplot as plt
        dom = idx[:, 0]
        pal = np.random.default_rng(3).random((nb, 3)) * 0.8 + 0.1
        fig, axs = plt.subplots(1, 3, figsize=(21, 7))
        for a, (u, v, t, dep) in zip(axs, [(2, 1, 'side', 0), (0, 1, 'front', 2), (0, 2, 'top', 1)]):
            o = np.argsort(Q[:, dep])
            a.scatter(Q[o, u], Q[o, v], s=0.6, c=pal[dom[o]])
            for bn in bones:
                a.plot([bn[2][u], bn[3][u]], [bn[2][v], bn[3][v]], 'k-', lw=2)
            a.set_aspect('equal')
            a.set_title(t)
        plt.savefig(os.path.join(PLOTS, 'weights.png'), dpi=70)
        plt.close()
    except ImportError:
        pass

    # 5. Unity space: mirror x, flip the winding.
    def U(v):
        return np.array([-v[0], v[1], v[2]], np.float32)
    Qu = Q.astype(np.float32).copy()
    Qu[:, 0] *= -1
    Nu = Nn.astype(np.float32).copy()
    Nu[:, 0] *= -1
    Tu = T[:, [0, 2, 1]].astype(np.int32)
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, 'biscuit_rig.bytes'), 'wb') as f:
        f.write(b'BDOG')
        f.write(struct.pack('<iiii', 1, nv, len(Tu), nb))
        for name, parent, a, t, r in bones:
            nmb = name.encode()[:31]
            f.write(nmb + b'\0' * (32 - len(nmb)))
            f.write(struct.pack('<i', parent))
            f.write(U(a).tobytes())
            f.write(U(t).tobytes())
        f.write(np.ascontiguousarray(Qu).tobytes())
        f.write(np.ascontiguousarray(Nu).tobytes())
        f.write(np.ascontiguousarray(UV.astype(np.float32)).tobytes())
        f.write(np.ascontiguousarray(idx.astype(np.int32)).tobytes())
        f.write(np.ascontiguousarray(wt.astype(np.float32)).tobytes())
        f.write(np.ascontiguousarray(Tu).tobytes())
    Image.open(os.path.join(SRC, 'Biscuit_meshy_basecolor.png')).save(os.path.join(OUT, 'Biscuit_basecolor.png'))
    print(f'wrote {nv} vertices, {len(Tu)} triangles, {nb} bones -> {OUT}')


if __name__ == '__main__':
    main()
