# SpaceCraft / Heaps `hxd.fmt.pak` PAK format

This document describes the PAK archive format accepted by SpaceCraft's bundled `hxd.fmt.pak` loader, based on the decompiled `hxd.fmt.pak` package and the observed `res.pak` header. It is intended as an implementation
guide for a repacker.

The most important rule: file offsets stored in the header are **relative to the start of the data block**, not absolute offsets in the physical `.pak` file. During mounting the loader adds `headerSize` to every stored
`dataPosition`.

---

## 1. Loader behavior summary

The game creates a `hxd.fmt.pak.FileSystem`, loads `res.pak`, then also loads `res1.pak`, `res2.pak`, ... while those files exist, and finally installs it as the `hxd.Res` loader.

For each PAK file, the loader:

1. opens the physical file as `sys.io.FileInput`;
2. parses the archive header with `hxd.fmt.pak.Reader.readHeader()`;
3. recursively registers every file entry into a path dictionary;
4. converts every file entry's stored relative `dataPosition` into an absolute physical file offset by adding `pak.headerSize`;
5. later reads resource bytes using only `absoluteDataPosition` and `dataSize`.

The loader does **not** require files to be physically contiguous. Gaps/padding inside the data block are tolerated as long as all offsets and sizes are correct.

The loader does **not** validate the archive tail. Extra bytes after the last referenced file are tolerated.

The loader does **not** verify per-file `checksum` against file contents in the inspected code. The field is parsed and stored, but not used for integrity checks by `PakEntry.getBytes()` / `readBytes()`.

---

## 2. Primitive encoding

All multi-byte integers observed in `res.pak` and consumed by the HashLink/Haxe reader are little-endian.

Primitive types used by this format:

| Type             |     Size | Encoding                                                                                                                    |
|------------------|---------:|-----------------------------------------------------------------------------------------------------------------------------|
| Byte / UInt8     |        1 | unsigned byte value 0..255                                                                                                  |
| Int32            |        4 | little-endian signed 32-bit integer                                                                                         |
| Float64 / Double |        8 | little-endian IEEE-754 double                                                                                               |
| String           | variable | length is stored separately as one byte; bytes are read as a Haxe string, effectively UTF-8/byte-compatible for ASCII paths |

Path and file names inside the tree are not NUL-terminated. Each name is encoded as:

```text
nameLen: UInt8
nameBytes: nameLen bytes
```

This means a single path component name must fit in 255 bytes.

---

## 3. Top-level file layout

Physical archive layout:

```text
offset  size                  field
------  --------------------  ------------------------------
0x0000  3                     magic = ASCII "PAK"
0x0003  1                     version byte
0x0004  4                     headerSize: Int32 LE
0x0008  4                     dataSize: Int32 LE
0x000C  headerSize - 16       serialized file tree
H - 4   4                     marker = ASCII "DATA"
H       data block            raw resource bytes
...     optional              unused trailing bytes tolerated
```

Where:

```text
H = headerSize
```

Important: `headerSize` is the absolute offset of the first byte of the data block. The `DATA` marker therefore starts at `headerSize - 4`.

The reader logic is equivalent to:

```haxe
if input.readString(3) != "PAK"
    throw "Invalid PAK file";

pak.version = input.readByte();
pak.headerSize = input.readInt32();
pak.dataSize = input.readInt32();

var treeBytes = input.read(pak.headerSize - 16);
pak.root = new Reader(new BytesInput(treeBytes)).readFile();

if input.readString(4) != "DATA"
    throw "Corrupted PAK header";
```

Why `headerSize - 16`: after the initial 12 bytes are read, the loader reads the serialized tree, then reads the 4-byte `DATA` marker. Therefore the serialized tree length is:

```text
headerTreeSize = headerSize - 12 - 4 = headerSize - 16
```

---

## 4. Header tree format

The header tree is a recursive serialized `hxd.fmt.pak.File` tree. The root is also encoded as a regular file record, normally a directory.

Each record starts with:

```text
nameLen: UInt8
name:    nameLen bytes
flags:   UInt8
```

Known flag bits:

```text
bit 0 / 0x01: directory entry
bit 1 / 0x02: file dataPosition is stored as Float64 instead of Int32
```

Other flag bits were not used by the inspected reader.

### 4.1 Directory record

If `flags & 0x01 != 0`, the record is a directory:

```text
nameLen:       UInt8
name:          nameLen bytes
flags:         UInt8 with bit 0 set
childrenCount: Int32 LE
children:      FileRecord[childrenCount]
```

Directory records do not have `dataPosition`, `dataSize`, or `checksum`.

The loader recurses through `children` and builds full paths by joining directory names with `/`.

### 4.2 Regular file record

If `flags & 0x01 == 0`, the record is a regular file:

