# Tests

These projects build the editor's code outside Unity and check it against the game files.
Unity does not use them (they are outside `Assets`).

| Project | What it is |
|---|---|
| `UnityStubs` | Compile-only stand-ins for `UnityEngine`, `UnityEngine.UI`, the event system, unity-ui-extensions, FlexibleColorPicker and Newtonsoft.Json. `Texture2D` keeps its pixels and `Color`↔`Color32` rounds as Unity does; everything else does nothing. |
| `Formats` | `Assets/Scripts/Formats` alone, as .NET Standard 2.0 and C# 8: no UnityEngine and no editor code, the rules the `Underworld.Formats` asmdef (`noEngineReferences`) applies in Unity. The mod manager builds this project too. |
| `Modding` | `Assets/Scripts/Modding` alone (mod packages: diff, apply, executables, saves), on top of `Formats`, under the rules of the `Underworld.Modding` asmdef. The mod manager builds this project too. |
| `EditorBuild` | All of `Assets/Scripts` except those two folders, built against the stand-ins: a build error here is one in Unity. |
| `Legacy` | `Assets/Scripts` as it was before the formats moved (commit `b61e96a` on `uw2-support`), taken from git at build time. |
| `Formats.Tests` | xUnit tests: parity of the old and the moved code (top folder), saving only what changed (`SaveChangedOnlyTests`), the built-in 3D models (`ExeModelTests`), and the mod packages (`Modding/`). |

## Running

```
dotnet test Tests/Formats.Tests
```

The game files come from `UW_GAMEDATA` (a folder holding `UW` and/or `UW2`, each with `DATA`,
`CRIT` and the executable), or else from the repository's `gamedata` folder. A game that is
not there is skipped, and with neither the data tests do nothing (they still build everything).
Each test copies the game folder to the temporary folder first, puts the editor's `.BKP`
backups back in place of the files they back up, and works on the copy.

## What the parity tests check

The formats moved from Unity-dependent files into `Assets/Scripts/Formats` (asmdef
`Underworld.Formats`). The old code (`legacy::`) and the new (`current::` and the Formats
assembly) run side by side on every file of both games:

- **Moved files:** each is byte-for-byte the old file, and its `.meta` (GUID) is the same.
  Classes cut out of a bigger file (`HuffmanNode`, `StringBlock`, `AdditionalTextureInfo`, the
  `ObjectData` family) appear in the old file as they are, apart from the documented edits.
- **STRINGS.PAK:** the same tree, blocks and strings; the same bytes written; every string
  encodes the same; the written file reads back to the same strings.
- **Pictures:** the same offsets and raw pictures for every `.GR`/`.TR`; every 4-bit picture
  of `OBJECTS.GR` decodes, re-encodes (RLE) and writes the same; the same palettes; and the
  editor's whole `LoadTextures` gives the same textures, pixel for pixel.
- **Object data:** `LoadObjectData` gives the same OBJECTS.DAT/COMOBJ.DAT/critter-art data,
  and `SaveObjectDatas` writes the same files.
- **Archives and levels:** every block of every `.ARK` reads the same, rewrites the same (and
  as the original file), LZSS encodes the same, and every level builds the same block.
- **Small files:** `.GR` decode and rebuild, the start square, SKILLS.DAT,
  CMB.DAT, SHADES.DAT, BABGLOBS.DAT and the PLAYER.DAT ciphers.

A deliberate change to the moved picture, string or object code makes these tests fail.

## What the save tests check (`SaveChangedOnlyTests`)

Saving writes only what changed, and what it writes is what the old writer wrote:

- The palette table read once into memory gives the old colour-to-index results for every UW1
  picture, and the small copies (W16, F16) are sampled as the old `ScaleDownTexture` did.
- A save with no edits leaves every picture file and TERRAIN.DAT as it was, with no `.BKP`, in
  both games, twice over.
- A replaced UW1 wall and floor are patched into W64/W16 and F32/F16 with the old writer's bytes,
  every other byte stays, they read back, and the next save writes nothing more. A replaced door
  rebuilds only its picture in DOORS.GR. An added wall rewrites the pair whole (one more
  picture), after which saves patch again.
- STRINGS.PAK is not written when no string changed. A changed string writes the same file as
  the old writer, and the faster encoder gives the old bytes for every string of both games.

## What the model tests check (`ExeModelTests`)

`Uw1ExeModels` changed after the move, so it has tests of its own instead of parity, on both
executables: every face's triangles cover it exactly (their areas, seen along the face's normal,
add up to the face's), the ankh's concave faces would be overfilled by a fan, the compact quads
(nodes A0h, A2h) take point numbers, and every other face is as the old decoder read it.

## What the mod package tests check (`Formats.Tests/Modding`)

Each test changes a copy of a game the way the editor would, exports the difference with
`ModDiff`/`ModBuilder`, writes the `.uwmod` and reads it back, applies it to the untouched
copy and requires the changed files back byte for byte (`ModBuilder.Verify`):

- a level edit is one `ark-blocks` block, a level mod needing a new game, made from the GOG set;
- string edits (changed, added, a new block), a same-size `.TR` picture, a smaller `OBJECTS.GR`
  picture and an OBJECTS.DAT byte are `strings`, `images` and `bytes`: a content mod;
- new, removed and resized files are `file` components;
- the start square is an `exe-param`, another byte an `exe-bytes` patch; a patch inside an
  overlay follows the overlay when it is moved to the end of the file as UltimaHacks moves
  one, and is refused where the hack wrote;
- a conversation whose variable count changed is flagged as breaking saves;
- a mod refuses files it was not made from;
- a saved game exports, checks against DATA (and catches a changed CNV.ARK) and installs.

Also: the zip writer against `System.IO.Compression`, every picture file and STRINGS.PAK
rebuilding byte for byte, the executable's MZ/FBOV layout, and the hack file parser.
