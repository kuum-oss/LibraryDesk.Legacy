#!/usr/bin/env python3
"""Render an SVG bar chart from the semicolon-separated metric table."""

from __future__ import annotations

import csv
import html
import sys
from pathlib import Path


def main() -> None:
    if len(sys.argv) != 4:
        raise SystemExit("usage: render_cc_chart.py INPUT.csv OUTPUT.svg TITLE")
    source, output, title = Path(sys.argv[1]), Path(sys.argv[2]), sys.argv[3]
    with source.open(encoding="utf-8") as stream:
        rows = list(csv.DictReader(stream, delimiter=";"))[:15]
    width, height = 1120, 660
    left, top, right, bottom = 260, 80, 45, 90
    plot_width, plot_height = width - left - right, height - top - bottom
    maximum = max(28, max(int(row["CC"]) for row in rows) + 2)
    row_height = plot_height / len(rows)
    chunks = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">',
        '<rect width="100%" height="100%" fill="white"/>',
        f'<text x="{width / 2}" y="38" text-anchor="middle" font-family="Arial" font-size="24" font-weight="700">{html.escape(title)}</text>',
        f'<line x1="{left}" y1="{top}" x2="{left}" y2="{height-bottom}" stroke="#1f2937"/>',
        f'<line x1="{left}" y1="{height-bottom}" x2="{width-right}" y2="{height-bottom}" stroke="#1f2937"/>',
    ]
    for value in range(0, maximum + 1, 4):
        x = left + value / maximum * plot_width
        chunks.append(f'<line x1="{x:.1f}" y1="{top}" x2="{x:.1f}" y2="{height-bottom}" stroke="#e5e7eb"/>')
        chunks.append(f'<text x="{x:.1f}" y="{height-bottom+25}" text-anchor="middle" font-family="Arial" font-size="13">{value}</text>')
    threshold_x = left + 8 / maximum * plot_width
    chunks.append(f'<line x1="{threshold_x:.1f}" y1="{top}" x2="{threshold_x:.1f}" y2="{height-bottom}" stroke="#dc2626" stroke-width="3" stroke-dasharray="8 6"/>')
    chunks.append(f'<text x="{threshold_x+8:.1f}" y="{top+18}" font-family="Arial" font-size="14" fill="#dc2626">поріг CC = 8</text>')
    for index, row in enumerate(rows):
        value = int(row["CC"])
        y = top + index * row_height + row_height * 0.18
        bar_height = row_height * 0.64
        bar_width = value / maximum * plot_width
        label = f'{row["Class"]}.{row["Method"]}'
        color = "#dc2626" if value > 8 else "#2563eb"
        chunks.append(f'<text x="{left-10}" y="{y+bar_height*0.72:.1f}" text-anchor="end" font-family="Arial" font-size="13">{html.escape(label)}</text>')
        chunks.append(f'<rect x="{left}" y="{y:.1f}" width="{bar_width:.1f}" height="{bar_height:.1f}" rx="3" fill="{color}"/>')
        chunks.append(f'<text x="{left+bar_width+8:.1f}" y="{y+bar_height*0.72:.1f}" font-family="Arial" font-size="13" font-weight="700">{value}</text>')
    chunks.append(f'<text x="{left+plot_width/2}" y="{height-24}" text-anchor="middle" font-family="Arial" font-size="16">Цикломатична складність (CC)</text>')
    chunks.append('</svg>')
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(chunks), encoding="utf-8")


if __name__ == "__main__":
    main()

