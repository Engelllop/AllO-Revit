"""Generate 16×16 and 32×32 PNG ribbon icons for AllO."""
from PIL import Image, ImageDraw
import os

BG = (250, 249, 245)      # #FAF9F5
FG = (20, 20, 19)          # #141413
OUT = os.path.dirname(os.path.abspath(__file__))

def new(size):
    img = Image.new("RGBA", (size, size), (*BG, 255))
    return img, ImageDraw.Draw(img)

def save(img, name, size):
    img.save(os.path.join(OUT, f"{name}_{size}.png"))

def scale(size, f):
    return round(size * f)

# ------------------------------------------------------------------
def draw_sheet_list(draw, s):
    margin = scale(s, 0.1875)
    x1, y1 = margin, margin
    x2, y2 = s - margin, s - margin
    draw.rectangle([x1, y1, x2, y2], outline=FG, width=max(1, s // 16))
    for i in range(1, 4):
        y = y1 + (y2 - y1) * i // 4
        draw.line([(x1 + 2, y), (x2 - 2, y)], fill=FG, width=max(1, s // 16))

def draw_view_list(draw, s):
    margin = scale(s, 0.1875)
    x1, y1 = margin, margin
    x2, y2 = s - margin, s - margin
    draw.rectangle([x1, y1, x2, y2], outline=FG, width=max(1, s // 16))
    draw.line([(x1 + 2, y1 + (y2-y1)//3), (x2 - 2, y1 + (y2-y1)//3)], fill=FG, width=max(1, s // 16))
    draw.line([(x1 + 2, y1 + 2*(y2-y1)//3), (x2 - 2, y1 + 2*(y2-y1)//3)], fill=FG, width=max(1, s // 16))

def draw_revisions(draw, s):
    cx, cy = s // 2, s // 2
    r = s // 3
    draw.ellipse([cx-r, cy-r, cx+r, cy+r], outline=FG, width=max(1, s // 16))
    # checkmark
    p1 = (cx - r//2, cy)
    p2 = (cx - r//4, cy + r//2)
    p3 = (cx + r//2, cy - r//3)
    draw.line([p1, p2, p3], fill=FG, width=max(1, s // 12))

def draw_publish(draw, s):
    cx = s // 2
    base_y = s - scale(s, 0.25)
    top_y = scale(s, 0.2)
    draw.polygon([(cx, top_y), (cx - scale(s, 0.15), top_y + scale(s, 0.2)),
                  (cx - scale(s, 0.05), top_y + scale(s, 0.2)),
                  (cx - scale(s, 0.05), base_y),
                  (cx + scale(s, 0.05), base_y),
                  (cx + scale(s, 0.05), top_y + scale(s, 0.2)),
                  (cx + scale(s, 0.15), top_y + scale(s, 0.2))], fill=FG)
    draw.line([(cx - scale(s, 0.2), base_y), (cx + scale(s, 0.2), base_y)], fill=FG, width=max(1, s // 16))

def draw_table_gen(draw, s):
    m = scale(s, 0.2)
    step = (s - 2*m) // 3
    for i in range(4):
        x = m + i*step
        draw.line([(x, m), (x, s-m)], fill=FG, width=max(1, s // 16))
    for j in range(4):
        y = m + j*step
        draw.line([(m, y), (s-m, y)], fill=FG, width=max(1, s // 16))

def draw_copy_crop(draw, s):
    m = scale(s, 0.18)
    draw.rectangle([m, m, s - m, s - m], outline=FG, width=max(1, s // 16))
    m2 = scale(s, 0.30)
    draw.rectangle([m2, m2, s - m2 + 1, s - m2 + 1], outline=FG, width=max(1, s // 16))

def draw_grids(draw, s):
    m = scale(s, 0.15)
    draw.line([(s//2, m), (s//2, s-m)], fill=FG, width=max(1, s // 16))
    draw.line([(m, s//2), (s-m, s//2)], fill=FG, width=max(1, s // 16))
    draw.line([(s//4, m), (s//4, s-m)], fill=FG, width=max(1, s // 16))
    draw.line([(3*s//4, m), (3*s//4, s-m)], fill=FG, width=max(1, s // 16))

def draw_levels(draw, s):
    m = scale(s, 0.2)
    for i, frac in enumerate([0.25, 0.5, 0.75]):
        y = m + int((s - 2*m) * frac)
        draw.line([(m, y), (s-m, y)], fill=FG, width=max(1, s // 16))
        # small tick
        draw.line([(m, y-1), (m, y+1)], fill=FG, width=max(1, s // 16))
        draw.line([(s-m, y-1), (s-m, y+1)], fill=FG, width=max(1, s // 16))

def draw_color_coder(draw, s):
    cx, cy = s // 2, s // 2
    r = s // 3
    draw.pieslice([cx-r, cy-r, cx+r, cy+r], start=0, end=120, fill=FG)
    draw.pieslice([cx-r, cy-r, cx+r, cy+r], start=120, end=240, outline=FG, width=max(1, s // 16))
    draw.pieslice([cx-r, cy-r, cx+r, cy+r], start=240, end=360, outline=FG, width=max(1, s // 16))

def draw_match_elev(draw, s):
    m = scale(s, 0.25)
    draw.line([(m, m), (s-m, m)], fill=FG, width=max(1, s // 16))
    draw.line([(m, s-m), (s-m, s-m)], fill=FG, width=max(1, s // 16))
    cx = s // 2
    draw.line([(cx, m+2), (cx, s-m-2)], fill=FG, width=max(1, s // 16))
    # arrow heads
    draw.polygon([(cx, m), (cx-2, m+3), (cx+2, m+3)], fill=FG)
    draw.polygon([(cx, s-m), (cx-2, s-m-3), (cx+2, s-m-3)], fill=FG)

def draw_param_push(draw, s):
    m = scale(s, 0.2)
    # box
    draw.rectangle([m, s//2, s-m, s-m], outline=FG, width=max(1, s // 16))
    # arrow pointing down
    cx = s // 2
    draw.line([(cx, m), (cx, s//2 - 1)], fill=FG, width=max(1, s // 16))
    draw.polygon([(cx, s//2 - 1), (cx-2, s//2 - 4), (cx+2, s//2 - 4)], fill=FG)

def draw_connector(draw, s):
    m = scale(s, 0.2)
    cy = s // 2
    draw.line([(m, cy), (s-m, cy)], fill=FG, width=max(1, s // 16))
    r = max(2, s // 8)
    draw.ellipse([s//2 - r, cy - r, s//2 + r, cy + r], outline=FG, width=max(1, s // 16))

def draw_multi_connect(draw, s):
    m = scale(s, 0.2)
    cy = s // 2
    # trunk
    draw.line([(m, cy), (s//2, cy)], fill=FG, width=max(1, s // 16))
    # branches
    draw.line([(s//2, cy), (s-m, m)], fill=FG, width=max(1, s // 16))
    draw.line([(s//2, cy), (s-m, s-m)], fill=FG, width=max(1, s // 16))

def draw_split_pipe(draw, s):
    m = scale(s, 0.2)
    cy = s // 2
    # trunk from top
    draw.line([(s//2, m), (s//2, cy)], fill=FG, width=max(1, s // 16))
    # branches
    draw.line([(s//2, cy), (m, s-m)], fill=FG, width=max(1, s // 16))
    draw.line([(s//2, cy), (s-m, s-m)], fill=FG, width=max(1, s // 16))

def draw_bloom(draw, s):
    cx, cy = s // 2, s // 2
    r = max(1, s // 5)
    # center
    draw.ellipse([cx-r, cy-r, cx+r, cy+r], fill=FG)
    # petals
    for dx, dy in [(0, -1), (0, 1), (-1, 0), (1, 0)]:
        px, py = cx + dx*2*r, cy + dy*2*r
        pr = max(1, s // 8)
        draw.ellipse([px-pr, py-pr, px+pr, py+pr], outline=FG, width=max(1, s // 16))

def draw_reroute(draw, s):
    m = scale(s, 0.2)
    # line that goes down, right, up
    pts = [(m, m), (m, s-m), (s-m, s-m), (s-m, m+scale(s,0.1))]
    draw.line(pts, fill=FG, width=max(1, s // 16))
    # arrow at end
    draw.polygon([(s-m, m), (s-m-3, m+3), (s-m+3, m+3)], fill=FG)

def draw_elbow_dir(draw, s):
    m = scale(s, 0.2)
    # L shape
    draw.line([(m, m), (m, s-m), (s-m, s-m)], fill=FG, width=max(1, s // 16))
    # arrow at end
    draw.polygon([(s-m, s-m), (s-m-3, s-m-3), (s-m-3, s-m+3)], fill=FG)

def draw_one_filter(draw, s):
    m = scale(s, 0.2)
    top_y = m
    mid_y = s//2 + scale(s,0.05)
    bot_y = s - m
    draw.polygon([(s//2, top_y), (m, mid_y), (s-m, mid_y)], outline=FG, width=max(1, s // 16))
    draw.line([(s//2, mid_y), (s//2, bot_y)], fill=FG, width=max(1, s // 16))
    draw.line([(s//2 - scale(s,0.05), bot_y), (s//2 + scale(s,0.05), bot_y)], fill=FG, width=max(1, s // 16))

def draw_re_ordering(draw, s):
    cx = s // 2
    m = scale(s, 0.2)
    # up arrow
    draw.line([(cx, s-m), (cx, m+4)], fill=FG, width=max(1, s // 16))
    draw.polygon([(cx, m), (cx-2, m+3), (cx+2, m+3)], fill=FG)
    # down arrow
    draw.line([(cx+scale(s,0.12), m), (cx+scale(s,0.12), s-m-4)], fill=FG, width=max(1, s // 16))
    draw.polygon([(cx+scale(s,0.12), s-m), (cx+scale(s,0.12)-2, s-m-3), (cx+scale(s,0.12)+2, s-m-3)], fill=FG)

def draw_family_export(draw, s):
    m = scale(s, 0.2)
    # box
    draw.rectangle([m, m, s-m, s-m], outline=FG, width=max(1, s // 16))
    # arrow out
    cx = s // 2
    draw.line([(cx, m+2), (cx, m-scale(s,0.05))], fill=FG, width=max(1, s // 16))
    draw.polygon([(cx, m-scale(s,0.05)), (cx-2, m+1), (cx+2, m+1)], fill=FG)

def draw_view_manager(draw, s):
    cx, cy = s // 2, s // 2
    r = s // 3
    # eye shape
    draw.arc([cx-r, cy-r//2, cx+r, cy+r//2], start=0, end=180, fill=FG, width=max(1, s // 16))
    draw.arc([cx-r, cy-r//2, cx+r, cy+r//2], start=180, end=360, fill=FG, width=max(1, s // 16))
    draw.ellipse([cx-r//3, cy-r//3, cx+r//3, cy+r//3], outline=FG, width=max(1, s // 16))

def draw_wipe(draw, s):
    m = scale(s, 0.2)
    # broom handle
    draw.line([(s-m, m), (m, s-m)], fill=FG, width=max(1, s // 12))
    # bristles
    draw.line([(m, s-m), (m-scale(s,0.05), s-m+scale(s,0.05))], fill=FG, width=max(1, s // 16))
    draw.line([(m, s-m), (m+scale(s,0.05), s-m+scale(s,0.05))], fill=FG, width=max(1, s // 16))

def draw_sync_views(draw, s):
    m = scale(s, 0.15)
    w = (s - 2*m) // 2 - 1
    h = s - 2*m
    # left rect
    draw.rectangle([m, m, m+w, m+h], outline=FG, width=max(1, s // 16))
    # right rect
    draw.rectangle([m+w+2, m, m+2*w+2, m+h], outline=FG, width=max(1, s // 16))
    # arrow between
    cx = m + w + 1
    draw.line([(cx, m+h//3), (cx, m+2*h//3)], fill=FG, width=max(1, s // 16))
    draw.polygon([(cx, m+h//3), (cx-2, m+h//3+2), (cx+2, m+h//3+2)], fill=FG)
    draw.polygon([(cx, m+2*h//3), (cx-2, m+2*h//3-2), (cx+2, m+2*h//3-2)], fill=FG)

def draw_copy_state(draw, s):
    m = scale(s, 0.2)
    draw.rectangle([m, m, s-m-2, s-m-2], outline=FG, width=max(1, s // 16))
    draw.rectangle([m+3, m+3, s-m+1, s-m+1], outline=FG, width=max(1, s // 16))

def draw_paste_state(draw, s):
    m = scale(s, 0.2)
    # clipboard
    draw.rectangle([m, m+2, s-m, s-m], outline=FG, width=max(1, s // 16))
    # clip
    draw.rectangle([s//2 - scale(s,0.06), m, s//2 + scale(s,0.06), m+4], outline=FG, width=max(1, s // 16))
    # lines
    draw.line([(m+2, m+6), (s-m-2, m+6)], fill=FG, width=max(1, s // 16))
    draw.line([(m+2, m+9), (s-m-2, m+9)], fill=FG, width=max(1, s // 16))

def draw_match(draw, s):
    m = scale(s, 0.25)
    cy = s // 2
    draw.line([(m, cy-1), (s-m, cy-1)], fill=FG, width=max(1, s // 12))
    draw.line([(m, cy+1), (s-m, cy+1)], fill=FG, width=max(1, s // 12))

def draw_auto_section_box(draw, s):
    m = scale(s, 0.22)
    d = scale(s, 0.12)  # depth offset (perspectiva)
    w = max(1, s // 16)
    fx1, fy1, fx2, fy2 = m, m + d, s - m - d, s - m          # cara frontal
    bx1, by1, bx2, by2 = m + d, m, s - m, s - m - d          # cara trasera
    draw.rectangle([fx1, fy1, fx2, fy2], outline=FG, width=w)
    draw.rectangle([bx1, by1, bx2, by2], outline=FG, width=w)
    draw.line([(fx1, fy1), (bx1, by1)], fill=FG, width=w)
    draw.line([(fx2, fy1), (bx2, by1)], fill=FG, width=w)
    draw.line([(fx1, fy2), (bx1, by2)], fill=FG, width=w)
    draw.line([(fx2, fy2), (bx2, by2)], fill=FG, width=w)

def draw_link_family(draw, s):
    m = scale(s, 0.25)
    cy = s // 2
    # two chain links (ovals)
    draw.ellipse([m, cy-scale(s,0.08), m+scale(s,0.15), cy+scale(s,0.08)], outline=FG, width=max(1, s // 16))
    draw.ellipse([s-m-scale(s,0.15), cy-scale(s,0.08), s-m, cy+scale(s,0.08)], outline=FG, width=max(1, s // 16))

def draw_link_visibility(draw, s):
    cx, cy = s // 2, s // 2
    r = s // 3
    draw.arc([cx-r, cy-r//2, cx+r, cy+r//2], start=0, end=180, fill=FG, width=max(1, s // 16))
    draw.arc([cx-r, cy-r//2, cx+r, cy+r//2], start=180, end=360, fill=FG, width=max(1, s // 16))
    draw.ellipse([cx-r//3, cy-r//3, cx+r//3, cy+r//3], outline=FG, width=max(1, s // 16))
    # link line under
    draw.line([(cx-r//2, cy+r//2+1), (cx+r//2, cy+r//2+1)], fill=FG, width=max(1, s // 16))

def draw_net_tree(draw, s):
    m = scale(s, 0.2)
    w = max(1, s // 16)
    trunk_x = m + scale(s, 0.1)
    # trunk
    draw.line([(trunk_x, m), (trunk_x, s - m)], fill=FG, width=w)
    # branches
    y1 = m + scale(s, 0.15)
    y2 = s // 2
    y3 = s - m - scale(s, 0.1)
    bx = s - m
    draw.line([(trunk_x, y1), (bx, y1)], fill=FG, width=w)
    draw.line([(trunk_x, y2), (bx - scale(s, 0.12), y2)], fill=FG, width=w)
    draw.line([(trunk_x, y3), (bx, y3)], fill=FG, width=w)
    # leaf dots
    r = max(1, scale(s, 0.06))
    for (x, y) in [(bx, y1), (bx - scale(s, 0.12), y2), (bx, y3)]:
        draw.ellipse([x - r, y - r, x + r, y + r], fill=FG)

# ------------------------------------------------------------------
ICONS = {
    "sheetList": draw_sheet_list,
    "viewList": draw_view_list,
    "revisions": draw_revisions,
    "publish": draw_publish,
    "tableGen": draw_table_gen,
    "copyCrop": draw_copy_crop,
    "grids": draw_grids,
    "levels": draw_levels,
    "colorCoder": draw_color_coder,
    "matchElev": draw_match_elev,
    "paramPush": draw_param_push,
    "connector": draw_connector,
    "multiConnect": draw_multi_connect,
    "splitPipe": draw_split_pipe,
    "bloom": draw_bloom,
    "reroute": draw_reroute,
    "elbowDir": draw_elbow_dir,
    "oneFilter": draw_one_filter,
    "reOrdering": draw_re_ordering,
    "familyExport": draw_family_export,
    "viewManager": draw_view_manager,
    "autoSectionBox": draw_auto_section_box,
    "wipe": draw_wipe,
    "syncViews": draw_sync_views,
    "copyState": draw_copy_state,
    "pasteState": draw_paste_state,
    "match": draw_match,
    "linkFamily": draw_link_family,
    "linkVisibility": draw_link_visibility,
    "netTree": draw_net_tree,
}

def gen(name, size):
    img, draw = new(size)
    ICONS[name](draw, size)
    save(img, name, size)

if __name__ == "__main__":
    for name in ICONS:
        gen(name, 16)
        gen(name, 32)
    print(f"Generated {len(ICONS)*2} icons in {OUT}")