```text
nameLen:  UInt8
name:     nameLen bytes
flags:    UInt8 with bit 0 clear
```

Then the offset is encoded according to flag bit 1:

```text
if flags & 0x02 != 0:
    dataPosition: Float64 LE
else:
    dataPosition: Int32 LE
```

Then always:

```text
dataSize: Int32 LE
checksum: Int32 LE
```

The reader stores `dataPosition` internally as `F64` regardless of whether it was read from an Int32 or a Double.

---

## 5. Meaning of `dataPosition`

In the serialized header, `dataPosition` is **relative to the beginning of the data block**, not relative to the beginning of the physical `.pak` file.

After parsing a PAK, `FileSystem.addRec()` adjusts every file entry:

```haxe
file.dataPosition += pak.headerSize;
```

After that point, the in-memory `PakEntry.file.dataPosition` is an absolute physical offset.

Therefore the repacker must write:

```text
storedDataPosition = absoluteDataOffset - headerSize
```

Do **not** write the absolute physical offset into the header. If you do, the loader will add `headerSize` again and read from the wrong location.

Example:

```text
headerSize = 346608
actual first file starts at physical offset 346608
stored dataPosition must be 0
```

If the file starts at physical offset `350000`, the stored position must be:

```text
350000 - 346608 = 3392
```

---

## 6. Reading file contents

When a resource is requested, `PakEntry.readBytes()` behaves like this:

```haxe
var input = fs.getFile(pakFileIndex);
var pos = file.dataPosition + requestedOffset; // already absolute after addRec()
FileSeek.seek(input, pos, SeekBegin);

var maxLen = file.dataSize - requestedOffset;
var readLen = min(requestedLen, maxLen);
input.readBytes(out, outPos, readLen);
```

`PakEntry.getBytes()` similarly seeks to `file.dataPosition` and reads exactly `file.dataSize` bytes.

Consequences:

- The loader does not scan for file boundaries.
- The loader does not infer file size from the next file's offset.
- The loader trusts `dataSize` exactly.
- If `dataPosition` or `dataSize` is wrong, the resource will read wrong bytes, truncated bytes, or bytes past the intended file.

---

## 7. `dataSize` in the top-level header

The top-level `pak.dataSize` is read from bytes 8..11 of the physical file header. In the inspected loader path, it is stored in `hxd.fmt.pak.Data.dataSize`, but ordinary resource reads do not use it to validate the
physical file size or archive tail.

For a correct repacker, still write a consistent value:

```text
dataSize = size of the data block after the DATA marker
         = physicalFileSize - headerSize
```

If preserving trailing padding/tail intentionally, either:

- include the tail in `dataSize`, or
- keep `dataSize` as the span of meaningful data.

The inspected game loader appears not to care, but writing the full physical data-block size is the least surprising option.

Note: `dataSize` is Int32. For archives larger than 2 GiB this can wrap as signed Int32 in Haxe, but the inspected loader does not use it for seek/read decisions. Per-file positions handle large offsets through the
`flags & 0x02` Double encoding.

---

## 8. Large offsets and flag `0x02`

For regular files, `flags & 0x02` selects the width of `dataPosition` in the serialized header.

Rules for writing:

```text
if relativeDataPosition fits signed Int32 safely:
    clear flag 0x02
    write dataPosition as Int32 LE
else:
    set flag 0x02
    write dataPosition as Float64 LE
```

For a 16 GB archive, many files will likely require Double positions. Losing this flag, forcing Int32, or truncating offsets will corrupt reads for files located after the Int32 range.

Because the loader stores positions as `F64`, integer offsets are exact up to 2^53. A 16 GB PAK is safely representable as a Double integer.

---

## 9. Checksum field

Every regular file has a 4-byte `checksum` field after `dataSize`.

The inspected loader:

- reads the field into `File.checksum`;
- does not compare it with file bytes;
- does not reject stale or zero checksums in the inspected read path.

Therefore checksum is not a hardcoded anti-tamper guard in this loader.

For repacking, practical options are:

1. preserve original checksums for unchanged files;
2. recompute whatever checksum the original writer used, if known;
3. write `0` for changed files if only this game loader needs to read the archive.

Option 2 is best for tooling compatibility, but not required by the inspected game loading path.

Do not spend time debugging black screen issues around checksum until offsets, header size, data sizes, and `DATA` marker placement are verified.

---

## 10. Gaps, padding, and tail bytes

### 10.1 Gap between header and first file

There is no special gap between the header and the data block. The `DATA` marker ends exactly at `headerSize`, and the data block starts exactly at `headerSize`.

However, the first file's stored `dataPosition` does not have to be zero. If it is non-zero, there is padding/gap at the beginning of the data block:

```text
physical offset headerSize + 0                     padding/gap
physical offset headerSize + firstDataPosition     first file bytes
```

The loader accepts this because it only seeks to the stored file offsets.

