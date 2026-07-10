#!/usr/bin/env python3
"""
create_unitypackage.py — Build an AssimpNetter.unitypackage from the UPM package tree.

Usage:
    python3 tools/create_unitypackage.py [--output <path>] [--assets-root <prefix>]

The script reads every Unity .meta file in UnityPlugin/UPM/, extracts the GUID,
and packs the corresponding asset into a .unitypackage (gzip-compressed tar archive).

Requirements: Python 3.8+, no third-party dependencies.

The .unitypackage format:
    <root>.tar.gz
        <guid>/
            asset          — binary content of the asset (omitted for folders)
            asset.meta     — copy of the .meta file
            pathname       — text file: asset path relative to Assets/ (e.g. Plugins/AssimpNetter/…)
"""

import argparse
import hashlib
import io
import os
import sys
import tarfile
import time

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
UPM_ROOT  = os.path.join(REPO_ROOT, "UnityPlugin", "UPM")

# The prefix used for pathnames inside the .unitypackage.
DEFAULT_ASSETS_ROOT = "Assets/Plugins/AssimpNetter"


def parse_guid(meta_path: str) -> str:
    """Extract the 'guid: <hex>' line from a Unity .meta file."""
    with open(meta_path, "r", encoding="utf-8") as fh:
        for line in fh:
            stripped = line.strip()
            if stripped.startswith("guid:"):
                return stripped.split(":", 1)[1].strip()
    raise ValueError(f"No guid found in {meta_path}")


def add_text_member(tar: tarfile.TarFile, name: str, content: str, mtime: float):
    data = content.encode("utf-8")
    info = tarfile.TarInfo(name=name)
    info.size  = len(data)
    info.mtime = int(mtime)
    tar.addfile(info, io.BytesIO(data))


def add_binary_member(tar: tarfile.TarFile, name: str, src_path: str, mtime: float):
    info      = tarfile.TarInfo(name=name)
    info.size  = os.path.getsize(src_path)
    info.mtime = int(mtime)
    with open(src_path, "rb") as fh:
        tar.addfile(info, fh)


def collect_entries(upm_root: str, assets_prefix: str):
    """
    Yield (guid, asset_path_or_None, meta_path, unity_pathname) for every
    tracked asset (file + its .meta) and directory (folder .meta only).
    """
    for dirpath, dirnames, filenames in os.walk(upm_root):
        dirnames.sort()
        filenames.sort()

        # Yield folder entries (folders have a .meta but no 'asset' member).
        rel_dir = os.path.relpath(dirpath, upm_root)
        if rel_dir != ".":
            folder_meta = dirpath + ".meta"
            if os.path.isfile(folder_meta):
                unity_path = assets_prefix + "/" + rel_dir.replace(os.sep, "/")
                yield parse_guid(folder_meta), None, folder_meta, unity_path

        for fname in filenames:
            if fname.endswith(".meta"):
                continue  # meta files are emitted alongside their asset

            asset_path = os.path.join(dirpath, fname)
            meta_path  = asset_path + ".meta"

            if not os.path.isfile(meta_path):
                print(f"  WARNING: No .meta for {asset_path}, skipping.", file=sys.stderr)
                continue

            rel_asset   = os.path.relpath(asset_path, upm_root)
            unity_path  = assets_prefix + "/" + rel_asset.replace(os.sep, "/")
            guid        = parse_guid(meta_path)

            yield guid, asset_path, meta_path, unity_path


def build_package(output_path: str, assets_prefix: str):
    now = time.time()

    # Collect all entries first so we can warn about duplicates.
    entries = list(collect_entries(UPM_ROOT, assets_prefix))

    seen_guids = {}
    for guid, asset_path, meta_path, unity_path in entries:
        if guid in seen_guids:
            print(
                f"  ERROR: Duplicate GUID {guid}\n"
                f"    {seen_guids[guid]}\n"
                f"    {unity_path}",
                file=sys.stderr,
            )
            sys.exit(1)
        seen_guids[guid] = unity_path

    os.makedirs(os.path.dirname(os.path.abspath(output_path)), exist_ok=True)

    with tarfile.open(output_path, "w:gz", compresslevel=6) as tar:
        for guid, asset_path, meta_path, unity_path in entries:
            # asset.meta
            add_binary_member(tar, f"{guid}/asset.meta", meta_path, now)

            # pathname
            add_text_member(tar, f"{guid}/pathname", unity_path + "\n", now)

            # asset (omitted for directory entries)
            if asset_path is not None:
                add_binary_member(tar, f"{guid}/asset", asset_path, now)

    print(f"Created: {output_path}  ({os.path.getsize(output_path) // 1024} KB, {len(entries)} assets)")


def main():
    parser = argparse.ArgumentParser(description="Build AssimpNetter.unitypackage")
    parser.add_argument(
        "--output", "-o",
        default=os.path.join(REPO_ROOT, "AssimpNetter.unitypackage"),
        help="Output path for the .unitypackage file (default: <repo_root>/AssimpNetter.unitypackage)",
    )
    parser.add_argument(
        "--assets-root",
        default=DEFAULT_ASSETS_ROOT,
        help=f"Unity Assets/ sub-path prefix (default: {DEFAULT_ASSETS_ROOT})",
    )
    args = parser.parse_args()

    print(f"Building .unitypackage from {UPM_ROOT}")
    build_package(args.output, args.assets_root)


if __name__ == "__main__":
    main()
