# PakTool

PakTool is a command-line tool for inspecting, extracting, validating, verifying, and rebuilding PAK archives that use the `hxd.fmt.pak` / Heaps-style archive format.

The tool was originally written as part of a community research project around **SpaceCraft**, but the format itself is not necessarily exclusive to SpaceCraft. Other games or applications built on the same technology stack may use the same or a compatible PAK format.

## Status

This is a hobby research project. It is intended for archive format analysis, modding experiments, and personal tooling.

PakTool is **not** an official Shiro Games application. It is not affiliated with, endorsed by, or supported by Shiro Games. The developer(s) of this tool are not responsible for any use of the program, including damaged files, broken game installations, lost data, account issues, or any other consequences.

Always keep backups of original archives before unpacking, modifying, or repacking them.

## Downloads and security

Builds are produced by GitHub Actions from the current source code in this repository.

You should still verify any executable file you download before running it. This applies to releases from this repository as well as any files shared elsewhere. When in doubt, build from source and compare the result with the published artifact.

## Requirements

PakTool currently targets **.NET 10**.

To build from source manually:

```bash
dotnet build -c Release
```

To run from source:

```bash
dotnet run -- <command> [args]
```

When using a published binary, replace `dotnet run --` with the executable name, for example:

```bash
PakTool <command> [args]
```

## Basic usage

```text
PakTool <command> [args]
PakTool help <command>
PakTool <command> --help
```

Available commands:

| Command | Description |
|---|---|
| `unpack` | Extracts files from a PAK archive. |
| `pack` | Builds a PAK archive from a directory. |
| `list` | Prints archive contents and extension statistics. |
| `validate` | Checks archive structure without comparing extracted files. |
| `verify` | Compares archive contents with a source directory. |
| `empty-tree` | Creates an empty file tree matching a PAK archive. |
| `version` | Prints the tool version. |

## Commands

### `unpack`

Extracts files from a PAK archive.

```text
PakTool unpack <pak-file> [--output <dir>] [--ignore-ext <ext1,ext2>] [--max-file-size <size>]
```

Arguments:

| Argument | Description |
|---|---|
| `<pak-file>` | PAK archive to unpack. |
| `--output <dir>`, `-o <dir>` | Output directory. Default: `unpacked`. |
| `--ignore-ext <ext1,ext2>` | Creates empty placeholder files for matching extensions instead of extracting their data. Extensions are comma-separated. Both `png,ogg` and `.png,.ogg` are accepted. |
| `--max-file-size <size>` | Creates empty placeholder files for entries larger than this size. Supported suffixes: `B`, `K`, `KB`, `M`, `MB`, `G`, `GB`, `T`, `TB`. Examples: `1048576`, `512K`, `20M`, `2G`. |

Examples:

```bash
PakTool unpack res.pak
PakTool unpack res.pak --output unpacked-res
PakTool unpack res.pak -o unpacked-res --ignore-ext ogg,mp4 --max-file-size 100M
```

### `pack`

Builds a PAK archive from a directory.

```text
PakTool pack <input-dir> <output-pak>
```

Arguments:

| Argument | Description |
|---|---|
| `<input-dir>` | Directory containing unpacked archive files. |
| `<output-pak>` | PAK archive to create. |

Example:

```bash
PakTool pack unpacked-res res.pak
```

The packer writes a valid archive tree, file data offsets, file sizes, and CRC32 checksums. Checksums are calculated while file data is being written, so packing does not need a separate full-file pre-scan before the archive build starts.

### `list`

Prints archive contents and extension statistics.

```text
PakTool list <pak-file> [--large]
```

Arguments:

| Argument | Description |
|---|---|
| `<pak-file>` | PAK archive to inspect. |
| `--large` | Print only files larger than 1 MiB. |

Examples:

```bash
PakTool list res.pak
PakTool list res.pak --large
```

### `validate`

Checks archive structure without comparing extracted files against an external directory.

```text
PakTool validate <pak-file>
```

Arguments:

| Argument | Description |
|---|---|
| `<pak-file>` | PAK archive to validate. |

Example:

```bash
PakTool validate res.pak
```

Use this after packing to catch structural archive problems such as invalid headers, invalid layout, overlapping file ranges, or inconsistent metadata.

### `verify`

Compares archive contents with a source directory.

```text
PakTool verify <pak-file> <input-dir>
```

Arguments:

| Argument | Description |
|---|---|
| `<pak-file>` | PAK archive to verify. |
| `<input-dir>` | Directory containing source files. |

Example:

```bash
PakTool verify res.pak unpacked-res
```

Use this after packing when you want to confirm that the archive matches the directory it was built from.

### `empty-tree`

Creates an empty file tree matching a PAK archive.

```text
PakTool empty-tree <pak-file> <output-dir>
```

Arguments:

| Argument | Description |
|---|---|
| `<pak-file>` | PAK archive to read. |
| `<output-dir>` | Directory to create or replace. |

Example:

```bash
PakTool empty-tree res.pak res-empty-tree
```

This is useful when you need the archive directory structure and file names, but not the actual file contents.

### `version`

Prints the tool version.

```text
PakTool version
```

## Typical workflow

```bash
# 1. Extract the original archive.
PakTool unpack res.pak --output unpacked-res

# 2. Modify files in unpacked-res.

# 3. Rebuild the archive.
PakTool pack unpacked-res res.pak

# 4. Validate archive structure.
PakTool validate res.pak

# 5. Optionally verify the rebuilt archive against the source directory.
PakTool verify res.pak unpacked-res
```

For real game installations, do not overwrite original files without a backup. A safer workflow is to write the rebuilt archive to a separate location first, validate it, and only then replace the target file manually.

## Format notes

The supported format is based on the `hxd.fmt.pak` archive layout used by Heaps/Haxe applications. In this format, archive entries are stored in a serialized file tree, and file data offsets are relative to the beginning of the data block rather than absolute physical file offsets.

PakTool is intentionally conservative about producing complete metadata, including CRC32 values, even if a particular observed application version does not currently validate all fields.

## Contributing

I do not monitor GitHub Issues and Pull Requests on a regular basis.

If you want to contribute or need a faster response, contact me on Discord first: `Johnson1893`

This will significantly speed up coordination and review.
