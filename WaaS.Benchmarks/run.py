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

BACKENDS = {"mono": ("Mono", "BuildMono"), "il2cpp": ("IL2CPP", "BuildIl2Cpp")}
COLD_MODES = ["cold-waas", "cold-lua", "cold-lua-bytecode", "cold-lua-csharp"]
ENGINE_LABELS = {"waas": "WaaS", "xlua": "xLua", "xlua-bytecode": "xLua (bytecode)", "lua-csharp": "Lua-CSharp"}
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
    command = [unity_editor(), "-batchmode", "-nographics", "-quit", "-projectPath", UNITY_PROJECT,
               "-executeMethod", f"WaaS.Benchmarks.Editor.BenchmarkBuild.{method}", "-logFile", log]
    try:
        run(command)
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
    target_dir = BUILD_DIR / "guest"
    run(["cargo", "build", "--release", "--target", "wasm32-unknown-unknown", "--target-dir", target_dir],
        cwd=ROOT / "guest", env=env)
    module = target_dir / "wasm32-unknown-unknown" / "release" / "waas_benchmark_guest.wasm"
    STREAMING_ASSETS.mkdir(parents=True, exist_ok=True)
    run(["wasm-tools", "component", "new", module, "-o", STREAMING_ASSETS / "benchmark.wasm"], env=env)
    shutil.copy(ROOT / "lua" / "benchmark.lua", STREAMING_ASSETS / "benchmark.lua")


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


def run_benchmarks(backends, cold_runs):
    for backend in backends:
        directory = RESULTS_DIR / backend
        shutil.rmtree(directory, ignore_errors=True)
        for mode in COLD_MODES:
            print(f"{backend}: {mode} x {cold_runs}", flush=True)
            for i in range(cold_runs):
                run_player(backend, mode, directory / f"{mode}-{i}.json")
        print(f"{backend}: warm", flush=True)
        run_player(backend, "warm", directory / "warm.json")


def median_load(records):
    """Returns {engine: {stage: median of the stage}} from load records."""
    values = {}
    for record in records:
        values.setdefault(record["engine"], {}).setdefault(record["stage"], []).append(record["ms"])
    return {engine: {stage: statistics.median(v) for stage, v in stages.items()} for engine, stages in values.items()}


def load_table(medians):
    lines = ["| Engine | Engine init | Load | Instantiate | First call | Total |", "|---|---:|---:|---:|---:|---:|"]
    for engine, label in ENGINE_LABELS.items():
        stages = medians[engine]
        cells = [f"{stages[stage]:.3f} ms" for stage in STAGES]
        lines.append(f"| {label} | {' | '.join(cells)} | {sum(stages.values()):.3f} ms |")
    return lines


def exec_table(records, per_call):
    unit = "ns/call" if per_call else "ms"
    lines = [f"| Workload | n | WaaS ({unit}) | xLua ({unit}) | Lua-CSharp ({unit}) | xLua / WaaS | Lua-CSharp / WaaS |",
             "|---|---:|---:|---:|---:|---:|---:|"]
    by_name = {}
    for record in records:
        by_name.setdefault(record["name"], {})[record["engine"]] = record
    for name, engines in by_name.items():
        n = engines["waas"]["n"]
        scale = 1e6 / n if per_call else 1
        waas, xlua, lua_csharp = (statistics.median(engines[e]["samples"]) * scale
                                  for e in ("waas", "xlua", "lua-csharp"))
        lines.append(f"| {name} | {n:,} | {waas:,.1f} | {xlua:,.1f} | {lua_csharp:,.1f} "
                     f"| {xlua / waas:.2f} | {lua_csharp / waas:.2f} |")
    return lines


def report(backends):
    lines = []
    for backend in backends:
        directory = RESULTS_DIR / backend
        warm = json.loads((directory / "warm.json").read_text(encoding="utf-8-sig"))
        cold_files = [p for p in directory.glob("cold-*.json")]
        cold_records = []
        for path in cold_files:
            cold_records += json.loads(path.read_text(encoding="utf-8-sig"))["records"]
        records = warm["records"]
        sizes = warm["sizes"]

        lines += [f"## {BACKENDS[backend][0]}", "",
                  f"Unity {warm['unity']}, {warm['cpu']}, {warm['os']}, development build: {warm['development']}", "",
                  f"Script size: component {sizes['component']:,} bytes, "
                  f"Lua source {sizes['lua']:,} bytes, Lua bytecode {sizes['luac']:,} bytes", "",
                  f"### Load, first time in a process (median of {len(cold_files) // len(COLD_MODES)} processes)", ""]
        lines += load_table(median_load(cold_records))
        lines += ["", "### Load, repeated in a process", ""]
        lines += load_table(median_load([r for r in records if r["kind"] == "load"]))
        lines += ["", "### Compute", ""]
        lines += exec_table([r for r in records if r.get("group") == "compute"], False)
        lines += ["", "### Script calls host", ""]
        lines += exec_table([r for r in records if r.get("group") == "ffi"], True)
        lines += ["", "### Host calls script", ""]
        lines += exec_table([r for r in records if r.get("group") == "callback"], True)
        lines.append("")

    text = "\n".join(lines)
    (RESULTS_DIR / "report.md").write_text(text, encoding="utf-8")
    print(text)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=["bindings", "build", "bench", "report", "all"])
    parser.add_argument("--backend", choices=[*BACKENDS, "all"], default="all")
    parser.add_argument("--cold-runs", type=int, default=15, help="number of processes for each cold measurement")
    args = parser.parse_args()

    backends = list(BACKENDS) if args.backend == "all" else [args.backend]
    if args.command == "bindings":
        generate_bindings()
    if args.command in ("build", "all"):
        build_players(backends)
    if args.command in ("bench", "all"):
        run_benchmarks(backends, args.cold_runs)
    if args.command in ("report", "bench", "all"):
        report(backends)


if __name__ == "__main__":
    main()
