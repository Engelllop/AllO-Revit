"""Generate 16x16 and 32x32 PNG ribbon icons for AllO.

Two-tone, rounded strokes, transparent background, supersampled (draw at x8
then downscale with LANCZOS) for crisp anti-aliased edges. Single accent color
= AllO terracotta #D97757 used on one element per icon.
"""
from PIL import Image, ImageDraw
import os

SS = 8                              # supersampling factor
OUT = os.path.dirname(os.path.abspath(__file__))

NEUTRAL = (42, 42, 40, 255)         # #2A2A28  main strokes
LIGHT   = (176, 174, 165, 255)      # #B0AEA5  secondary lines
ACCENT  = (217, 119, 87, 255)       # #D97757  terracotta accent
SOFT    = (245, 233, 227, 255)      # #F5E9E3  accent fill (dim)


def f(s, x):
    return round(s * x)

# --- primitives (round caps + joints) -----------------------------------
def rline(d, pts, w, fill):
    pts = [(round(x), round(y)) for x, y in pts]
    if len(pts) > 1:
        d.line(pts, fill=fill, width=int(round(w)), joint="curve")
    r = w / 2
    for x, y in pts:
        d.ellipse([x - r, y - r, x + r, y + r], fill=fill)

def rrect(d, box, w, outline, fill=None, rad=None):
    x1, y1, x2, y2 = box
    if rad is None:
        rad = (x2 - x1) * 0.14
    d.rounded_rectangle([x1, y1, x2, y2], radius=rad, outline=outline,
                        fill=fill, width=int(round(w)))

def dot(d, c, r, fill):
    x, y = c
    d.ellipse([x - r, y - r, x + r, y + r], fill=fill)

def circle(d, c, r, w, outline, fill=None):
    x, y = c
    d.ellipse([x - r, y - r, x + r, y + r], outline=outline, fill=fill,
              width=int(round(w)))

# ------------------------------------------------------------------------
def sheet_list(d, s):
    w = f(s, 0.07)
    rrect(d, (f(s,.30), f(s,.14), f(s,.78), f(s,.70)), w, NEUTRAL, fill=(255,255,255,255))
    rrect(d, (f(s,.18), f(s,.26), f(s,.66), f(s,.82)), w, NEUTRAL, fill=(255,255,255,255))
    d.rounded_rectangle([f(s,.26), f(s,.34), f(s,.58), f(s,.40)], radius=f(s,.02), fill=ACCENT)
    for fr in (.50, .60, .70):
        rline(d, [(f(s,.26), f(s,fr)), (f(s,.58), f(s,fr))], f(s,.045), LIGHT)

def view_list(d, s):
    w = f(s, 0.07)
    rrect(d, (f(s,.20), f(s,.22), f(s,.80), f(s,.78)), w, NEUTRAL, fill=(255,255,255,255))
    d.rounded_rectangle([f(s,.28), f(s,.31), f(s,.72), f(s,.38)], radius=f(s,.02), fill=ACCENT)
    for fr in (.52, .65):
        rline(d, [(f(s,.28), f(s,fr)), (f(s,.72), f(s,fr))], f(s,.05), LIGHT)

def revisions(d, s):
    w = f(s, 0.07)
    circle(d, (f(s,.5), f(s,.5)), f(s,.32), w, NEUTRAL)
    rline(d, [(f(s,.35), f(s,.52)), (f(s,.46), f(s,.63)), (f(s,.66), f(s,.38))], f(s,.085), ACCENT)

def publish(d, s):
    w = f(s, 0.07)
    cx = f(s,.5)
    rline(d, [(f(s,.26), f(s,.62)), (f(s,.26), f(s,.80)), (f(s,.74), f(s,.80)), (f(s,.74), f(s,.62))], w, NEUTRAL)
    rline(d, [(cx, f(s,.74)), (cx, f(s,.24))], f(s,.085), ACCENT)
    rline(d, [(f(s,.36), f(s,.37)), (cx, f(s,.22)), (f(s,.64), f(s,.37))], f(s,.085), ACCENT)

