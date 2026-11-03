#!/usr/bin/env python3
"""Sets up, builds and runs the comparison between WaaS and xLua. See README.md."""

import argparse
import io
import json
import os
import shutil
import statistics
import subprocess
import sys
import urllib.request
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent
UNITY_PROJECT = ROOT / "unity"
BUILD_DIR = ROOT / ".build"
RESULTS_DIR = ROOT / "results"
STREAMING_ASSETS = UNITY_PROJECT / "Assets" / "StreamingAssets"

# master at the time of writing. Its Windows x64 native library is Lua 5.3.
XLUA_COMMIT = "59bf42685dbe36fe0e1678a6ba8597f859ef7ca3"
XLUA_URL = f"https://github.com/Tencent/xLua/archive/{XLUA_COMMIT}.zip"

LUA_CSHARP_VERSION = "0.5.8"
# package id -> (version, directory in the package, destination relative to the Unity project)
LUA_CSHARP_PACKAGES = {
    "LuaCSharp": (LUA_CSHARP_VERSION, "lib/netstandard2.1/", "Assets/LuaCSharp"),
    "LuaCSharp.Annotations": (LUA_CSHARP_VERSION, "lib/netstandard2.1/", "Assets/LuaCSharp"),
    "Microsoft.Bcl.TimeProvider": ("8.0.0", "lib/netstandard2.0/", "Assets/LuaCSharp"),
    # loaded by Assets/csc.rsp
    "LuaCSharp.SourceGenerator": (LUA_CSHARP_VERSION, "analyzers/dotnet/cs/", "Tools/LuaCSharpAnalyzer"),
}

# the main branch at the time of writing. The native harness builds it with clang, which it needs for tail calls.
WASM3_COMMIT = "28ecb9af6d2040e474a70f7cb7f43666740141fb"
WASM3_URL = f"https://github.com/wasm3/wasm3/archive/{WASM3_COMMIT}.zip"

BACKENDS = {"mono": ("Mono", "BuildMono"), "il2cpp": ("IL2CPP", "BuildIl2Cpp")}
COLD_MODES = ["cold-waas", "cold-waas-moonbit", "cold-lua", "cold-lua-bytecode", "cold-lua-csharp"]
ENGINE_LABELS = {"waas": "WaaS", "waas-moonbit": "WaaS (MoonBit)", "xlua": "xLua", "xlua-bytecode": "xLua (bytecode)", "lua-csharp": "Lua-CSharp"}
UNITY_TIMEOUT = 900
STAGES = ["engine-init", "load", "instantiate", "first-call"]


def run(command, cwd=None, env=None):
    print("+", " ".join(str(c) for c in command), flush=True)
    # CreateProcess searches the PATH of this process, not the one in env
    executable = shutil.which(str(command[0]), path=(env or os.environ)["PATH"]) or str(command[0])
    subprocess.run([executable, *map(str, command[1:])], cwd=cwd, env=env, check=True)


def tool_environment():
    """Puts the active Rust toolchain first on PATH. The proxies of rustup are symbolic links on some setups."""
    env = dict(os.environ)
    rustup = shutil.which("rustup")
    if rustup:
        sysroot = subprocess.run([rustup, "run", "stable", "rustc", "--print", "sysroot"],
                                 capture_output=True, text=True)
        if sysroot.returncode == 0:
            env["PATH"] = str(Path(sysroot.stdout.strip()) / "bin") + os.pathsep + env["PATH"]
    return env


def unity_editor():
    configured = os.environ.get("UNITY_EDITOR")
    if configured:
        return Path(configured)
    version_file = UNITY_PROJECT / "ProjectSettings" / "ProjectVersion.txt"
    version = version_file.read_text().splitlines()[0].split(":")[1].strip()
    return Path(r"C:\Program Files\Unity\Hub\Editor") / version / "Editor" / "Unity.exe"


