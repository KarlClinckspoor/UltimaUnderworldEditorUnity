# Ultimate Underworld Editor (krokots' Ultima Underworld editor, extended for UW2)

A level and data editor for **Ultima Underworld I: The Stygian Abyss** and **Ultima Underworld II:
Labyrinth of Worlds**, built in Unity.

krokots, a user of the GOG forums, created the editor for UW1. He
[released it in 2019](https://www.gog.com/forum/ultima_series/ultima_underworld_editor_release/post1)
and [published the source in 2024](https://www.gog.com/forum/ultima_series/ultima_underworld_editor_release/post27)
on MediaFire, a file host that may vanish, so this repository was created to keep it available.
He also made two videos, [one about traps](https://www.youtube.com/watch?v=u94tdpnkd3g) and
[one about the conversation engine](https://www.youtube.com/watch?v=fh0s3iswLYA).

Since then the editor has gained UW2 support, saved games, mod export and safer saving (0.4).

## What it can do

- **Levels:** edit tiles, textures, objects, critters, doors, triggers and traps in both games.
  UW2 levels are picked by world, and an empty slot can be made into a level.
- **Conversations:** write conversations in a small scripting language, compile them into the game,
  and play them in a simulator.
- **Game data:** object tables, terrain types, strings, keys, mantras, textures and pictures, class
  skills, item combinations and light levels.
- **Saved games:** open a save to edit its levels, rename slots, edit conversation variables, edit
  the character (attributes, skills, position), and edit UW2's schedules.
- **Tools:** an object field inspector, a search across all levels, consistency checks with fixes, the
  automap and map notes, the new-game start square, and a viewer for the built-in 3D models.
- **Mods:** export your changes as a `.uwmod` package for the mod manager.
- **Safety:** files are written only when they change, through a temporary file. The originals are
  kept as `.BKP`, older versions go in `Editor backups`, autosaves are written to separate copies,
  and moves and deletes can be undone.

## Documentation

The full manual is
[`Assets/StreamingAssets/documentation.txt`](Assets/StreamingAssets/documentation.txt), which the
editor opens from Help > Documentation. Besides the editor itself, it explains in detail how
**triggers and traps** work in both games, how they interact with conversations, schedules and
plot code, recipes for common effects (ambushes, arrow traps, levers that spawn enemies, moving
floors...), and how the games build their own set pieces.

That material comes from the shipped levels and from
[underworld-exhumed](https://github.com/abedegno/underworld-exhumed), a commented reconstruction
of both games' code.

## Using it

Download a release, run `UltimateUnderworldEditor.exe`, choose File > Open game..., then pick the
game's `UW.EXE` or `UW2.EXE`, then its `DATA` folder or a saved game folder. **Back up the game folder
first.**

## Building

- Unity **2020.3.32f1** is known to work; newer versions are untested.
- The repository doesn't contain everything from krokots' zip. For example, the Newtonsoft.Json
  DLLs must be added to the project.
- Tools > Release > Build Windows x64 builds the editor into `Builds/`.

The code can also be built and tested outside Unity: see [`Tests/README.md`](Tests/README.md).
The tests need copies of the game files, which are never committed.

## Status

- Beta: back up your game before editing.
- The randomizer tools (Sector mode, zones) are a work in progress and hidden by default.
