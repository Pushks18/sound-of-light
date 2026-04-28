"""
Generate Gold analytics graphs from the public Google Sheet CSV export.

Usage:
    python docs/gold_analytics/make_gold_analytics.py --fetch
    python docs/gold_analytics/make_gold_analytics.py --csv gold_feedback_sheet_gid1551200349.csv
"""
from __future__ import annotations

import argparse
import csv
import json
import math
import statistics
import urllib.request
from collections import Counter, defaultdict
from datetime import datetime
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
import numpy as np


ROOT = Path(__file__).resolve().parents[2]
OUT_DIR = Path(__file__).resolve().parent
DEFAULT_CSV = ROOT / "gold_feedback_sheet_gid1551200349.csv"
SHEET_CSV_URL = (
    "https://docs.google.com/spreadsheets/d/"
    "1tixu10taawIMOLwd5kgXZ6ln1-OHcisn5rEYA8zM2R4"
    "/export?format=csv&gid=1551200349"
)

DARK_BG = "#17181d"
PANEL = "#23242b"
FG = "#ececf2"
GRID = "#3b3d48"
SLASH = "#ffd24a"
BULLET = "#89bfff"
DASH = "#7df0b0"
TRAP = "#ff8a8a"
UNKNOWN = "#a8a8b8"
ACCENT = "#ff7070"
NEUTRAL = "#d3d5de"

plt.rcParams.update(
    {
        "figure.facecolor": DARK_BG,
        "axes.facecolor": PANEL,
        "axes.edgecolor": FG,
        "axes.labelcolor": FG,
        "axes.titlecolor": FG,
        "xtick.color": FG,
        "ytick.color": FG,
        "text.color": FG,
        "axes.grid": True,
        "grid.color": GRID,
        "grid.alpha": 0.55,
        "axes.spines.top": False,
        "axes.spines.right": False,
        "font.family": "DejaVu Sans",
        "font.size": 10,
    }
)