def run_unity(method, log_name):
    BUILD_DIR.mkdir(exist_ok=True)
    log = BUILD_DIR / f"{log_name}.log"
    # the directory monitor can deadlock the Editor on a ReFS Dev Drive (Unity issue 23832)
    command = [unity_editor(), "-batchmode", "-nographics", "-quit", "-DisableDirectoryMonitor",
               "-projectPath", UNITY_PROJECT,
               "-executeMethod", f"WaaS.Benchmarks.Editor.BenchmarkBuild.{method}", "-logFile", log]
    print("+", " ".join(str(c) for c in command), flush=True)
    try:
        subprocess.run([str(c) for c in command], check=True, timeout=UNITY_TIMEOUT)
    except subprocess.TimeoutExpired:
        sys.exit(f"Unity did not finish in {UNITY_TIMEOUT} seconds. See {log}")
    except subprocess.CalledProcessError:
        sys.exit(f"Unity failed. See {log}")


def setup_xlua():
    marker = UNITY_PROJECT / "Assets" / "XLua" / ".commit"
    if marker.exists() and marker.read_text().strip() == XLUA_COMMIT:
        return

    print(f"Downloading xLua {XLUA_COMMIT}", flush=True)
    with urllib.request.urlopen(XLUA_URL) as response:
        archive = zipfile.ZipFile(io.BytesIO(response.read()))

    assets = UNITY_PROJECT / "Assets"
    shutil.rmtree(assets / "XLua", ignore_errors=True)
    shutil.rmtree(assets / "Plugins", ignore_errors=True)

    # the examples and tutorials are not needed, and their configuration would add types to the generated code
    wanted = ("Assets/XLua/Src/", "Assets/XLua/Resources/xlua/", "Assets/XLua/Editor/XLuaUnityDefaultConfig.cs",
              "Assets/Plugins/x86_64/xlua.dll")
    for info in archive.infolist():
        relative = info.filename.split("/", 1)[1]
        if info.is_dir() or not relative.startswith(wanted):
            continue
        destination = UNITY_PROJECT / relative
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(archive.read(info))
    marker.write_text(XLUA_COMMIT + "\n")


def setup_lua_csharp():
    marker = UNITY_PROJECT / "Assets" / "LuaCSharp" / ".version"
    if marker.exists() and marker.read_text().strip() == LUA_CSHARP_VERSION:
        return

    shutil.rmtree(UNITY_PROJECT / "Assets" / "LuaCSharp", ignore_errors=True)
    shutil.rmtree(UNITY_PROJECT / "Tools", ignore_errors=True)
    for package, (version, directory_in_package, destination) in LUA_CSHARP_PACKAGES.items():
        print(f"Downloading {package} {version}", flush=True)
        url = f"https://www.nuget.org/api/v2/package/{package}/{version}"
        with urllib.request.urlopen(url) as response:
            archive = zipfile.ZipFile(io.BytesIO(response.read()))
        names = [n for n in archive.namelist() if n.startswith(directory_in_package) and n.endswith(".dll")]
        if not names:
            sys.exit(f"{package} has no assembly in {directory_in_package}: {archive.namelist()}")
        directory = UNITY_PROJECT / destination
        directory.mkdir(parents=True, exist_ok=True)
        for name in names:
            (directory / Path(name).name).write_bytes(archive.read(name))
    marker.write_text(LUA_CSHARP_VERSION + "\n")


