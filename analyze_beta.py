from __future__ import annotations

import argparse
import math
import re
import statistics
import textwrap
import zipfile
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable
import xml.etree.ElementTree as ET

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt


MAIN_NS = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
PKG_REL_NS = "http://schemas.openxmlformats.org/package/2006/relationships"
DOC_REL_NS = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
NS = {"main": MAIN_NS, "pkgrel": PKG_REL_NS}


@dataclass
class TTKRecord:
    session_id: str
    enemy_name: str
    ttk_seconds: float
    kill_method: str
    damage_types_used: list[str]


@dataclass
class InteractionRecord:
    session_id: str
    interaction_index: int
    damage_taken: float
    duration_seconds: float
    end_reason: str


@dataclass
class RunRecord:
    session_id: str
    run_outcome: str
    total_kills: float
    kills_by_method: dict[str, float]


def excel_col_to_index(cell_ref: str) -> int:
    col = ""
    for char in cell_ref:
        if char.isalpha():
            col += char
        else:
            break
    value = 0
    for char in col:
        value = value * 26 + (ord(char.upper()) - 64)
    return value - 1


def load_shared_strings(zf: zipfile.ZipFile) -> list[str]:
    if "xl/sharedStrings.xml" not in zf.namelist():
        return []

    root = ET.fromstring(zf.read("xl/sharedStrings.xml"))
    strings: list[str] = []
    for si in root.findall("main:si", NS):
        strings.append("".join((t.text or "") for t in si.iterfind(".//main:t", NS)))
    return strings


def get_sheet_target(zf: zipfile.ZipFile, sheet_name: str) -> str:
    workbook_root = ET.fromstring(zf.read("xl/workbook.xml"))
    rels_root = ET.fromstring(zf.read("xl/_rels/workbook.xml.rels"))
    rel_map = {
        rel.attrib["Id"]: rel.attrib["Target"]
        for rel in rels_root.findall("pkgrel:Relationship", NS)
    }

    sheets = workbook_root.find("main:sheets", NS)
    if sheets is None:
        raise ValueError("Workbook does not contain any sheets.")

    for sheet in sheets:
        if sheet.attrib.get("name") != sheet_name:
            continue
        rel_id = sheet.attrib.get(f"{{{DOC_REL_NS}}}id")
        if not rel_id or rel_id not in rel_map:
            break
        target = rel_map[rel_id]
        return target if target.startswith("xl/") else f"xl/{target}"

    raise ValueError(f"Sheet '{sheet_name}' was not found in the workbook.")


def read_sheet_rows(xlsx_path: Path, sheet_name: str) -> list[dict[str, str]]:
    with zipfile.ZipFile(xlsx_path) as zf:
        shared_strings = load_shared_strings(zf)
        target = get_sheet_target(zf, sheet_name)
        sheet_root = ET.fromstring(zf.read(target))

    sheet_data = sheet_root.find("main:sheetData", NS)
    if sheet_data is None:
        return []

    rows: list[list[str]] = []
    max_index = 0
    for row in sheet_data.findall("main:row", NS):
        values_by_index: dict[int, str] = {}
        for cell in row.findall("main:c", NS):
            ref = cell.attrib.get("r", "")
            col_index = excel_col_to_index(ref)
            value = parse_cell_value(cell, shared_strings)
            values_by_index[col_index] = value
            max_index = max(max_index, col_index)

        if not values_by_index:
            continue

        row_values = [""] * (max_index + 1)
        for idx, value in values_by_index.items():
            row_values[idx] = value
        rows.append(row_values)

    if not rows:
        return []

    headers = rows[0]
    normalized_rows: list[dict[str, str]] = []
    for raw_row in rows[1:]:
        if len(raw_row) < len(headers):
            raw_row += [""] * (len(headers) - len(raw_row))
        row_dict = {headers[i]: raw_row[i] for i in range(len(headers))}
        if any(value.strip() for value in row_dict.values()):
            normalized_rows.append(row_dict)
    return normalized_rows