def fetch_csv(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with urllib.request.urlopen(SHEET_CSV_URL, timeout=30) as resp:
        path.write_bytes(resp.read())


def unique_headers(headers: list[str]) -> list[str]:
    seen: Counter[str] = Counter()
    out: list[str] = []
    for header in headers:
        key = header.strip() or "blank"
        seen[key] += 1
        out.append(key if seen[key] == 1 else f"{key}_{seen[key]}")
    return out


def load_rows(path: Path) -> list[dict[str, str]]:
    with path.open(newline="", encoding="utf-8-sig") as f:
        reader = csv.reader(f)
        headers = unique_headers(next(reader))
        rows = []
        for row in reader:
            row = row + [""] * (len(headers) - len(row))
            rows.append(dict(zip(headers, row)))
    return rows


def parse_time(value: str) -> datetime | None:
    value = (value or "").strip()
    if not value:
        return None
    for fmt in ("%m/%d/%Y %H:%M:%S", "%m/%d/%Y %H:%M"):
        try:
            return datetime.strptime(value, fmt)
        except ValueError:
            pass
    return None


def num(value: str | int | float | None) -> float | None:
    if value is None:
        return None
    if isinstance(value, (int, float)):
        return float(value)
    value = value.strip()
    if not value:
        return None
    try:
        return float(value)
    except ValueError:
        return None


def pct(part: float, whole: float) -> float:
    return (part / whole * 100.0) if whole else 0.0


def med(values: list[float]) -> float | None:
    return statistics.median(values) if values else None


def mean(values: list[float]) -> float | None:
    return statistics.mean(values) if values else None


def clean_method(value: str) -> str:
    value = (value or "").strip().lower()
    return value if value in {"slash", "bullet", "dash", "trap"} else "unknown"


def save(fig: plt.Figure, name: str) -> str:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    out = OUT_DIR / name
    fig.savefig(out, dpi=150, bbox_inches="tight", facecolor=DARK_BG)
    plt.close(fig)
    return out.name


def graph_combat_tool_imbalance(rows: list[dict[str, str]], summary: dict) -> None:
    run_rows = [r for r in rows if r.get("run_outcome", "").strip()]
    methods = ["slash", "bullet", "dash", "trap", "unknown"]
    colors = [SLASH, BULLET, DASH, TRAP, UNKNOWN]
    kill_counts = Counter()
    nonzero_runs = 0
    pure_slash = zero_bullet = zero_dash = zero_trap = 0

    for row in run_rows:
        counts = {
            "slash": int(num(row.get("slash_kills")) or 0),
            "bullet": int(num(row.get("bullet_kills")) or 0),
            "dash": int(num(row.get("dash_kills")) or 0),
            "trap": int(num(row.get("trap_kills")) or 0),
            "unknown": int(num(row.get("unknown_kills")) or 0),
        }
        kill_counts.update(counts)
        total = sum(counts[m] for m in ["slash", "bullet", "dash", "trap"])
        if total:
            nonzero_runs += 1
            pure_slash += counts["slash"] == total
            zero_bullet += counts["bullet"] == 0
            zero_dash += counts["dash"] == 0
            zero_trap += counts["trap"] == 0

    ttk_by_method: dict[str, list[float]] = defaultdict(list)
    for row in rows:
        seconds = num(row.get("ttk_seconds"))
        if seconds is None:
            continue
        enemy = row.get("ttk_enemy_name", "")
        if "Clone" in enemy:
            continue
        ttk_by_method[clean_method(row.get("ttk_kill_method", ""))].append(seconds)

    fig, (ax1, ax2, ax3) = plt.subplots(
        1, 3, figsize=(14, 5), gridspec_kw={"width_ratios": [1, 1.2, 1.1]}
    )

    total_kills = sum(kill_counts.values())
    shares = [pct(kill_counts[m], total_kills) for m in methods]
    bars = ax1.bar(methods, shares, color=colors, edgecolor=DARK_BG)
    ax1.set_ylim(0, 100)
    ax1.set_ylabel("share of run-summary kills (%)")
    ax1.set_title("Kill method mix")
    for bar, share in zip(bars, shares):
        ax1.text(bar.get_x() + bar.get_width() / 2, share + 2, f"{share:.0f}%", ha="center")

    median_methods = [m for m in ["slash", "bullet", "dash", "trap"] if ttk_by_method[m]]
    medians = [med(ttk_by_method[m]) for m in median_methods]
    means = [mean(ttk_by_method[m]) for m in median_methods]
    x = np.arange(len(median_methods))
    ax2.bar(x - 0.18, medians, 0.36, color=[SLASH, BULLET, DASH, TRAP][: len(x)], label="median")
    ax2.bar(x + 0.18, means, 0.36, color=NEUTRAL, label="mean")
    ax2.set_xticks(x)
    ax2.set_xticklabels(median_methods)
    ax2.set_ylabel("seconds")
    ax2.set_title("TTK by method")
    ax2.legend(frameon=False)

    collapse_cats = ["pure slash", "no bullet", "no dash", "no trap"]
    collapse_vals = [
        pct(pure_slash, nonzero_runs),
        pct(zero_bullet, nonzero_runs),
        pct(zero_dash, nonzero_runs),
        pct(zero_trap, nonzero_runs),
    ]
    bars = ax3.bar(collapse_cats, collapse_vals, color=[SLASH, BULLET, DASH, TRAP], edgecolor=DARK_BG)
    ax3.set_ylim(0, 100)
    ax3.set_ylabel("% of nonzero-kill runs")
    ax3.set_title("Resource use collapse")
    ax3.tick_params(axis="x", rotation=20)
    for bar, value in zip(bars, collapse_vals):
        ax3.text(bar.get_x() + bar.get_width() / 2, value + 2, f"{value:.0f}%", ha="center")

    fig.suptitle("Gold Issue 1: Slash dominates combat and suppresses the rest of the kit", fontsize=13)
    fig.tight_layout()
    file_name = save(fig, "01_gold_combat_tool_imbalance.png")

    summary["combat_tool_imbalance"] = {
        "graph": file_name,
        "run_rows": len(run_rows),
        "nonzero_kill_runs": nonzero_runs,
        "run_summary_kills": dict(kill_counts),
        "slash_share_percent": pct(kill_counts["slash"], total_kills),
        "zero_bullet_runs_percent": pct(zero_bullet, nonzero_runs),
        "zero_dash_runs_percent": pct(zero_dash, nonzero_runs),
        "pure_slash_runs_percent": pct(pure_slash, nonzero_runs),
        "ttk_median_seconds": {m: med(ttk_by_method[m]) for m in median_methods},
        "ttk_mean_seconds": {m: mean(ttk_by_method[m]) for m in median_methods},
        "ttk_counts": {m: len(ttk_by_method[m]) for m in median_methods},
    }


def graph_damage_spikes(rows: list[dict[str, str]], summary: dict) -> None:
    fatal: list[float] = []
    survived: list[float] = []
    durations: list[float] = []
    for row in rows:
        damage = num(row.get("interaction_damage_taken"))
        if damage is None:
            continue
        durations.append(num(row.get("interaction_duration_seconds")) or 0.0)
        if row.get("interaction_end_reason", "").strip() == "death":
            fatal.append(damage)
        else:
            survived.append(damage)

    run_rows = [r for r in rows if r.get("run_outcome", "").strip()]
    outcomes = Counter(r.get("run_outcome", "").strip() for r in run_rows)
    death_levels = [int(n) for n in (num(r.get("death_level")) for r in run_rows) if n is not None]

    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12, 5))
    max_dmg = int(max(fatal + survived + [3]))
    bins = np.arange(0.5, max_dmg + 1.5, 1)
    ax1.hist(survived, bins=bins, color=BULLET, alpha=0.78, label=f"survived (n={len(survived)})")
    ax1.hist(fatal, bins=bins, color=ACCENT, alpha=0.82, label=f"fatal (n={len(fatal)})")
    ax1.axvline(3, color=TRAP, linestyle=":", linewidth=1.5)
    ax1.text(3.05, ax1.get_ylim()[1] * 0.92, "base HP = 3", color=TRAP)
    ax1.set_xlabel("damage in one interaction chain")
    ax1.set_ylabel("interaction count")
    ax1.set_title("Fatal chains are stacked damage")
    ax1.legend(frameon=False)

    if death_levels:
        level_counts = Counter(death_levels)
        xs = sorted(level_counts)
        ax2.bar([str(x) for x in xs], [level_counts[x] for x in xs], color=ACCENT, edgecolor=DARK_BG)
        ax2.set_xlabel("death level")
        ax2.set_ylabel("run count")
        ax2.set_title("Where death runs ended")
    else:
        ax2.text(0.5, 0.5, "No death-level rows", ha="center", va="center", transform=ax2.transAxes)
        ax2.set_axis_off()

    fig.suptitle("Gold Issue 2: Damage chains can delete the player before recovery is possible", fontsize=13)
    fig.tight_layout()
    file_name = save(fig, "02_gold_damage_spikes_survivability.png")

    all_interactions = fatal + survived
    high_damage = [x for x in all_interactions if x >= 3]
    summary["damage_spikes_survivability"] = {
        "graph": file_name,
        "interaction_count": len(all_interactions),
        "fatal_interactions": len(fatal),
        "fatal_interaction_percent": pct(len(fatal), len(all_interactions)),
        "survived_mean_damage": mean(survived),
        "fatal_mean_damage": mean(fatal),
        "survived_median_damage": med(survived),
        "fatal_median_damage": med(fatal),
        "max_chain_damage": max(all_interactions) if all_interactions else None,
        "chain_damage_at_or_above_base_hp_percent": pct(len(high_damage), len(all_interactions)),
        "run_outcomes": dict(outcomes),
        "death_levels": dict(Counter(death_levels)),
        "median_interaction_duration_seconds": med(durations),
    }