def build_guest():
    env = tool_environment()
    STREAMING_ASSETS.mkdir(parents=True, exist_ok=True)

    target_dir = BUILD_DIR / "guest"
    run(["cargo", "build", "--release", "--target", "wasm32-unknown-unknown", "--target-dir", target_dir],
        cwd=ROOT / "guest", env=env)
    module = target_dir / "wasm32-unknown-unknown" / "release" / "waas_benchmark_guest.wasm"
    run(["wasm-tools", "component", "new", module, "-o", STREAMING_ASSETS / "benchmark.wasm"], env=env)

    moonbit = ROOT / "guest-moonbit"
    run(["moon", "build", "gen", "compute", "--target", "wasm", "--release"], cwd=moonbit, env=env)
    embedded = BUILD_DIR / "benchmark-moonbit.embed.wasm"
    run(["wasm-tools", "component", "embed", ROOT / "guest" / "wit", "--world", "benchmark", "--encoding", "utf16",
         moonbit / "_build" / "wasm" / "release" / "build" / "gen" / "gen.wasm", "-o", embedded], env=env)
    run(["wasm-tools", "component", "new", embedded, "-o", STREAMING_ASSETS / "benchmark-moonbit.wasm"], env=env)

    shutil.copy(ROOT / "lua" / "benchmark.lua", STREAMING_ASSETS / "benchmark.lua")


def setup_wasm3():
    directory = BUILD_DIR / "wasm3"
    source = directory / f"wasm3-{WASM3_COMMIT}"
    if source.exists():
        return source

    print(f"Downloading wasm3 {WASM3_COMMIT}", flush=True)
    with urllib.request.urlopen(WASM3_URL) as response:
        zipfile.ZipFile(io.BytesIO(response.read())).extractall(directory)
    return source


def find_clang():
    configured = os.environ.get("WASM3_CLANG") or shutil.which("clang")
    if configured:
        return configured

    # the LLVM tools of Visual Studio
    for root in Path(r"C:\Program Files\Microsoft Visual Studio").glob("*/*/VC/Tools/Llvm/x64/bin/clang.exe"):
        return str(root)
    sys.exit("clang is not found. Set WASM3_CLANG.")


def build_outside_unity():
    env = tool_environment()
    target_dir = BUILD_DIR / "guest-compute"
    run(["cargo", "build", "--release", "--target", "wasm32-unknown-unknown", "--target-dir", target_dir],
        cwd=ROOT / "guest-compute", env=env)

    native_env = dict(env, WASM3_SOURCE=str(setup_wasm3()), WASM3_CLANG=find_clang())
    run(["cargo", "build", "--release", "--target-dir", BUILD_DIR / "native"], cwd=ROOT / "native", env=native_env)

    run(["dotnet", "build", ROOT / "dotnet", "-c", "Release", "-o", BUILD_DIR / "coreclr"], env=env)


def generate_bindings():
    output = BUILD_DIR / "generated"
    shutil.rmtree(output, ignore_errors=True)
    run(["wit2waas", "-w", "wit", "-o", output], cwd=ROOT / "guest")
    for path in (output / "waas" / "benchmark").glob("*.g.cs"):
        shutil.copy(path, UNITY_PROJECT / "Assets" / "Benchmark" / "Bindings" / path.name)


def build_players(backends):
    setup_xlua()
    setup_lua_csharp()
    build_guest()
    build_outside_unity()
    run_unity("GenerateLuaBindings", "generate-lua-bindings")
    for backend in backends:
        run_unity(BACKENDS[backend][1], f"build-{backend}")


def run_player(backend, mode, output):
    executable = UNITY_PROJECT / "Builds" / BACKENDS[backend][0] / "Benchmark.exe"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.unlink(missing_ok=True)
    log = output.with_suffix(".log")
    subprocess.run([str(executable), "-batchmode", "-nographics", "-benchmark-mode", mode,
                    "-benchmark-output", str(output), "-logFile", str(log)], check=False)
    if not output.exists():
        sys.exit(f"The player did not write its results. See {log}")