def parse_cell_value(cell: ET.Element, shared_strings: list[str]) -> str:
    cell_type = cell.attrib.get("t")
    if cell_type == "s":
        value_node = cell.find("main:v", NS)
        if value_node is None or value_node.text is None:
            return ""
        return shared_strings[int(value_node.text)]

    if cell_type == "inlineStr":
        return "".join((t.text or "") for t in cell.iterfind(".//main:t", NS))

    value_node = cell.find("main:v", NS)
    return "" if value_node is None or value_node.text is None else value_node.text


def to_float(value: str) -> float | None:
    if value is None:
        return None
    cleaned = value.strip()
    if not cleaned:
        return None
    try:
        return float(cleaned)
    except ValueError:
        return None


def to_int(value: str) -> int | None:
    as_float = to_float(value)
    if as_float is None:
        return None
    return int(as_float)


def split_damage_types(raw_value: str) -> list[str]:
    if not raw_value:
        return []
    return [part.strip() for part in raw_value.split(",") if part.strip()]


def normalize_enemy_name(enemy_name: str) -> str:
    cleaned = enemy_name.strip()
    cleaned = cleaned.replace("(Clone)", "").strip()
    cleaned = re.sub(r"\s*\(\d+\)\s*$", "", cleaned)
    return cleaned or "Unknown Enemy"


def parse_ttk_records(rows: Iterable[dict[str, str]]) -> list[TTKRecord]:
    records: list[TTKRecord] = []
    for row in rows:
        session_id = row.get("ttk_session_id", "").strip()
        enemy_name = normalize_enemy_name(row.get("ttk_enemy_name", ""))
        ttk_seconds = to_float(row.get("ttk_seconds", ""))
        if not session_id or not enemy_name or ttk_seconds is None:
            continue
        records.append(
            TTKRecord(
                session_id=session_id,
                enemy_name=enemy_name,
                ttk_seconds=ttk_seconds,
                kill_method=row.get("ttk_kill_method", "").strip() or "unknown",
                damage_types_used=split_damage_types(
                    row.get("ttk_damage_types_used", "").strip()
                ),
            )
        )
    return records


def parse_interaction_records(rows: Iterable[dict[str, str]]) -> list[InteractionRecord]:
    records: list[InteractionRecord] = []
    for row in rows:
        session_id = row.get("interaction_session_id", "").strip()
        interaction_index = to_int(row.get("interaction_index", ""))
        damage_taken = to_float(row.get("interaction_damage_taken", ""))
        duration_seconds = to_float(row.get("interaction_duration_seconds", ""))
        if (
            not session_id
            or interaction_index is None
            or damage_taken is None
            or duration_seconds is None
        ):
            continue
        records.append(
            InteractionRecord(
                session_id=session_id,
                interaction_index=interaction_index,
                damage_taken=damage_taken,
                duration_seconds=duration_seconds,
                end_reason=row.get("interaction_end_reason", "").strip() or "unknown",
            )
        )
    return records


def parse_run_records(rows: Iterable[dict[str, str]]) -> list[RunRecord]:
    method_columns = {
        "bullet": "bullet_kills",
        "slash": "slash_kills",
        "trap": "trap_kills",
        "dash": "dash_kills",
        "unknown": "unknown_kills",
    }
    records: list[RunRecord] = []
    for row in rows:
        session_id = row.get("session_id", "").strip()
        total_kills = to_float(row.get("total_kills", ""))
        run_outcome = row.get("run_outcome", "").strip()
        if not session_id or total_kills is None or not run_outcome:
            continue
        records.append(
            RunRecord(
                session_id=session_id,
                run_outcome=run_outcome,
                total_kills=total_kills,
                kills_by_method={
                    method: to_float(row.get(column, "")) or 0.0
                    for method, column in method_columns.items()
                },
            )
        )
    return records


def mean(values: Iterable[float]) -> float:
    seq = list(values)
    return statistics.fmean(seq) if seq else math.nan