def graph_progression_dropoff(rows: list[dict[str, str]], summary: dict) -> None:
    run_rows = [r for r in rows if r.get("run_outcome", "").strip()]
    highest_levels = [int(n) for n in (num(r.get("highest_level_reached")) for r in run_rows) if n is not None]
    death_levels = [int(n) for n in (num(r.get("death_level")) for r in run_rows) if n is not None]

    room_clears = []
    for row in rows:
        level = num(row.get("room_clear_level"))
        seconds = num(row.get("room_clear_seconds"))
        result = row.get("room_clear_result", "").strip()
        if level is not None and seconds is not None and result:
            room_clears.append((int(level), seconds, result))

    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(12.5, 5))

    if highest_levels:
        max_level = max(highest_levels)
        levels = list(range(1, max_level + 1))
        reached_pct = [pct(sum(1 for x in highest_levels if x >= lvl), len(highest_levels)) for lvl in levels]
        ax1.plot(levels, reached_pct, color=BULLET, marker="o", linewidth=2.2)
        ax1.fill_between(levels, reached_pct, color=BULLET, alpha=0.2)
        ax1.set_ylim(0, 105)
        ax1.set_xlabel("level reached")
        ax1.set_ylabel("% of runs reaching level")
        ax1.set_title("Progression dropoff")
    else:
        ax1.text(0.5, 0.5, "No highest-level rows", ha="center", va="center", transform=ax1.transAxes)
        ax1.set_axis_off()

    level_times: dict[int, list[float]] = defaultdict(list)
    for level, seconds, _ in room_clears:
        level_times[level].append(seconds)
    levels = sorted(level_times)
    if levels:
        med_times = [med(level_times[level]) for level in levels]
        counts = [len(level_times[level]) for level in levels]
        bars = ax2.bar([str(x) for x in levels], med_times, color=DASH, edgecolor=DARK_BG)
        ax2.set_xlabel("room level")
        ax2.set_ylabel("median room-clear seconds")
        ax2.set_title("Room-clear time by level")
        for bar, count in zip(bars, counts):
            ax2.text(bar.get_x() + bar.get_width() / 2, bar.get_height() + 1, f"n={count}", ha="center", fontsize=8)
    else:
        ax2.text(0.5, 0.5, "No room-clear rows", ha="center", va="center", transform=ax2.transAxes)
        ax2.set_axis_off()

    fig.suptitle("Gold Issue 3: Players need clearer onboarding, navigation, and room-state feedback", fontsize=13)
    fig.tight_layout()
    file_name = save(fig, "03_gold_progression_navigation_dropoff.png")

    summary["progression_navigation_dropoff"] = {
        "graph": file_name,
        "run_rows": len(run_rows),
        "highest_level_distribution": dict(Counter(highest_levels)),
        "death_level_distribution": dict(Counter(death_levels)),
        "median_highest_level_reached": med(highest_levels),
        "room_clear_count": len(room_clears),
        "room_clear_counts_by_level": {level: len(level_times[level]) for level in levels},
        "room_clear_median_seconds_by_level": {level: med(level_times[level]) for level in levels},
    }


