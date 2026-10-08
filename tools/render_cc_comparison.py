#!/usr/bin/env python3
"""Render grouped before/after CC bars for the baseline top fifteen."""

from __future__ import annotations

import csv
import html
import sys
from pathlib import Path


def read(path: Path) -> list[dict[str, str]]:
    with path.open(encoding="utf-8") as stream:
        return list(csv.DictReader(stream, delimiter=";"))


def main() -> None:
    if len(sys.argv) != 4:
        raise SystemExit("usage: render_cc_comparison.py BEFORE.csv AFTER.csv OUTPUT.svg")
    before = read(Path(sys.argv[1]))[:15]
    after_rows = read(Path(sys.argv[2]))
    after = {(row["Class"], row["Method"]): int(row["CC"]) for row in after_rows}
    width, height = 1160, 720
    left, top, right, bottom = 270, 86, 50, 100
    plot_width, plot_height = width - left - right, height - top - bottom
    maximum = 28
    row_height = plot_height / len(before)
    chunks = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
        '<rect width="100%" height="100%" fill="white"/>',
        f'<text x="{width/2}" y="38" text-anchor="middle" font-family="Arial" font-size="24" font-weight="700">Цикломатична складність до і після рефакторингу</text>',
    ]
    for value in range(0, maximum + 1, 4):
        x = left + value / maximum * plot_width
        chunks.append(f'<line x1="{x:.1f}" y1="{top}" x2="{x:.1f}" y2="{height-bottom}" stroke="#e5e7eb"/>')
        chunks.append(f'<text x="{x:.1f}" y="{height-bottom+24}" text-anchor="middle" font-family="Arial" font-size="13">{value}</text>')
    threshold_x = left + 8 / maximum * plot_width
    chunks.append(f'<line x1="{threshold_x:.1f}" y1="{top}" x2="{threshold_x:.1f}" y2="{height-bottom}" stroke="#dc2626" stroke-width="3" stroke-dasharray="8 6"/>')
    chunks.append(f'<text x="{threshold_x+7:.1f}" y="{top+16}" font-family="Arial" font-size="13" fill="#dc2626">поріг 8</text>')
    for index, row in enumerate(before):
        key = (row["Class"], row["Method"])
        old, new = int(row["CC"]), after.get(key, 0)
        y = top + index * row_height + 3
        label = f'{row["Class"]}.{row["Method"]}'
        chunks.append(f'<text x="{left-10}" y="{y+row_height*0.55:.1f}" text-anchor="end" font-family="Arial" font-size="12">{html.escape(label)}</text>')
        chunks.append(f'<rect x="{left}" y="{y:.1f}" width="{old/maximum*plot_width:.1f}" height="{row_height*0.34:.1f}" rx="2" fill="#94a3b8"/>')
        chunks.append(f'<rect x="{left}" y="{y+row_height*0.39:.1f}" width="{new/maximum*plot_width:.1f}" height="{row_height*0.34:.1f}" rx="2" fill="#2563eb"/>')
        chunks.append(f'<text x="{left+old/maximum*plot_width+5:.1f}" y="{y+row_height*0.28:.1f}" font-family="Arial" font-size="11">{old}</text>')
        chunks.append(f'<text x="{left+new/maximum*plot_width+5:.1f}" y="{y+row_height*0.67:.1f}" font-family="Arial" font-size="11">{new}</text>')
    chunks.append(f'<rect x="{left}" y="{height-48}" width="18" height="10" fill="#94a3b8"/><text x="{left+26}" y="{height-39}" font-family="Arial" font-size="13">до</text>')
    chunks.append(f'<rect x="{left+90}" y="{height-48}" width="18" height="10" fill="#2563eb"/><text x="{left+116}" y="{height-39}" font-family="Arial" font-size="13">після</text>')
    chunks.append('</svg>')
    output = Path(sys.argv[3])
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(chunks), encoding="utf-8")


if __name__ == "__main__":
    main()

