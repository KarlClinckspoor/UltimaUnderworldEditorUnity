# Tests

These projects build the editor's code outside Unity and check it against the game files.
Unity does not use them (they are outside `Assets`).

| Project | What it is |
|---|---|
| `UnityStubs` | Compile-only stand-ins for `UnityEngine`, `UnityEngine.UI`, the event system, unity-ui-extensions, FlexibleColorPicker and Newtonsoft.Json. `Texture2D` keeps its pixels and `Color`↔`Color32` rounds as Unity does; everything else does nothing. |
| `Formats` | `Assets/Scripts/Formats` alone, as .NET Standard 2.0 and C# 8: no UnityEngine and no editor code, the rules the `Underworld.Formats` asmdef (`noEngineReferences`) applies in Unity. The mod manager builds this project too. |
| `EditorBuild` | All of `Assets/Scripts` except those two folders, built against the stand-ins: a build error here is one in Unity. |
| `Legacy` | `Assets/Scripts` as it was before the formats moved (commit `b61e96a` on `uw2-support`), taken from git at build time. |
| `Formats.Tests` | xUnit tests: parity of the old and the moved code. |

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
- **Small files:** `.GR` decode and rebuild, the start square and UW.EXE's models, SKILLS.DAT,
  CMB.DAT, SHADES.DAT, BABGLOBS.DAT and the PLAYER.DAT ciphers.

A deliberate change to the moved picture, string or object code makes these tests fail.
