#!/usr/bin/env python3
"""Generates the charts of the documentation from the results of run.py.

Times cover several orders of magnitude, so the charts are dot plots on a logarithmic axis. Each engine has its own lane
in a row of a workload, and its own marker shape, so that its identity does not depend on the color alone.
"""

import math
from pathlib import Path

import run

OUTPUT = run.ROOT.parent / "docs" / "static" / "img"

STYLE = """
  svg { --surface: #fcfcfb; --text: #0b0b0b; --muted: #52514e; --grid: #e4e3df;
        --s1: #2a78d6; --s2: #eb6834; --s3: #1baf7a; --s4: #eda100; --s5: #e87ba4; --s6: #008300; }
  @media (prefers-color-scheme: dark) {
    svg { --surface: #1a1a19; --text: #f0efec; --muted: #c3c2b7; --grid: #3a3a37;
          --s1: #3987e5; --s2: #d95926; --s3: #199e70; --s4: #c98500; --s5: #d55181; --s6: #008300; }
  }
  text { font-family: -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif; fill: var(--text); }
  .title { font-size: 16px; font-weight: 600; }
  .note { font-size: 12px; fill: var(--muted); }
  .row { font-size: 13px; }
  .tick { font-size: 12px; fill: var(--muted); }
  .value { font-size: 11px; fill: var(--muted); }
  .legend { font-size: 12px; }
  .grid { stroke: var(--grid); stroke-width: 1; }
  .band { fill: var(--grid); opacity: 0.28; }
  .link { stroke: var(--muted); stroke-width: 1.5; opacity: 0.6; }
"""

MARKERS = ["circle", "square", "diamond", "triangle", "triangle-down", "plus"]

# an engine has the same color and shape in every chart
ENGINE_SLOTS = {"WaaS Burst": 0, "WaaS CoreCLR": 1, "xLua": 2, "Lua-CSharp": 3, "wasm3": 4, "wasmtime": 5}


def marker(shape, x, y, color, title, size=4.5, hollow=False):
    fill = "var(--surface)" if hollow else color
    stroke = f'stroke="{color if hollow else "var(--surface)"}" stroke-width="{2 if hollow else 1}"'
    tooltip = f"<title>{title}</title>"
    if shape == "circle":
        return f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{size}" fill="{fill}" {stroke}>{tooltip}</circle>'
    if shape == "square":
        s = size * 0.9
        return f'<rect x="{x - s:.1f}" y="{y - s:.1f}" width="{2 * s:.1f}" height="{2 * s:.1f}" rx="1" fill="{fill}" {stroke}>{tooltip}</rect>'
    points = {
        "diamond": [(0, -1.3), (1.3, 0), (0, 1.3), (-1.3, 0)],
        "triangle": [(0, -1.25), (1.2, 0.9), (-1.2, 0.9)],
        "triangle-down": [(0, 1.25), (1.2, -0.9), (-1.2, -0.9)],
        "plus": [(-0.4, -1.2), (0.4, -1.2), (0.4, -0.4), (1.2, -0.4), (1.2, 0.4), (0.4, 0.4), (0.4, 1.2), (-0.4, 1.2),
                 (-0.4, 0.4), (-1.2, 0.4), (-1.2, -0.4), (-0.4, -0.4)],
    }[shape]
    path = " ".join(f"{x + a * size:.1f},{y + b * size:.1f}" for a, b in points)
    return f'<polygon points="{path}" fill="{fill}" {stroke}>{tooltip}</polygon>'


class Axis:
    def __init__(self, low, high, left, right):
        self.low, self.high, self.left, self.right = low, high, left, right

    def x(self, value):
        ratio = (math.log10(value) - math.log10(self.low)) / (math.log10(self.high) - math.log10(self.low))
        return self.left + ratio * (self.right - self.left)

    def ticks(self):
        exponent = round(math.log10(self.low))
        while 10 ** exponent <= self.high:
            yield 10 ** exponent
            exponent += 1