def run_benchmarks(backends, cold_runs, rounds):
    """Runs the harnesses alternately, so that a change of the load of the machine affects all of them alike."""
    env = tool_environment()
    shutil.rmtree(RESULTS_DIR, ignore_errors=True)
    rust_module = BUILD_DIR / "guest-compute" / "wasm32-unknown-unknown" / "release" / "waas_benchmark_compute.wasm"
    moonbit_module = ROOT / "guest-moonbit" / "_build" / "wasm" / "release" / "build" / "compute" / "compute.wasm"

    for backend in backends:
        for mode in COLD_MODES:
            print(f"{backend}: {mode} x {cold_runs}", flush=True)
            for i in range(cold_runs):
                run_player(backend, mode, RESULTS_DIR / backend / f"{mode}-{i}.json")

    for i in range(rounds):
        print(f"round {i + 1} / {rounds}", flush=True)
        for backend in backends:
            run_player(backend, "warm", RESULTS_DIR / backend / f"warm-{i}.json")

        output = RESULTS_DIR / "coreclr" / f"warm-{i}.json"
        output.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run([str(BUILD_DIR / "coreclr" / "WaaS.Benchmarks.CoreClr.exe"),
                        str(STREAMING_ASSETS / "benchmark.wasm"), str(STREAMING_ASSETS / "benchmark-moonbit.wasm"),
                        str(output)], check=True)

        output = RESULTS_DIR / "native" / f"warm-{i}.json"
        output.parent.mkdir(parents=True, exist_ok=True)
        result = subprocess.run([str(BUILD_DIR / "native" / "release" / "waas-benchmark-native.exe"),
                                 str(rust_module), str(moonbit_module)], check=True, capture_output=True, env=env)
        output.write_bytes(result.stdout)


def median_load(records):
    """Returns {engine: {stage: median of the stage}} from load records."""
    values = {}
    for record in records:
        values.setdefault(record["engine"], {}).setdefault(record["stage"], []).append(record["ms"])
    return {engine: {stage: statistics.median(v) for stage, v in stages.items()} for engine, stages in values.items()}


def read_records(directory, pattern):
    records = []
    for path in sorted(directory.glob(pattern)):
        records.append(json.loads(path.read_text(encoding="utf-8-sig")))
    return records


def collect(records_by_column):
    """Returns {workload: {column: the lowest median of the rounds}}, and checks that the checksums agree."""
    times, checksums, unsupported = {}, {}, {}
    for column, records in records_by_column.items():
        for record in records:
            if record["kind"] == "unsupported":
                unsupported.setdefault(record["name"], {})[column] = record["message"]
            if record["kind"] != "exec":
                continue
            name = record["name"]
            scale = 1 if record["group"] == "compute" else 1e6 / record["n"]
            value = statistics.median(record["samples"]) * scale
            cell = times.setdefault(name, {})
            cell[column] = min(cell.get(column, value), value)
            checksums.setdefault(name, set()).add(record["checksum"])

    for name, values in checksums.items():
        if len(values) != 1:
            sys.exit(f"The checksums of {name} differ: {values}")
    return times, unsupported


def table(columns, times, unsupported, names, digits):
    lines = ["| Workload | " + " | ".join(columns) + " |", "|---|" + "---:|" * len(columns)]
    for name in names:
        cells = []
        for column in columns:
            value = times.get(name, {}).get(column)
            if value is not None:
                cells.append(f"{value:,.{digits}f}")
            else:
                cells.append("n/a" if column in unsupported.get(name, {}) else "-")
        lines.append(f"| {name.replace('compute_', '')} | " + " | ".join(cells) + " |")
    return lines