def table_gen(d, s):
    w = f(s, 0.07)
    box = (f(s,.18), f(s,.18), f(s,.82), f(s,.82))
    d.rounded_rectangle(box, radius=f(s,.04), fill=(255,255,255,255))
    d.rectangle([f(s,.18), f(s,.18), f(s,.82), f(s,.34)], fill=SOFT)
    for fr in (.42, .58, .66, .82):
        if fr in (.42, .58):
            rline(d, [(f(s,.18), f(s,fr)), (f(s,.82), f(s,fr))], f(s,.04), LIGHT)
    for fr in (.40, .60):
        rline(d, [(f(s,fr), f(s,.18)), (f(s,fr), f(s,.82))], f(s,.04), LIGHT)
    rline(d, [(f(s,.18), f(s,.34)), (f(s,.82), f(s,.34))], f(s,.05), ACCENT)
    rrect(d, box, w, NEUTRAL, rad=f(s,.04))

def copy_crop(d, s):
    w = f(s, 0.07)
    rrect(d, (f(s,.16), f(s,.16), f(s,.64), f(s,.64)), w, NEUTRAL, rad=f(s,.03))
    rrect(d, (f(s,.36), f(s,.36), f(s,.84), f(s,.84)), w, ACCENT, rad=f(s,.03))

def grids(d, s):
    rline(d, [(f(s,.5), f(s,.14)), (f(s,.5), f(s,.86))], f(s,.06), ACCENT)
    rline(d, [(f(s,.14), f(s,.5)), (f(s,.86), f(s,.5))], f(s,.06), ACCENT)
    for x in (.26, .74):
        rline(d, [(f(s,x), f(s,.14)), (f(s,x), f(s,.86))], f(s,.045), LIGHT)
        rline(d, [(f(s,.14), f(s,x)), (f(s,.86), f(s,x))], f(s,.045), LIGHT)

def levels(d, s):
    for fr, col in ((.30, LIGHT), (.5, ACCENT), (.70, LIGHT)):
        y = f(s, fr)
        rline(d, [(f(s,.18), y), (f(s,.82), y)], f(s,.055), col)
        dot(d, (f(s,.18), y), f(s,.05), col)
        dot(d, (f(s,.82), y), f(s,.05), col)

def color_coder(d, s):
    w = f(s, 0.06)
    rrect(d, (f(s,.40), f(s,.16), f(s,.78), f(s,.54)), w, NEUTRAL, fill=(255,255,255,255), rad=f(s,.06))
    rrect(d, (f(s,.28), f(s,.30), f(s,.66), f(s,.68)), w, NEUTRAL, fill=(231,230,224,255), rad=f(s,.06))
    rrect(d, (f(s,.16), f(s,.44), f(s,.54), f(s,.82)), w, NEUTRAL, fill=ACCENT, rad=f(s,.06))

def match_elev(d, s):
    rline(d, [(f(s,.24), f(s,.26)), (f(s,.76), f(s,.26))], f(s,.06), NEUTRAL)
    rline(d, [(f(s,.24), f(s,.74)), (f(s,.76), f(s,.74))], f(s,.06), NEUTRAL)
    cx = f(s,.5)
    rline(d, [(cx, f(s,.30)), (cx, f(s,.70))], f(s,.06), ACCENT)
    rline(d, [(f(s,.44), f(s,.36)), (cx, f(s,.29)), (f(s,.56), f(s,.36))], f(s,.06), ACCENT)
    rline(d, [(f(s,.44), f(s,.64)), (cx, f(s,.71)), (f(s,.56), f(s,.64))], f(s,.06), ACCENT)

def param_push(d, s):
    w = f(s, 0.07)
    rrect(d, (f(s,.24), f(s,.52), f(s,.76), f(s,.84)), w, NEUTRAL, rad=f(s,.03))
    cx = f(s,.5)
    rline(d, [(cx, f(s,.16)), (cx, f(s,.46))], f(s,.08), ACCENT)
    rline(d, [(f(s,.40), f(s,.34)), (cx, f(s,.48)), (f(s,.60), f(s,.34))], f(s,.08), ACCENT)