def frame(width, height, title, note, body):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width} {height}" width="{width}" height="{height}" '
            f'role="img" aria-label="{title}"><style>{STYLE}</style>'
            f'<rect width="{width}" height="{height}" rx="8" fill="var(--surface)"/>'
            f'<text class="title" x="24" y="32">{title}</text><text class="note" x="24" y="51">{note}</text>'
            f'{body}</svg>\n')


def axis_lines(axis, top, bottom, unit):
    parts = []
    for tick in axis.ticks():
        x = axis.x(tick)
        parts.append(f'<line class="grid" x1="{x:.1f}" y1="{top}" x2="{x:.1f}" y2="{bottom}"/>')
        parts.append(f'<text class="tick" x="{x:.1f}" y="{bottom + 18}" text-anchor="middle">{tick:,} {unit}</text>')
    return parts


def legend(entries, y, left=24, step=None):
    parts = []
    x = left
    for label, draw in entries:
        parts.append(draw(x + 6, y - 4))
        parts.append(f'<text class="legend" x="{x + 18}" y="{y}">{label}</text>')
        x += step or (len(label) * 7 + 44)
    return parts


def dot_plot(path, title, note, engines, names, times, unit, low, high, emphasized, digits, unsupported=None):
    width, left, right = 860, 118, 830
    lane, gap = 11, 12
    top = 96
    row_height = lane * len(engines) + gap
    bottom = top + row_height * len(names)
    axis = Axis(low, high, left, right)
    body = legend([(column, lambda x, y, s=ENGINE_SLOTS[column]: marker(MARKERS[s], x, y, f"var(--s{s + 1})", column))
                   for column, _ in engines], 80)
    body += axis_lines(axis, top, bottom, unit)
    for row, name in enumerate(names):
        y0 = top + row * row_height
        if row % 2 == 0:
            body.append(f'<rect class="band" x="{left - 8}" y="{y0}" width="{right - left + 16}" height="{row_height}"/>')
        label = name.replace("compute_", "")
        body.append(f'<text class="row" x="{left - 16}" y="{y0 + row_height / 2 + 4}" text-anchor="end">{label}</text>')
        for i, (column, _) in enumerate(engines):
            value = times.get(name, {}).get(column)
            if value is None:
                continue
            y = y0 + gap / 2 + lane * (i + 0.5)
            x = axis.x(value)
            text = f"{value:,.{digits}f} {unit}"
            slot = ENGINE_SLOTS[column]
            body.append(marker(MARKERS[slot], x, y, f"var(--s{slot + 1})", f"{label}, {column}: {text}"))
            if column in emphasized:
                body.append(f'<text class="value" x="{x + 9:.1f}" y="{y + 4}">{value:,.{digits}f}</text>')
    (OUTPUT / path).write_text(frame(width, bottom + 34, title, note, "".join(body)), encoding="utf-8", newline="\n")