### 10.2 Gaps between files

Gaps between files are tolerated:

```text
file A bytes
padding/gap
file B bytes
padding/gap
file C bytes
```

The loader does not require contiguous packing and does not compare adjacent offsets.

### 10.3 Archive tail

Trailing bytes after the last referenced file are tolerated by the inspected loader. It does not check that the final referenced file ends at EOF.

For a clean repack, prefer either:

- no tail: EOF is exactly `headerSize + dataSize`; or
- preserved original tail if the original archive had one and you are doing minimal binary surgery.

---

## 11. Overlay behavior with multiple PAKs

The game loads `res.pak`, then `res1.pak`, `res2.pak`, ... if present.

`FileSystem.addRec()` registers entries by path in a string map. If a later PAK contains the same path, it can replace the entry's backing file/pak index. This is overlay-like behavior.

Implications:

- A separate `res1.pak` containing only modified files may be viable if the loader sees it.
- If repacking `res.pak` itself, path uniqueness and path spelling must be preserved.
- Duplicate paths inside the same PAK are risky; the effective entry is whichever registration wins in traversal order.

---

## 12. Writer checklist for a valid repack

A repacker should perform these steps:

1. Build an in-memory tree of directories and files.
2. Decide final physical layout of file data inside the data block.
3. For each file, store:
    - `name`,
    - `isDirectory = false`,
    - `relativeDataPosition`,
    - `dataSize`,
    - `checksum`.
4. Serialize the root file tree to bytes.
5. Compute:

```text
headerSize = 12 + serializedTreeSize + 4
```

6. Write top-level header:

```text
"PAK"
version byte
headerSize Int32 LE
dataSize Int32 LE
serializedTree
"DATA"
```

7. Write data block bytes at exactly offset `headerSize`.
8. Verify every file:

```text
absoluteOffset = headerSize + relativeDataPosition
absoluteOffset >= headerSize
absoluteOffset + dataSize <= physicalFileSize
bytes at absoluteOffset are the intended file bytes
```

9. Verify marker placement:

```text
file[0..2] == "PAK"
file[headerSize - 4 .. headerSize - 1] == "DATA"
```

10. Verify root tree can be parsed by the same recursive reader.

---

## 13. Minimal validation algorithm

A validator for this format should check:

```text
read magic; must be "PAK"
read version byte
read headerSize; must be >= 16
read dataSize
read exactly headerSize - 16 bytes as tree
read marker; must be "DATA"
parse root FileRecord from tree bytes
ensure parser consumed all tree bytes, or at least report leftover bytes
for each regular file:
    relativePosition >= 0
    dataSize >= 0
    absolute = headerSize + relativePosition
    absolute + dataSize <= physicalFileSize
    if relativePosition needs Double, flags must have 0x02
    if flags lacks 0x02, raw stored position must fit Int32
```

Optional checks:

```text
warn if top-level dataSize != physicalFileSize - headerSize
warn if files overlap
warn if duplicate full paths exist
warn if component name byte length > 255
warn if unknown flag bits are set
warn if checksum does not match a chosen checksum algorithm
```

---

## 14. Common repacker bugs that produce black screen

Most likely failure modes:

1. **Absolute offsets written into header.**
    - Symptom: loader adds `headerSize` again and reads wrong bytes.
    - Fix: write `absoluteOffset - headerSize`.

2. **Offsets not recomputed after changing one file size.**
    - Symptom: all resources after the changed file are shifted and corrupt.
    - Fix: recompute every following file's relative position or preserve original positions with gaps when possible.

3. **Wrong `headerSize`.**
    - Symptom: loader reads tree incorrectly or expects `DATA` at the wrong location.
    - Fix: `headerSize = 12 + treeBytes.size + 4`.

4. **`DATA` marker not exactly at `headerSize - 4`.**
    - Symptom: immediate `Corrupted PAK header` if the exception is visible; otherwise startup failure.

5. **Large offsets serialized as Int32.**
    - Symptom: files beyond 2 GiB/Int32 range read from wrong locations.
    - Fix: set flag `0x02` and write Float64 for large relative positions.

6. **Wrong endian.**
    - Symptom: absurd header size, data size, child counts, or offsets.
    - Fix: write Int32 and Double little-endian.

7. **`dataSize` per file not updated.**
    - Symptom: resource reads truncated data or extra bytes from the next resource.

8. **Tree name lengths written as Int32 instead of UInt8.**
    - Symptom: tree parser desynchronizes immediately.

9. **Directory flag missing on directories.**
    - Symptom: reader interprets child count bytes as data offset/size/checksum.

10. **Root record missing.**
    - Symptom: reader treats the first top-level file as root; path tree becomes invalid.

---

## 15. Recommended repacking strategies

### Strategy A: Full rebuild

Rebuild header tree and data block from scratch.