def connector(d, s):
    cy = f(s,.5)
    rline(d, [(f(s,.20), cy), (f(s,.80), cy)], f(s,.06), NEUTRAL)
    circle(d, (f(s,.20), cy), f(s,.07), f(s,.05), NEUTRAL, fill=(255,255,255,255))
    circle(d, (f(s,.80), cy), f(s,.07), f(s,.05), NEUTRAL, fill=(255,255,255,255))
    dot(d, (f(s,.5), cy), f(s,.085), ACCENT)

def multi_connect(d, s):
    cy = f(s,.5)
    rline(d, [(f(s,.18), cy), (f(s,.5), cy)], f(s,.06), NEUTRAL)
    rline(d, [(f(s,.5), cy), (f(s,.82), f(s,.22))], f(s,.06), ACCENT)
    rline(d, [(f(s,.5), cy), (f(s,.82), f(s,.78))], f(s,.06), ACCENT)
    dot(d, (f(s,.5), cy), f(s,.06), NEUTRAL)

def split_pipe(d, s):
    cx = f(s,.5)
    rline(d, [(cx, f(s,.16)), (cx, f(s,.42))], f(s,.06), NEUTRAL)
    rline(d, [(cx, f(s,.58)), (f(s,.22), f(s,.84))], f(s,.06), NEUTRAL)
    rline(d, [(cx, f(s,.58)), (f(s,.78), f(s,.84))], f(s,.06), NEUTRAL)
    rline(d, [(f(s,.38), f(s,.50)), (f(s,.62), f(s,.50))], f(s,.07), ACCENT)

def bloom(d, s):
    cx, cy = f(s,.5), f(s,.5)
    for dx, dy in [(0,-1),(0,1),(-1,0),(1,0)]:
        circle(d, (cx+dx*f(s,.26), cy+dy*f(s,.26)), f(s,.13), f(s,.05), NEUTRAL)
    dot(d, (cx, cy), f(s,.15), ACCENT)

def reroute(d, s):
    rline(d, [(f(s,.20), f(s,.22)), (f(s,.20), f(s,.78)), (f(s,.74), f(s,.78))], f(s,.06), NEUTRAL)
    rline(d, [(f(s,.62), f(s,.40)), (f(s,.78), f(s,.24)), (f(s,.62), f(s,.24))], f(s,.07), ACCENT)
    rline(d, [(f(s,.78), f(s,.24)), (f(s,.78), f(s,.42))], f(s,.07), ACCENT)

def elbow_dir(d, s):
    rline(d, [(f(s,.22), f(s,.20)), (f(s,.22), f(s,.78)), (f(s,.80), f(s,.78))], f(s,.06), NEUTRAL)
    rline(d, [(f(s,.66), f(s,.66)), (f(s,.82), f(s,.78)), (f(s,.66), f(s,.90))], f(s,.07), ACCENT)

def one_filter(d, s):
    rline(d, [(f(s,.20), f(s,.24)), (f(s,.80), f(s,.24))], f(s,.07), NEUTRAL)
    rline(d, [(f(s,.20), f(s,.24)), (f(s,.44), f(s,.52))], f(s,.07), NEUTRAL)
    rline(d, [(f(s,.80), f(s,.24)), (f(s,.56), f(s,.52))], f(s,.07), NEUTRAL)
    rline(d, [(f(s,.44), f(s,.52)), (f(s,.44), f(s,.80))], f(s,.07), ACCENT)
    rline(d, [(f(s,.56), f(s,.52)), (f(s,.56), f(s,.70))], f(s,.07), ACCENT)

def re_ordering(d, s):
    rline(d, [(f(s,.36), f(s,.78)), (f(s,.36), f(s,.26))], f(s,.06), ACCENT)
    rline(d, [(f(s,.28), f(s,.36)), (f(s,.36), f(s,.24)), (f(s,.44), f(s,.36))], f(s,.06), ACCENT)
    rline(d, [(f(s,.64), f(s,.22)), (f(s,.64), f(s,.74))], f(s,.06), NEUTRAL)
    rline(d, [(f(s,.56), f(s,.64)), (f(s,.64), f(s,.76)), (f(s,.72), f(s,.64))], f(s,.06), NEUTRAL)