def guest_plot(path, title, note, engines, names, times, unit, low, high):
    """Connects the results of the two guests of each engine: the distance is what the guest language costs."""
    width, left, right = 860, 118, 830
    lane, gap = 15, 12
    top = 96
    row_height = lane * len(engines) + gap
    bottom = top + row_height * len(names)
    axis = Axis(low, high, left, right)
    entries = [(label, lambda x, y, s=ENGINE_SLOTS[label]: marker(MARKERS[s], x, y, f"var(--s{s + 1})", label))
               for label, _, _ in engines]
    entries.append(("Filled: Rust", lambda x, y: marker("circle", x, y, "var(--muted)", "Rust")))
    entries.append(("Hollow: MoonBit", lambda x, y: marker("circle", x, y, "var(--muted)", "MoonBit", hollow=True)))
    body = legend(entries, 80)
    body += axis_lines(axis, top, bottom, unit)
    for row, name in enumerate(names):
        y0 = top + row * row_height
        if row % 2 == 0:
            body.append(f'<rect class="band" x="{left - 8}" y="{y0}" width="{right - left + 16}" height="{row_height}"/>')
        label = name.replace("compute_", "")
        body.append(f'<text class="row" x="{left - 16}" y="{y0 + row_height / 2 + 4}" text-anchor="end">{label}</text>')
        for i, (engine, rust_column, moonbit_column) in enumerate(engines):
            y = y0 + gap / 2 + lane * (i + 0.5)
            rust = times.get(name, {}).get(rust_column)
            moonbit = times.get(name, {}).get(moonbit_column)
            slot = ENGINE_SLOTS[engine]
            color = f"var(--s{slot + 1})"
            if rust and moonbit:
                body.append(f'<line class="link" x1="{axis.x(rust):.1f}" y1="{y}" x2="{axis.x(moonbit):.1f}" y2="{y}"/>')
            if rust:
                body.append(marker(MARKERS[slot], axis.x(rust), y, color, f"{label}, {engine}, Rust: {rust:,.1f} {unit}"))
            if moonbit:
                body.append(marker(MARKERS[slot], axis.x(moonbit), y, color,
                                   f"{label}, {engine}, MoonBit: {moonbit:,.1f} {unit}", hollow=True))
                body.append(f'<text class="value" x="{axis.x(moonbit) + 9:.1f}" y="{y + 4}">{moonbit / rust:.1f}x</text>'
                            if rust else "")
            elif rust:
                body.append(f'<text class="value" x="{axis.x(rust) + 9:.1f}" y="{y + 4}">MoonBit: n/a</text>')
    (OUTPUT / path).write_text(frame(width, bottom + 34, title, note, "".join(body)), encoding="utf-8", newline="\n")


def main():
    warm = run.read_records(run.RESULTS_DIR / "il2cpp", "warm-*.json")
    coreclr = run.read_records(run.RESULTS_DIR / "coreclr", "warm-*.json")
    native = run.read_records(run.RESULTS_DIR / "native", "warm-*.json")

    def pick(rounds, engine, guest=None):
        return [r for data in rounds for r in data["records"]
                if r.get("engine") == engine and (guest is None or r.get("guest") == guest)]

    times, unsupported = run.collect({
        "WaaS Burst": pick(warm, "waas"),
        "WaaS CoreCLR": pick(coreclr, "waas-coreclr", "rust"),
        "xLua": pick(warm, "xlua"),
        "Lua-CSharp": pick(warm, "lua-csharp"),
        "wasm3": pick(native, "wasm3", "rust"),
        "wasmtime": pick(native, "wasmtime", "rust"),
        "WaaS Burst (MoonBit)": pick(warm, "waas-moonbit"),
        "wasm3 (MoonBit)": pick(native, "wasm3", "moonbit"),
        "wasmtime (MoonBit)": pick(native, "wasmtime", "moonbit"),
    })
    compute = [n for n in times if n.startswith("compute_")]
    calls = [n for n in times if not n.startswith("compute_")]

    OUTPUT.mkdir(parents=True, exist_ok=True)
    dot_plot("performance-compute.svg", "Compute workloads",
             "Milliseconds on a logarithmic scale, lower is better. WebAssembly guests are compiled from Rust.",
             [(c, None) for c in ("WaaS Burst", "WaaS CoreCLR", "xLua", "Lua-CSharp", "wasm3", "wasmtime")],
             compute, times, "ms", 1, 1000, {"WaaS Burst"}, 1)
    dot_plot("performance-calls.svg", "Calls between scripts and the host",
             "Nanoseconds per call on a logarithmic scale, lower is better. WaaS guests are compiled from Rust.",
             [(c, None) for c in ("WaaS Burst", "WaaS CoreCLR", "xLua", "Lua-CSharp")],
             calls, times, "ns", 10, 10000, {"WaaS Burst"}, 0)
    guest_plot("performance-guests.svg", "Rust and MoonBit guests",
               "Milliseconds on a logarithmic scale, lower is better. The label is the time of MoonBit relative to Rust.",
               [("WaaS Burst", "WaaS Burst", "WaaS Burst (MoonBit)"), ("wasm3", "wasm3", "wasm3 (MoonBit)"),
                ("wasmtime", "wasmtime", "wasmtime (MoonBit)")],
               compute, times, "ms", 1, 1000)


if __name__ == "__main__":
    main()
