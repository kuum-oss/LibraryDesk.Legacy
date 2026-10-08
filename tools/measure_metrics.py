#!/usr/bin/env python3
"""Small reproducible metric counter for the SR-2 C# training snapshot."""

from __future__ import annotations

import csv
import re
import sys
from pathlib import Path


TYPE_RE = re.compile(r"\b(?:class|record|interface|enum)\s+(\w+)")
METHOD_RE = re.compile(
    r"^\s*(?:public|private|protected|internal)\s+"
    r"(?:static\s+)?(?:async\s+)?[\w<>?,.\[\]]+\s+(\w+)\s*\((.*)\)"
)
METHOD_START_RE = re.compile(
    r"^\s*(?:public|private|protected|internal)\s+"
    r"(?:static\s+)?(?:async\s+)?[\w<>?,.\[\]]+\s+\w+\s*\("
)
CONTROL_RE = re.compile(r"\b(if|for|foreach|while|switch|catch|try)\b")
BRACE_ONLY_RE = re.compile(r"^[{};,]+$")


def strip_line(line: str) -> str:
    line = re.sub(r"//.*", "", line).strip()
    return line


def logical_lines(lines: list[str]) -> int:
    return sum(
        1
        for line in lines
        if (clean := strip_line(line)) and not BRACE_ONLY_RE.fullmatch(clean)
    )


def cc_of(lines: list[str]) -> int:
    text = "\n".join(strip_line(line) for line in lines)
    cc = 1
    cc += len(re.findall(r"\bif\s*\(", text))
    cc += len(re.findall(r"\b(?:for|foreach|while)\s*\(", text))
    cc += len(re.findall(r"\bcase\b|\bcatch\b", text))
    cc += text.count("&&") + text.count("||") + text.count("??")
    cc += len(re.findall(r"\?\.", text))
    cc += len(re.findall(r"(?<!\?)\?(?![?.])[^:\n]+:", text))
    return cc


def nesting_of(lines: list[str]) -> int:
    depth = 1
    maximum = 1
    pending_control = False
    control_depths: list[int] = []
    brace_depth = 0
    for raw in lines:
        line = strip_line(raw)
        if not line:
            continue
        if CONTROL_RE.search(line) and not line.startswith("else"):
            pending_control = True
        for char in line:
            if char == "{":
                brace_depth += 1
                if pending_control:
                    control_depths.append(brace_depth)
                    depth = 1 + len(control_depths)
                    maximum = max(maximum, depth)
                    pending_control = False
            elif char == "}":
                if control_depths and control_depths[-1] == brace_depth:
                    control_depths.pop()
                brace_depth -= 1
        if pending_control and ";" in line:
            maximum = max(maximum, 2 + len(control_depths))
            pending_control = False
    return maximum


def param_count(parameters: str) -> int:
    parameters = parameters.strip()
    return 0 if not parameters else parameters.count(",") + 1


def method_blocks(path: Path) -> list[dict[str, object]]:
    lines = path.read_text(encoding="utf-8").splitlines()
    results: list[dict[str, object]] = []
    index = 0
    while index < len(lines):
        if not METHOD_START_RE.match(lines[index]):
            index += 1
            continue
        signature = lines[index]
        while ")" not in signature and index + 1 < len(lines):
            index += 1
            signature += " " + lines[index].strip()
        signature_match = METHOD_RE.match(signature)
        if signature_match is None:
            index += 1
            continue
        name, parameters = signature_match.groups()
        previous_types = TYPE_RE.findall("\n".join(lines[: index + 1]))
        class_name = previous_types[-1] if previous_types else path.stem
        start = index
        brace = signature.count("{") - signature.count("}")
        while brace == 0 and index + 1 < len(lines):
            index += 1
            brace += lines[index].count("{") - lines[index].count("}")
        while brace > 0 and index + 1 < len(lines):
            index += 1
            brace += lines[index].count("{") - lines[index].count("}")
        body = lines[start + 1 : index]
        results.append(
            {
                "File": path.name,
                "Class": class_name,
                "Method": name,
                "LOC": logical_lines(body),
                "CC": cc_of(body),
                "Nesting": nesting_of(body),
                "Parameters": param_count(parameters),
            }
        )
        index += 1
    return results


def class_fanout(source_files: list[Path]) -> dict[str, int]:
    all_text = {path: path.read_text(encoding="utf-8") for path in source_files}
    type_names = sorted({name for text in all_text.values() for name in TYPE_RE.findall(text)})
    result: dict[str, int] = {}
    for path, text in all_text.items():
        own = TYPE_RE.findall(text)
        if not own:
            continue
        for class_name in own:
            result[class_name] = sum(
                1
                for candidate in type_names
                if candidate != class_name and re.search(rf"\b{candidate}\b", text)
            )
    return result


def clone_group(row: dict[str, object], fine_clone_exists: bool) -> str:
    key = (row["Class"], row["Method"])
    if fine_clone_exists and key in {
        ("LoanManager", "PreviewFine"),
        ("LoanRepository", "SumOutstandingFines"),
        ("LoanReport", "BuildOverdueReport"),
    }:
        return "CL-01"
    return ""


def main() -> None:
    if len(sys.argv) != 3:
        raise SystemExit("usage: measure_metrics.py SOURCE_DIR OUTPUT.csv")
    source_dir = Path(sys.argv[1])
    output = Path(sys.argv[2])
    source_files = sorted(source_dir.glob("*.cs"))
    fine_clone_exists = sum(
        "overdueDays *" in path.read_text(encoding="utf-8")
        for path in source_files
    ) >= 3
    fanout = class_fanout(source_files)
    rows = [row for path in source_files for row in method_blocks(path)]
    rows.sort(key=lambda row: (-int(row["CC"]), -int(row["LOC"]), str(row["Method"])))
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="") as stream:
        fields = ["File", "Class", "Method", "LOC", "CC", "Nesting", "Parameters", "FanOut", "CloneGroup"]
        writer = csv.DictWriter(stream, fieldnames=fields, delimiter=";")
        writer.writeheader()
        for row in rows:
            row["FanOut"] = fanout.get(str(row["Class"]), 0)
            row["CloneGroup"] = clone_group(row, fine_clone_exists)
            writer.writerow(row)


if __name__ == "__main__":
    main()
