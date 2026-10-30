"""Sets or checks the version shared by WaaS and the distributions derived from it.

WaaS and its derived distributions (the NuGet packages, the Unity packages, the Rust crates, the MoonBit module and the
extension of VS Code) are released together with the same version.

    python scripts/version.py show
    python scripts/version.py set 0.3.0
    python scripts/version.py check 0.3.0    # fails if any distribution has another version

The version of the NuGet packages is given by the release tag at build time, so it has no file to update.
"""

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

# package.json of the Unity packages and the extension of VS Code
PACKAGE_JSONS = [
    "WaaS.Unity/Packages/com.ruccho.waas/package.json",
    "WaaS.Unity/Packages/com.ruccho.wukuro/package.json",
    "vscode-waas/package.json",
]

# [package] version of a crate, [workspace.package] version of a workspace, or version of a MoonBit module,
# which are written in the same form
CARGO_VERSIONS = [
    "WaaS/wit2waas/Cargo.toml",
    "Wukuro/Cargo.toml",
    "Wukuro/moonbit/wukuro/moon.mod",
]

# dependencies between the crates released together
CARGO_DEPENDENCIES = [
    ("Wukuro/crates/wukuro/Cargo.toml", "wukuro-macro"),
    ("Wukuro/crates/wukuro-dialog/Cargo.toml", "wukuro"),
]

# dependencies between the Unity packages released together
UNITY_DEPENDENCIES = [
    ("WaaS.Unity/Packages/com.ruccho.wukuro/package.json", "com.ruccho.waas"),
]

CARGO_VERSION = re.compile(r'^(version\s*=\s*")([^"]*)(")', re.M)


def cargo_dependency(name):
    return re.compile(r'^(' + re.escape(name) + r'\s*=\s*\{[^}\n]*version\s*=\s*")([^"]*)(")', re.M)


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def write(path, text):
    (ROOT / path).write_bytes(text.encode("utf-8"))


def collect():
    """Returns (description, version) of every place that has the version."""
    versions = []
    for path in PACKAGE_JSONS:
        versions.append((path, json.loads(read(path))["version"]))
    for path, name in UNITY_DEPENDENCIES:
        versions.append((f"{path} ({name})", json.loads(read(path))["dependencies"][name]))
    for path in CARGO_VERSIONS:
        versions.append((path, CARGO_VERSION.search(read(path)).group(2)))
    for path, name in CARGO_DEPENDENCIES:
        versions.append((f"{path} ({name})", cargo_dependency(name).search(read(path)).group(2)))
    return versions


def set_version(version):
    for path in PACKAGE_JSONS:
        text = read(path)
        text, count = re.subn(r'("version"\s*:\s*")[^"]*(")', r"\g<1>" + version + r"\g<2>", text, count=1)
        assert count == 1, path
        write(path, text)
    for path, name in UNITY_DEPENDENCIES:
        text = read(path)
        text, count = re.subn(r'("' + re.escape(name) + r'"\s*:\s*")[^"]*(")', r"\g<1>" + version + r"\g<2>", text,
                              count=1)
        assert count == 1, (path, name)
        write(path, text)
    for path in CARGO_VERSIONS:
        text, count = CARGO_VERSION.subn(r"\g<1>" + version + r"\g<3>", read(path), count=1)
        assert count == 1, path
        write(path, text)
    for path, name in CARGO_DEPENDENCIES:
        text, count = cargo_dependency(name).subn(r"\g<1>" + version + r"\g<3>", read(path), count=1)
        assert count == 1, (path, name)
        write(path, text)


def main(args):
    if len(args) == 1 and args[0] == "show":
        for place, version in collect():
            print(f"{version}\t{place}")
        return 0
    if len(args) == 2 and args[0] == "set":
        set_version(args[1])
        return main(["check", args[1]])
    if len(args) == 2 and args[0] == "check":
        # a tag such as "v0.3.0" is also accepted
        expected = args[1][1:] if args[1].startswith("v") else args[1]
        mismatched = [(place, version) for place, version in collect() if version != expected]
        for place, version in mismatched:
            print(f"version mismatch: {place} has {version}, expected {expected}", file=sys.stderr)
        return 1 if mismatched else 0
    print(__doc__, file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