def summarize_ttk(records: list[TTKRecord]) -> dict[str, object]:
    by_enemy: dict[str, list[float]] = defaultdict(list)
    by_method: dict[str, list[float]] = defaultdict(list)
    damage_types = Counter()
    kill_methods = Counter()

    for record in records:
        by_enemy[record.enemy_name].append(record.ttk_seconds)
        by_method[record.kill_method].append(record.ttk_seconds)
        kill_methods[record.kill_method] += 1
        for damage_type in record.damage_types_used:
            damage_types[damage_type] += 1

    per_enemy = {
        enemy: mean(times)
        for enemy, times in sorted(by_enemy.items(), key=lambda item: mean(item[1]), reverse=True)
    }
    per_method = {
        method: mean(times)
        for method, times in sorted(by_method.items(), key=lambda item: mean(item[1]), reverse=True)
    }
    return {
        "count": len(records),
        "overall_average": mean(record.ttk_seconds for record in records),
        "per_enemy_average": per_enemy,
        "per_method_average": per_method,
        "kill_method_counts": dict(kill_methods),
        "damage_type_counts": dict(damage_types),
    }


def summarize_interactions(records: list[InteractionRecord]) -> dict[str, object]:
    by_end_reason_damage: dict[str, list[float]] = defaultdict(list)
    by_end_reason_duration: dict[str, list[float]] = defaultdict(list)

    for record in records:
        by_end_reason_damage[record.end_reason].append(record.damage_taken)
        by_end_reason_duration[record.end_reason].append(record.duration_seconds)

    return {
        "count": len(records),
        "average_damage_taken": mean(record.damage_taken for record in records),
        "average_duration_seconds": mean(record.duration_seconds for record in records),
        "average_damage_by_end_reason": {
            reason: mean(values) for reason, values in sorted(by_end_reason_damage.items())
        },
        "average_duration_by_end_reason": {
            reason: mean(values) for reason, values in sorted(by_end_reason_duration.items())
        },
    }


def summarize_runs(records: list[RunRecord]) -> dict[str, object]:
    total_kills_by_method = Counter()
    kills_by_outcome: dict[str, Counter] = defaultdict(Counter)
    total_kills = 0.0

    for record in records:
        total_kills += record.total_kills
        for method, count in record.kills_by_method.items():
            total_kills_by_method[method] += count
            kills_by_outcome[record.run_outcome][method] += count

    distribution = {}
    for method, count in total_kills_by_method.items():
        percent = (count / total_kills * 100.0) if total_kills else 0.0
        distribution[method] = {"kills": count, "percent": percent}

    return {
        "count": len(records),
        "total_kills": total_kills,
        "average_total_kills_per_run": mean(record.total_kills for record in records),
        "distribution": distribution,
        "kills_by_outcome": {
            outcome: dict(counter) for outcome, counter in sorted(kills_by_outcome.items())
        },
    }


def save_ttk_chart(summary: dict[str, object], output_dir: Path) -> Path:
    per_enemy = summary["per_enemy_average"]
    chart_path = output_dir / "ttk_by_enemy.png"
    if not per_enemy:
        return chart_path

    labels = list(per_enemy.keys())
    values = [per_enemy[label] for label in labels]

    plt.figure(figsize=(10, 6))
    bars = plt.bar(labels, values, color="#2f6b7e", edgecolor="#1f1f1f", linewidth=1.0)
    plt.xticks(rotation=0, ha="center")
    plt.ylabel("Average TTK (seconds)")
    plt.title("Average Time To Kill Per Enemy")
    plt.grid(axis="y", linestyle="--", alpha=0.3)

    max_value = max(values) if values else 0
    for bar, value in zip(bars, values):
        plt.text(
            bar.get_x() + bar.get_width() / 2,
            value + max(max_value * 0.02, 0.02),
            f"{value:.2f}s",
            ha="center",
            va="bottom",
            fontsize=10,
        )

    plt.tight_layout()
    plt.savefig(chart_path, dpi=300, bbox_inches="tight")
    plt.close()
    return chart_path