def report(backends):
    backend = "il2cpp"
    warm = read_records(RESULTS_DIR / backend, "warm-*.json")
    meta = warm[0]

    def pick(rounds, engine, guest=None):
        result = []
        for data in rounds:
            result += [r for r in data["records"] if r.get("engine") == engine and
                       (guest is None or r.get("guest") == guest)]
        return result

    coreclr = read_records(RESULTS_DIR / "coreclr", "warm-*.json")
    native = read_records(RESULTS_DIR / "native", "warm-*.json")
    records = {
        "WaaS CoreCLR (Rust)": pick(coreclr, "waas-coreclr", "rust"),
        "WaaS CoreCLR (MoonBit)": pick(coreclr, "waas-coreclr", "moonbit"),
        "WaaS Burst (Rust)": pick(warm, "waas"),
        "WaaS Burst (MoonBit)": pick(warm, "waas-moonbit"),
        "xLua": pick(warm, "xlua"),
        "Lua-CSharp": pick(warm, "lua-csharp"),
        "wasm3 (Rust)": pick(native, "wasm3", "rust"),
        "wasm3 (MoonBit)": pick(native, "wasm3", "moonbit"),
        "wasmtime (Rust)": pick(native, "wasmtime", "rust"),
        "wasmtime (MoonBit)": pick(native, "wasmtime", "moonbit"),
    }
    times, unsupported = collect(records)

    compute = [n for n in times if n.startswith("compute_")]
    calls = [n for n in times if not n.startswith("compute_")]
    columns = list(records)
    sizes = meta["sizes"]
    lines = [f"Unity {meta['unity']}, {meta['cpu']}, {meta['os']}, development build: {meta['development']}", "",
             f"Component: Rust {sizes['component']:,} bytes, MoonBit {sizes['moonbit']:,} bytes. "
             f"Lua source {sizes['lua']:,} bytes.", "", "### Compute (ms)", ""]
    lines += table(columns, times, unsupported, compute, 1)
    lines += ["", "### Calls across the script boundary (ns/call)", ""]
    lines += table(columns[:6], times, unsupported, calls, 0)

    lines += ["", "### Load (ms)", "",
              "| Engine | Engine init | Load | Instantiate | First call | Total |", "|---|---:|---:|---:|---:|---:|"]
    cold = read_records(RESULTS_DIR / backend, "cold-*.json")
    medians = median_load([r for data in cold for r in data["records"]])
    for engine, label in ENGINE_LABELS.items():
        stages = medians[engine]
        cells = [f"{stages[stage]:.3f}" for stage in STAGES]
        lines.append(f"| {label}, first in a process | {' | '.join(cells)} | {sum(stages.values()):.3f} |")
    medians = median_load([r for data in warm for r in data["records"] if r["kind"] == "load"])
    for engine, label in ENGINE_LABELS.items():
        stages = medians[engine]
        cells = [f"{stages[stage]:.3f}" for stage in STAGES]
        lines.append(f"| {label}, repeated | {' | '.join(cells)} | {sum(stages.values()):.3f} |")
    for label, engine, guest, rounds in (("WaaS CoreCLR (Rust)", "waas-coreclr", "rust", coreclr),
                                         ("WaaS CoreCLR (MoonBit)", "waas-coreclr", "moonbit", coreclr),
                                         ("wasm3 (Rust)", "wasm3", "rust", native),
                                         ("wasm3 (MoonBit)", "wasm3", "moonbit", native),
                                         ("wasmtime (Rust)", "wasmtime", "rust", native),
                                         ("wasmtime (MoonBit)", "wasmtime", "moonbit", native)):
        values = [r["ms"] for r in pick(rounds, engine, guest) if r["kind"] == "load"]
        lines.append(f"| {label}, repeated | - | - | - | - | {statistics.median(values):.3f} |")

    for name, cells in unsupported.items():
        for column, message in cells.items():
            lines += ["", f"{column} cannot run {name}: {message}"]

    text = "\n".join(lines) + "\n"
    (RESULTS_DIR / "report.md").write_text(text, encoding="utf-8")
    print(text)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["bindings", "build", "bench", "report", "all"])
    parser.add_argument("--backend", choices=[*BACKENDS, "all"], default="il2cpp")
    parser.add_argument("--cold-runs", type=int, default=15, help="number of processes for each cold measurement")
    parser.add_argument("--rounds", type=int, default=4, help="number of times to run the harnesses alternately")
    args = parser.parse_args()

    backends = list(BACKENDS) if args.backend == "all" else [args.backend]
    if args.command == "bindings":
        generate_bindings()
    if args.command in ("build", "all"):
        build_players(backends)
    if args.command in ("bench", "all"):
        run_benchmarks(backends, args.cold_runs, args.rounds)
    if args.command in ("report", "bench", "all"):
        report(backends)


if __name__ == "__main__":
    main()
