(() => {
  const cfg = window.DASHBOARD_CONFIG;
  const SHEET_URL = `https://docs.google.com/spreadsheets/d/${cfg.sheetId}/gviz/tq?tqx=out:csv&gid=${cfg.gid}`;

  const charts = {};
  let lastRows = [];
  let timeWindow = "24h";

  const $ = (id) => document.getElementById(id);
  const fmt = new Intl.NumberFormat("en-US");

  function setStatus(state, msg) {
    $("status-dot").className = `dot ${state}`;
    $("status-text").textContent = msg;
  }

  function parseTimestamp(s) {
    if (!s) return null;
    const d = new Date(s);
    return isNaN(d.getTime()) ? null : d;
  }

  function num(v) {
    if (v === "" || v == null) return null;
    const n = Number(String(v).replace(/,/g, ""));
    return Number.isFinite(n) ? n : null;
  }

  // Each Google Form row is one event with only the columns of that event filled.
  // We classify each row into: run_summary | ttk | room_clear | interaction | other
  function classify(row) {
    if (row.run_outcome) return "run_summary";
    if (row.ttk_enemy_name) return "ttk";
    if (row.room_clear_result) return "room_clear";
    if (row.interaction_end_reason) return "interaction";
    return "other";
  }

  // Best-effort session id: row values for the various *_session_id columns,
  // falling back to (timestamp rounded to minute) so empty-id rows still count
  // as part of a session bucket rather than collapsing to one.
  function sessionKey(row) {
    return (
      row.session_id ||
      row.ttk_session_id ||
      row.room_clear_session_id ||
      row.interaction_session_id ||
      ""
    );
  }

  async function fetchData() {
    setStatus("loading", "Fetching live data…");
    return new Promise((resolve, reject) => {
      Papa.parse(SHEET_URL, {
        download: true,
        header: true,
        skipEmptyLines: true,
        complete: (res) => resolve(res.data),
        error: (err) => reject(err),
      });
    });
  }

  function aggregate(rows) {
    const sessions = new Set();
    let runs = 0;
    let deaths = 0;
    let kills = 0;
    let lastPlay = null;

    const killMethods = { bullet: 0, slash: 0, trap: 0, dash: 0, unknown: 0 };
    const outcomeCounts = {};
    const deathsByLevel = {};
    const deathsTimeline = []; // array of Date

    for (const row of rows) {
      const ts = parseTimestamp(row.Timestamp);
      if (ts && (!lastPlay || ts > lastPlay)) lastPlay = ts;

      const sid = sessionKey(row);
      if (sid) sessions.add(sid);

      const kind = classify(row);

      if (kind === "run_summary") {
        runs++;
        const outcome = (row.run_outcome || "unknown").toLowerCase();
        outcomeCounts[outcome] = (outcomeCounts[outcome] || 0) + 1;
        if (outcome === "death") {
          deaths++;
          if (ts) deathsTimeline.push(ts);
          const lvl = num(row.death_level);
          if (lvl != null) {
            deathsByLevel[lvl] = (deathsByLevel[lvl] || 0) + 1;
          }
        }
        kills += num(row.total_kills) || 0;
        killMethods.bullet += num(row.bullet_kills) || 0;
        killMethods.slash += num(row.slash_kills) || 0;
        killMethods.trap += num(row.trap_kills) || 0;
        killMethods.dash += num(row.dash_kills) || 0;
        killMethods.unknown += num(row.unknown_kills) || 0;
      }
    }

    return {
      sessions: sessions.size,
      runs,
      deaths,
      kills,
      lastPlay,
      killMethods,
      outcomeCounts,
      deathsByLevel,
      deathsTimeline,
    };
  }

  function bucketDeaths(timeline, windowKey) {
    const now = Date.now();
    let cutoff, bucketMs, fmtLabel;
    if (windowKey === "24h") {
      cutoff = now - 24 * 3600_000;
      bucketMs = 3600_000; // 1h buckets
      fmtLabel = (d) => d.getHours().toString().padStart(2, "0") + ":00";
    } else if (windowKey === "7d") {
      cutoff = now - 7 * 24 * 3600_000;
      bucketMs = 24 * 3600_000;
      fmtLabel = (d) => `${d.getMonth() + 1}/${d.getDate()}`;
    } else {
      cutoff = -Infinity;
      bucketMs = 24 * 3600_000;
      fmtLabel = (d) => `${d.getMonth() + 1}/${d.getDate()}`;
    }

    const buckets = new Map();
    for (const t of timeline) {
      const ms = t.getTime();
      if (ms < cutoff) continue;
      const bucket = Math.floor(ms / bucketMs) * bucketMs;
      buckets.set(bucket, (buckets.get(bucket) || 0) + 1);
    }

    // Build full bucket range so empty intervals still show up
    const sortedKeys = [...buckets.keys()].sort((a, b) => a - b);
    if (sortedKeys.length === 0) return { labels: [], values: [] };

    const start = windowKey === "all" ? sortedKeys[0] : Math.floor(cutoff / bucketMs) * bucketMs;
    const end = Math.floor(now / bucketMs) * bucketMs;
    const labels = [];
    const values = [];
    for (let k = start; k <= end; k += bucketMs) {
      labels.push(fmtLabel(new Date(k)));
      values.push(buckets.get(k) || 0);
    }
    return { labels, values };
  }

  const PALETTE = {
    bullet: "#ef476f",
    slash: "#ffd166",
    trap: "#118ab2",
    dash: "#06d6a0",
    unknown: "#8a93a6",
    line: "#06d6a0",
    bar: "#ffd166",
    outcome: ["#06d6a0", "#ef476f", "#ffd166", "#118ab2", "#8a93a6"],
  };

  const COMMON_OPTS = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { labels: { color: "#e7ecf3" } },
      tooltip: { backgroundColor: "#1c2230", borderColor: "#2a3142", borderWidth: 1 },
    },
    scales: {
      x: { ticks: { color: "#8a93a6" }, grid: { color: "#1c2230" } },
      y: { ticks: { color: "#8a93a6" }, grid: { color: "#1c2230" }, beginAtZero: true },
    },
  };

  function renderKillMethods(km) {
    const ctx = $("kill-method-chart");
    const data = {
      labels: ["Bullet", "Slash", "Trap", "Dash", "Unknown"],
      datasets: [{
        data: [km.bullet, km.slash, km.trap, km.dash, km.unknown],
        backgroundColor: [PALETTE.bullet, PALETTE.slash, PALETTE.trap, PALETTE.dash, PALETTE.unknown],
        borderColor: "#0b0d12",
        borderWidth: 2,
      }],
    };
    if (charts.killMethod) { charts.killMethod.data = data; charts.killMethod.update(); return; }
    charts.killMethod = new Chart(ctx, {
      type: "doughnut",
      data,
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: "right", labels: { color: "#e7ecf3" } } },
      },
    });
  }

  function renderDeathsTime(timeline) {
    const { labels, values } = bucketDeaths(timeline, timeWindow);
    const ctx = $("deaths-time-chart");
    const data = {
      labels,
      datasets: [{
        label: "Deaths",
        data: values,
        borderColor: PALETTE.line,
        backgroundColor: "rgba(6,214,160,0.15)",
        fill: true,
        tension: 0.25,
        pointRadius: 2,
      }],
    };
    if (charts.deathsTime) { charts.deathsTime.data = data; charts.deathsTime.update(); return; }
    charts.deathsTime = new Chart(ctx, { type: "line", data, options: COMMON_OPTS });
  }

  function renderDeathsByLevel(map) {
    const levels = Object.keys(map).map(Number).sort((a, b) => a - b);
    const ctx = $("deaths-by-level-chart");
    const data = {
      labels: levels.map((l) => `Level ${l}`),
      datasets: [{
        label: "Deaths",
        data: levels.map((l) => map[l]),
        backgroundColor: PALETTE.bar,
        borderRadius: 6,
      }],
    };
    if (charts.deathsByLevel) { charts.deathsByLevel.data = data; charts.deathsByLevel.update(); return; }
    charts.deathsByLevel = new Chart(ctx, { type: "bar", data, options: COMMON_OPTS });
  }

  function renderOutcomes(counts) {
    const labels = Object.keys(counts);
    const ctx = $("outcome-chart");
    const data = {
      labels,
      datasets: [{
        label: "Runs",
        data: labels.map((k) => counts[k]),
        backgroundColor: labels.map((_, i) => PALETTE.outcome[i % PALETTE.outcome.length]),
        borderRadius: 6,
      }],
    };
    if (charts.outcome) { charts.outcome.data = data; charts.outcome.update(); return; }
    charts.outcome = new Chart(ctx, { type: "bar", data, options: COMMON_OPTS });
  }

  function relTime(d) {
    if (!d) return "—";
    const diffMs = Date.now() - d.getTime();
    const sec = Math.round(diffMs / 1000);
    if (sec < 60) return `${sec}s ago`;
    const min = Math.round(sec / 60);
    if (min < 60) return `${min}m ago`;
    const hr = Math.round(min / 60);
    if (hr < 24) return `${hr}h ago`;
    const day = Math.round(hr / 24);
    return `${day}d ago`;
  }

  function renderAll(rows) {
    lastRows = rows;
    const a = aggregate(rows);

    $("kpi-sessions").textContent = fmt.format(a.sessions);
    $("kpi-runs").textContent = fmt.format(a.runs);
    $("kpi-deaths").textContent = fmt.format(a.deaths);
    $("kpi-kills").textContent = fmt.format(a.kills);
    $("kpi-lastplay").textContent = a.lastPlay
      ? `${relTime(a.lastPlay)} (${a.lastPlay.toLocaleString()})`
      : "—";

    renderKillMethods(a.killMethods);
    renderDeathsTime(a.deathsTimeline);
    renderDeathsByLevel(a.deathsByLevel);
    renderOutcomes(a.outcomeCounts);
  }

  async function refresh() {
    try {
      const rows = await fetchData();
      renderAll(rows);
      setStatus("ok", `Updated ${new Date().toLocaleTimeString()}`);
    } catch (err) {
      console.error(err);
      setStatus("error", "Failed to load — sheet may not be public");
    }
  }

  // Tab handlers for time window
  document.querySelectorAll(".tab").forEach((btn) => {
    btn.addEventListener("click", () => {
      document.querySelectorAll(".tab").forEach((b) => b.classList.remove("active"));
      btn.classList.add("active");
      timeWindow = btn.dataset.window;
      if (lastRows.length) {
        const a = aggregate(lastRows);
        renderDeathsTime(a.deathsTimeline);
      }
    });
  });

  $("refresh-btn").addEventListener("click", refresh);

  function updateClock() {
    const now = new Date();
    $("header-date").textContent = now.toLocaleDateString(undefined, {
      weekday: "short", year: "numeric", month: "short", day: "numeric",
    });
    $("header-time").textContent = now.toLocaleTimeString();
  }
  updateClock();
  setInterval(updateClock, 1000);

  refresh();
  setInterval(refresh, cfg.refreshIntervalMs);
})();
