# 🎒 BackpackMod for Schedule I

A storage expansion modification for Schedule I available on [Steam](https://store.steampowered.com/app/3164500/Schedule_I/).

## Features

- **Portable Storage Solution** – Carry extra items with you anywhere in the game.
- **Persistent Saving** – Backpack contents are now saved between game sessions.
- **Easy Access** – Open the backpack with a single keypress (default is **B**, ~~configurable via a `config.ini` file~~).
- **Fixed Layout** – Always organized in a 4×3 grid (12 slots); saved data is remapped to fit this layout.
- **Seamless Integration** – Uses the game’s native storage UI.
- **Lightweight Implementation** – Minimal impact on performance.

## Installation

1. Ensure [MelonLoader](https://melonwiki.xyz/wiki/MelonLoader) is installed on your game.
2. Download the `BackpackMod.dll` file.
3. Place the DLL in your Schedule I Mods folder.
4. ~~*(Optional)* Adjust the key binding by editing the `config.ini` file located in the “BackpackMod” folder (which is automatically created next to the DLL).~~
5. Launch the game.

## Usage Guide

- **Toggle Backpack:** Press the configured key (default: **B**) to open or close the backpack interface.
- **Item Management:** Drag and drop items between your main inventory and the backpack.

## Known Issues

- **Missing configuration:** There is no way to configure the key binding for opening the backpack. The default is **B**.

## Credits & Collaboration

- **Developer:** D-Kay
- **Special Thanks:** Tugakit for their initial work and idea; Bread-chan for their contributions to the original mod.

Want to help improve the mod? Feel free to reach out on Discord or contribute via this repository.

## Previous Versions

- **Version 1.5.0** – Redesigned the storage system to support saving backpack data for all players in multiplayer.
- **Version 1.4.0** – Reworked all code to use a custom storage system; made saving work (just the host) in multiplayer.
- **Version 1.2.0** – Simplified code to focus on UI and slot functionality; added in-game warnings about potential item loss.
- **Version 1.1.0** – Improved serialization and packaging type discovery.
- **Version 1.0.0** – Initial release.

---

Thank you for downloading BackpackMod!

**Remember:** Always back up your save files before installing any mods.