def family_export(d, s):
    w = f(s, 0.07)
    rrect(d, (f(s,.20), f(s,.34), f(s,.80), f(s,.82)), w, NEUTRAL, rad=f(s,.03))
    cx = f(s,.5)
    rline(d, [(cx, f(s,.52)), (cx, f(s,.16))], f(s,.08), ACCENT)
    rline(d, [(f(s,.40), f(s,.30)), (cx, f(s,.16)), (f(s,.60), f(s,.30))], f(s,.08), ACCENT)

def view_manager(d, s):
    cx, cy = f(s,.5), f(s,.5)
    rline(d, [(f(s,.18), cy), (f(s,.32), f(s,.34)), (f(s,.68), f(s,.34)), (f(s,.82), cy),
              (f(s,.68), f(s,.66)), (f(s,.32), f(s,.66)), (f(s,.18), cy)], f(s,.055), NEUTRAL)
    dot(d, (cx, cy), f(s,.11), ACCENT)

def auto_section_box(d, s):
    w = f(s, 0.05)
    top = [(f(s,.5),f(s,.16)),(f(s,.82),f(s,.32)),(f(s,.5),f(s,.48)),(f(s,.18),f(s,.32))]
    d.polygon(top, fill=SOFT)
    d.line(top + [top[0]], fill=ACCENT, width=int(w), joint="curve")
    rline(d, [(f(s,.18),f(s,.32)),(f(s,.5),f(s,.48)),(f(s,.5),f(s,.84)),(f(s,.18),f(s,.68)),(f(s,.18),f(s,.32))], w, NEUTRAL)
    rline(d, [(f(s,.82),f(s,.32)),(f(s,.5),f(s,.48)),(f(s,.5),f(s,.84)),(f(s,.82),f(s,.68)),(f(s,.82),f(s,.32))], w, NEUTRAL)

def wipe(d, s):
    rline(d, [(f(s,.78), f(s,.20)), (f(s,.40), f(s,.62))], f(s,.075), NEUTRAL)
    for dx in (-.10, 0, .10):
        rline(d, [(f(s,.40), f(s,.62)), (f(s,.30+dx), f(s,.84))], f(s,.05), ACCENT)

def sync_views(d, s):
    w = f(s, 0.06)
    rrect(d, (f(s,.14), f(s,.20), f(s,.44), f(s,.80)), w, NEUTRAL, fill=(255,255,255,255), rad=f(s,.03))
    rrect(d, (f(s,.56), f(s,.20), f(s,.86), f(s,.80)), w, NEUTRAL, fill=(255,255,255,255), rad=f(s,.03))
    cx = f(s,.5)
    rline(d, [(f(s,.45), f(s,.40)), (f(s,.55), f(s,.40))], f(s,.05), ACCENT)
    rline(d, [(f(s,.51), f(s,.34)), (f(s,.57), f(s,.40)), (f(s,.51), f(s,.46))], f(s,.05), ACCENT)
    rline(d, [(f(s,.55), f(s,.60)), (f(s,.45), f(s,.60))], f(s,.05), ACCENT)
    rline(d, [(f(s,.49), f(s,.54)), (f(s,.43), f(s,.60)), (f(s,.49), f(s,.66))], f(s,.05), ACCENT)

def copy_state(d, s):
    w = f(s, 0.06)
    rrect(d, (f(s,.18), f(s,.18), f(s,.62), f(s,.62)), w, NEUTRAL, fill=(255,255,255,255), rad=f(s,.03))
    rrect(d, (f(s,.38), f(s,.38), f(s,.82), f(s,.82)), w, ACCENT, fill=(255,255,255,255), rad=f(s,.03))

def paste_state(d, s):
    w = f(s, 0.06)
    rrect(d, (f(s,.22), f(s,.24), f(s,.78), f(s,.84)), w, NEUTRAL, fill=(255,255,255,255), rad=f(s,.04))
    d.rounded_rectangle([f(s,.40), f(s,.14), f(s,.60), f(s,.28)], radius=f(s,.03), fill=ACCENT)
    for fr in (.46, .58, .70):
        rline(d, [(f(s,.32), f(s,fr)), (f(s,.68), f(s,fr))], f(s,.045), LIGHT)