def save_interaction_chart(records: list[InteractionRecord], output_dir: Path) -> Path:
    chart_path = output_dir / "interaction_damage_vs_duration.png"
    if not records:
        return chart_path

    colors = {"timeout": "#457b9d", "death": "#d62828", "unknown": "#6c757d"}
    plt.figure(figsize=(10, 6))
    for reason in sorted({record.end_reason for record in records}):
        subset = [record for record in records if record.end_reason == reason]
        plt.scatter(
            [record.duration_seconds for record in subset],
            [record.damage_taken for record in subset],
            label=reason,
            s=75,
            alpha=0.85,
            color=colors.get(reason, "#6c757d"),
            edgecolors="#1f1f1f",
            linewidths=0.5,
        )
    plt.xlabel("Interaction Duration (seconds)")
    plt.ylabel("Damage Taken")
    plt.title("Damage Taken Per Interaction")
    plt.legend()
    plt.grid(linestyle="--", alpha=0.3)
    plt.tight_layout()
    plt.savefig(chart_path, dpi=300, bbox_inches="tight")
    plt.close()
    return chart_path


def save_kill_method_chart(summary: dict[str, object], output_dir: Path) -> Path:
    chart_path = output_dir / "kill_methods_distribution.png"
    distribution = summary["distribution"]
    if not distribution:
        return chart_path

    labels = list(distribution.keys())
    values = [distribution[label]["kills"] for label in labels]
    percents = [distribution[label]["percent"] for label in labels]
    colors = ["#264653", "#2a9d8f", "#e9c46a", "#f4a261", "#e76f51"]

    plt.figure(figsize=(10, 6))
    bars = plt.bar(labels, values, color=colors[: len(labels)], edgecolor="#1f1f1f", linewidth=1.0)
    plt.ylabel("Total Kills")
    plt.title("End-Of-Run Kills By Method")
    plt.grid(axis="y", linestyle="--", alpha=0.3)

    max_value = max(values) if values else 0
    for bar, kill_count, percent in zip(bars, values, percents):
        plt.text(
            bar.get_x() + bar.get_width() / 2,
            kill_count + max(max_value * 0.02, 0.05),
            f"{kill_count:.0f} ({percent:.1f}%)",
            ha="center",
            va="bottom",
            fontsize=10,
        )

    plt.tight_layout()
    plt.savefig(chart_path, dpi=300, bbox_inches="tight")
    plt.close()
    return chart_path


def format_float(value: float) -> str:
    if value is None or math.isnan(value):
        return "n/a"
    return f"{value:.2f}"


def top_lines_from_mapping(mapping: dict[str, float], limit: int = 10) -> list[str]:
    items = list(mapping.items())[:limit]
    return [f"- {key}: {format_float(value)}" for key, value in items]