def graph_enemy_longtail(rows: list[dict[str, str]], summary: dict) -> None:
    per_enemy: dict[str, list[float]] = defaultdict(list)
    basic_methods = Counter()
    skitter_methods = Counter()
    basic_ttks: list[float] = []
    skitter_ttks: list[float] = []

    for row in rows:
        seconds = num(row.get("ttk_seconds"))
        if seconds is None:
            continue
        enemy = row.get("ttk_enemy_name", "").strip() or "Unknown"
        if "Clone" in enemy:
            continue
        method = clean_method(row.get("ttk_kill_method", ""))
        if "Skitter" in enemy:
            skitter_methods[method] += 1
            skitter_ttks.append(seconds)
        else:
            basic_methods[method] += 1
            basic_ttks.append(seconds)
            per_enemy[enemy].append(seconds)

    rows_data = []
    for enemy, ttks in per_enemy.items():
        if len(ttks) >= 15:
            rows_data.append((enemy, len(ttks), med(ttks), mean(ttks)))
    rows_data.sort(key=lambda x: -x[1])
    rows_data = rows_data[:12]

    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(13.5, 5.6))

    if rows_data:
        labels = [x[0] for x in rows_data]
        y = np.arange(len(labels))
        medians = [x[2] for x in rows_data]
        means = [x[3] for x in rows_data]
        counts = [x[1] for x in rows_data]
        for yi, median, avg in zip(y, medians, means):
            ax1.plot([median, avg], [yi, yi], color=NEUTRAL, alpha=0.6, linewidth=2.4)
        ax1.scatter(medians, y, color=BULLET, label="median", s=65, zorder=3)
        ax1.scatter(means, y, color=ACCENT, label="mean", s=65, zorder=3)
        for yi, avg, count in zip(y, means, counts):
            ax1.text(avg + 0.04, yi, f"n={count}", va="center", fontsize=8)
        ax1.set_yticks(y)
        ax1.set_yticklabels(labels)
        ax1.invert_yaxis()
        ax1.set_xlabel("seconds")
        ax1.set_title("Basic enemy TTK long tail")
        ax1.legend(frameon=False)
    else:
        ax1.text(0.5, 0.5, "No enemy rows with n >= 15", ha="center", va="center", transform=ax1.transAxes)
        ax1.set_axis_off()

    methods = ["slash", "bullet", "dash", "trap"]
    x = np.arange(len(methods))
    width = 0.36

    def shares(counter: Counter[str]) -> list[float]:
        total = sum(counter[m] for m in methods)
        return [pct(counter[m], total) for m in methods]

    basic_share = shares(basic_methods)
    skitter_share = shares(skitter_methods)
    ax2.bar(x - width / 2, basic_share, width, color=NEUTRAL, label=f"basic n={sum(basic_methods.values())}")
    ax2.bar(x + width / 2, skitter_share, width, color=[SLASH, BULLET, DASH, TRAP], label=f"Skitter n={sum(skitter_methods.values())}")
    ax2.set_xticks(x)
    ax2.set_xticklabels(methods)
    ax2.set_ylim(0, 100)
    ax2.set_ylabel("share of kills (%)")
    ax2.set_title("Skitter changes method mix")
    ax2.legend(frameon=False)

    fig.suptitle("Gold Issue 4: Enemy behavior needed clearer counterplay and cleanup", fontsize=13)
    fig.tight_layout()
    file_name = save(fig, "04_gold_enemy_longtail_skitter.png")

    summary["enemy_longtail_skitter"] = {
        "graph": file_name,
        "basic_enemy_ttk_count": len(basic_ttks),
        "skitter_ttk_count": len(skitter_ttks),
        "basic_median_ttk_seconds": med(basic_ttks),
        "basic_mean_ttk_seconds": mean(basic_ttks),
        "skitter_median_ttk_seconds": med(skitter_ttks),
        "skitter_mean_ttk_seconds": mean(skitter_ttks),
        "basic_method_share_percent": dict(zip(methods, basic_share)),
        "skitter_method_share_percent": dict(zip(methods, skitter_share)),
        "enemy_mean_median_gap_seconds": {
            enemy: {"n": count, "median": median, "mean": avg, "gap": avg - median}
            for enemy, count, median, avg in rows_data
        },
    }