Pros:

- simple mental model;
- all offsets can be compact and sequential;
- no stale gaps.

Requirements:

- correctly serialize root tree;
- recompute all relative positions;
- handle Double offsets for large archives;
- update all file sizes.

### Strategy B: Minimal in-place patch

Preserve the original header and data layout as much as possible. Replace only files whose new size fits into the original allocated span or known gap.

Pros:

- fewer offsets change;
- less risk with large files and ordering.

Requirements:

- know the available space before the next referenced file;
- update `dataSize` if changed;
- avoid overflowing into following file data;
- if the new file is larger than the original span, either move following files and update offsets, or append the new data to the tail and update only this file's offset/size.

### Strategy C: Overlay PAK

Create `res1.pak` with only changed files, relying on the game's `res.pak`, `res1.pak`, `res2.pak`, ... loading behavior.

Pros:

- avoids modifying the large original archive;
- small archive;
- simpler data layout.

Risks:

- must confirm later PAK entries override earlier entries for the same path in this exact loader path;
- must preserve exact resource paths.

---

## 16. Reference pseudocode: parse header

```kotlin
fun readPak(input: Input): Pak {
    require(input.readAscii(3) == "PAK")

    val version = input.readU8()
    val headerSize = input.readI32LE()
    val dataSize = input.readI32LE()

    val treeSize = headerSize - 16
    require(treeSize >= 0)

    val treeBytes = input.readBytes(treeSize)
    val root = readFileRecord(ByteInput(treeBytes))

    require(input.readAscii(4) == "DATA")

    // Convert relative positions to absolute positions for reading.
    walk(root) { file ->
        if (!file.isDirectory) {
            file.absoluteDataPosition = headerSize + file.relativeDataPosition
        }
    }

    return Pak(version, headerSize, dataSize, root)
}
```

---

## 17. Reference pseudocode: parse file record

```kotlin
fun readFileRecord(input: Input): Node {
    val nameLen = input.readU8()
    val name = input.readString(nameLen)
    val flags = input.readU8()

    val isDirectory = (flags and 0x01) != 0
    val wideOffset = (flags and 0x02) != 0

    if (isDirectory) {
        val count = input.readI32LE()
        val children = ArrayList<Node>(count)
        repeat(count) {
            children += readFileRecord(input)
        }
        return DirectoryNode(name, flags, children)
    }

    val relativePos = if (wideOffset) {
        input.readF64LE().toLongExactOrChecked()
    } else {
        input.readI32LE().toLong()
    }

    val size = input.readI32LE()
    val checksum = input.readI32LE()

    return FileNode(name, flags, relativePos, size, checksum)
}
```

---

## 18. Reference pseudocode: write file record

```kotlin
fun writeFileRecord(out: Output, node: Node) {
    val nameBytes = node.name.toByteArray(Charsets.UTF_8)
    require(nameBytes.size <= 255)

    out.writeU8(nameBytes.size)
    out.writeBytes(nameBytes)

    if (node is DirectoryNode) {
        val flags = node.flags or 0x01
        out.writeU8(flags)
        out.writeI32LE(node.children.size)
        node.children.forEach { writeFileRecord(out, it) }
        return
    }

    node as FileNode

    val needsWide = node.relativeDataPosition < Int.MIN_VALUE ||
                    node.relativeDataPosition > Int.MAX_VALUE
    val flags = if (needsWide) node.flags or 0x02 else node.flags and 0x02.inv()

    out.writeU8(flags and 0x01.inv())

    if (needsWide) {
        out.writeF64LE(node.relativeDataPosition.toDouble())
    } else {
        out.writeI32LE(node.relativeDataPosition.toInt())
    }

    out.writeI32LE(node.dataSize)
    out.writeI32LE(node.checksum)
}
```

---

## 19. Final compatibility target

A PAK should be considered compatible with the inspected SpaceCraft loader when all of these are true:

```text
1. Magic is "PAK".
2. Top-level integers are little-endian.
3. headerSize points to the first byte of the data block.
4. Serialized tree length is exactly headerSize - 16.
5. "DATA" is exactly at headerSize - 4.
6. The header tree starts with a root FileRecord.
7. Directory records have flag 0x01 and an Int32 child count.
8. Regular file records do not have flag 0x01.
9. Regular file offsets are relative to data block start.
10. Large regular file offsets use flag 0x02 and Float64.
11. Each regular file's dataSize equals the number of bytes to read.
12. For every file, headerSize + relativeDataPosition + dataSize <= physical file size.
13. Names use one-byte length prefixes.
14. No integrity/hash check is expected from this loader path.
```

If a repacked archive satisfies these invariants but the game still hangs before menu, the next suspects are not PAK container integrity but corrupted content-level files, compressed/encoded resource payloads, or a
resource path/overlay mismatch.