def render_summary_markdown(
    xlsx_path: Path,
    ttk_summary: dict[str, object],
    interaction_summary: dict[str, object],
    run_summary: dict[str, object],
    chart_paths: list[Path],
) -> str:
    ttk_damage_counts = ttk_summary["damage_type_counts"]
    ttk_method_counts = ttk_summary["kill_method_counts"]
    run_distribution = run_summary["distribution"]

    lines = [
        "# Beta Analytics Summary",
        "",
        f"Source workbook: `{xlsx_path.name}`",
        "",
        "## 1. Average Time To Kill Per Enemy",
        f"- Samples: {ttk_summary['count']}",
        f"- Overall average TTK: {format_float(ttk_summary['overall_average'])} s",
        "- Highest average-TTK enemies:",
        *top_lines_from_mapping(ttk_summary["per_enemy_average"]),
        "- Average TTK by final kill method:",
        *top_lines_from_mapping(ttk_summary["per_method_average"]),
        "- Kill method counts:",
        *[f"- {key}: {value}" for key, value in sorted(ttk_method_counts.items())],
        "- Damage type usage counts:",
        *[f"- {key}: {value}" for key, value in sorted(ttk_damage_counts.items())],
        "",
        "## 2. Average Damage Taken Per Interaction",
        f"- Samples: {interaction_summary['count']}",
        f"- Average damage taken: {format_float(interaction_summary['average_damage_taken'])}",
        f"- Average interaction duration: {format_float(interaction_summary['average_duration_seconds'])} s",
        "- Average damage by end reason:",
        *[
            f"- {reason}: {format_float(value)}"
            for reason, value in interaction_summary["average_damage_by_end_reason"].items()
        ],
        "- Average duration by end reason:",
        *[
            f"- {reason}: {format_float(value)} s"
            for reason, value in interaction_summary["average_duration_by_end_reason"].items()
        ],
        "",
        "## 3. End-Of-Run Kills By Method",
        f"- Runs: {run_summary['count']}",
        f"- Total kills logged: {format_float(run_summary['total_kills'])}",
        f"- Average kills per run: {format_float(run_summary['average_total_kills_per_run'])}",
        "- Kill distribution:",
        *[
            f"- {method}: {format_float(values['kills'])} kills ({format_float(values['percent'])}%)"
            for method, values in sorted(run_distribution.items())
        ],
        "- Kills by run outcome:",
    ]

    for outcome, counts in run_summary["kills_by_outcome"].items():
        joined = ", ".join(f"{method}={format_float(count)}" for method, count in sorted(counts.items()))
        lines.append(f"- {outcome}: {joined}")

    lines.extend(["", "## Output Files"])
    for chart_path in chart_paths:
        lines.append(f"- `{chart_path.name}`")

    return "\n".join(lines) + "\n"


def build_outputs(xlsx_path: Path, sheet_name: str, output_dir: Path) -> None:
    rows = read_sheet_rows(xlsx_path, sheet_name)
    ttk_records = parse_ttk_records(rows)
    interaction_records = parse_interaction_records(rows)
    run_records = parse_run_records(rows)

    ttk_summary = summarize_ttk(ttk_records)
    interaction_summary = summarize_interactions(interaction_records)
    run_summary = summarize_runs(run_records)

    output_dir.mkdir(parents=True, exist_ok=True)
    chart_paths = [
        save_ttk_chart(ttk_summary, output_dir),
        save_interaction_chart(interaction_records, output_dir),
        save_kill_method_chart(run_summary, output_dir),
    ]

    summary_text = render_summary_markdown(
        xlsx_path=xlsx_path,
        ttk_summary=ttk_summary,
        interaction_summary=interaction_summary,
        run_summary=run_summary,
        chart_paths=chart_paths,
    )
    summary_path = output_dir / "beta_analysis_summary.md"
    summary_path.write_text(summary_text, encoding="utf-8")

    print(textwrap.dedent(
        f"""\
        Analysis complete.
        - TTK records: {len(ttk_records)}
        - Interaction records: {len(interaction_records)}
        - Run records: {len(run_records)}
        - Output directory: {output_dir}
        - Summary: {summary_path.name}
        """
    ).strip())


def main() -> None:
    parser = argparse.ArgumentParser(description="Analyze Alter Ego beta telemetry workbook.")
    parser.add_argument(
        "xlsx_path",
        nargs="?",
        default="alter-ego beta.xlsx",
        help="Path to the telemetry workbook (.xlsx).",
    )
    parser.add_argument(
        "--sheet",
        default="Form Responses 1",
        help="Sheet name to analyze.",
    )
    parser.add_argument(
        "--output-dir",
        default="analysis_output",
        help="Directory where charts and markdown summary will be written.",
    )
    args = parser.parse_args()

    xlsx_path = Path(args.xlsx_path).resolve()
    if not xlsx_path.exists():
        raise FileNotFoundError(f"Workbook not found: {xlsx_path}")

    output_dir = Path(args.output_dir).resolve()
    build_outputs(xlsx_path=xlsx_path, sheet_name=args.sheet, output_dir=output_dir)


if __name__ == "__main__":
    main()