def write_summary(summary: dict) -> None:
    json_path = OUT_DIR / "gold_analytics_summary.json"
    json_path.write_text(json.dumps(summary, indent=2, sort_keys=True), encoding="utf-8")

    lines = [
        "# Gold Analytics Summary",
        "",
        f"Source CSV: `{summary['source_csv']}`",
        f"Rows analyzed: {summary['rows_analyzed']}",
        f"Date range: {summary['date_range'][0]} to {summary['date_range'][1]}",
        "",
    ]
    for key, data in summary["issues"].items():
        lines.append(f"## {key.replace('_', ' ').title()}")
        lines.append(f"- Graph: `{data['graph']}`")
        for stat_key, stat_value in data.items():
            if stat_key == "graph":
                continue
            lines.append(f"- {stat_key}: {stat_value}")
        lines.append("")

    (OUT_DIR / "README.md").write_text("\n".join(lines), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--csv", type=Path, default=DEFAULT_CSV)
    parser.add_argument("--fetch", action="store_true", help="Fetch the latest CSV from Google Sheets first.")
    args = parser.parse_args()

    csv_path = args.csv
    if not csv_path.is_absolute():
        csv_path = ROOT / csv_path
    if args.fetch or not csv_path.exists():
        fetch_csv(csv_path)

    rows = load_rows(csv_path)
    dates = [t.date().isoformat() for t in (parse_time(r.get("Timestamp", "")) for r in rows) if t]

    summary = {
        "source_csv": str(csv_path),
        "rows_analyzed": len(rows),
        "date_range": [min(dates), max(dates)] if dates else [None, None],
        "issues": {},
    }
    graph_combat_tool_imbalance(rows, summary["issues"])
    graph_damage_spikes(rows, summary["issues"])
    graph_progression_dropoff(rows, summary["issues"])
    graph_enemy_longtail(rows, summary["issues"])
    write_summary(summary)

    print(f"Wrote Gold analytics outputs to {OUT_DIR}")
    for issue in summary["issues"].values():
        print(issue["graph"])
    print("gold_analytics_summary.json")
    print("README.md")


if __name__ == "__main__":
    main()