def match(d, s):
    rline(d, [(f(s,.22), f(s,.40)), (f(s,.78), f(s,.40))], f(s,.07), NEUTRAL)
    rline(d, [(f(s,.22), f(s,.60)), (f(s,.78), f(s,.60))], f(s,.07), ACCENT)

def link_family(d, s):
    w = f(s, 0.06)
    d.arc([f(s,.16), f(s,.34), f(s,.52), f(s,.66)], 40, 320, fill=NEUTRAL, width=int(w))
    d.arc([f(s,.48), f(s,.34), f(s,.84), f(s,.66)], 220, 140, fill=ACCENT, width=int(w))

def link_visibility(d, s):
    cx, cy = f(s,.5), f(s,.44)
    rline(d, [(f(s,.20), cy), (f(s,.34), f(s,.30)), (f(s,.66), f(s,.30)), (f(s,.80), cy),
              (f(s,.66), f(s,.58)), (f(s,.34), f(s,.58)), (f(s,.20), cy)], f(s,.05), NEUTRAL)
    dot(d, (cx, cy), f(s,.10), ACCENT)
    rline(d, [(f(s,.34), f(s,.74)), (f(s,.66), f(s,.74))], f(s,.05), LIGHT)

def net_tree(d, s):
    tx = f(s,.28)
    rline(d, [(tx, f(s,.18)), (tx, f(s,.82))], f(s,.055), NEUTRAL)
    for y, x in ((.26, .78), (.5, .66), (.74, .78)):
        rline(d, [(tx, f(s,y)), (f(s,x), f(s,y))], f(s,.055), NEUTRAL)
        dot(d, (f(s,x), f(s,y)), f(s,.07), ACCENT)

def ai_connector(d, s):
    cx, cy = f(s,.40), f(s,.40)
    r = f(s,.22)
    for dx, dy in [(1,0),(0,1),(0.7,0.7),(0.7,-0.7)]:
        rline(d, [(cx-r*dx, cy-r*dy), (cx+r*dx, cy+r*dy)], f(s,.06), ACCENT)
    rline(d, [(cx+r*0.5, cy+r*0.5), (f(s,.80), f(s,.80))], f(s,.05), NEUTRAL)
    dot(d, (f(s,.80), f(s,.80)), f(s,.085), NEUTRAL)

def power_each(d, s):
    for ox, col in ((.30, NEUTRAL), (.62, ACCENT)):
        rline(d, [(f(s,ox+.06), f(s,.16)), (f(s,ox-.04), f(s,.50)),
                  (f(s,ox+.10), f(s,.50)), (f(s,ox), f(s,.84))], f(s,.055), col)

ICONS = {
    "sheetList": sheet_list, "viewList": view_list, "revisions": revisions,
    "publish": publish, "tableGen": table_gen, "copyCrop": copy_crop,
    "grids": grids, "levels": levels, "colorCoder": color_coder,
    "matchElev": match_elev, "paramPush": param_push, "connector": connector,
    "multiConnect": multi_connect, "splitPipe": split_pipe, "bloom": bloom,
    "reroute": reroute, "elbowDir": elbow_dir, "oneFilter": one_filter,
    "reOrdering": re_ordering, "familyExport": family_export,
    "viewManager": view_manager, "autoSectionBox": auto_section_box,
    "wipe": wipe, "syncViews": sync_views, "copyState": copy_state,
    "pasteState": paste_state, "match": match, "linkFamily": link_family,
    "linkVisibility": link_visibility, "netTree": net_tree,
    "powerEach": power_each, "aiConnector": ai_connector,
}


def gen(name, size):
    S = size * SS
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ICONS[name](ImageDraw.Draw(img), S)
    img = img.resize((size, size), Image.LANCZOS)
    img.save(os.path.join(OUT, f"{name}_{size}.png"))


if __name__ == "__main__":
    for name in ICONS:
        gen(name, 16)
        gen(name, 32)
    print(f"Generated {len(ICONS)*2} icons in {OUT}")
