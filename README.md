# 🗑 Valheim DB Dumper

<p align="center">
  <img src="https://i.ibb.co/DH7d4V75/VALHEIM-DB-DUMPER-LOGO.png" alt="Valheim DB Dumper Logo" width="400">
</p>

**Valheim DB Dumper** is a powerful mod designed to extract and dump internal game database structures, 3D models, textures, and prefabs directly from Valheim into your local file system.

Whether you are a modder, wiki maintainer, or data enthusiast, this tool provides a comprehensive pipeline to extract game assets effortlessly, paired with an out-of-the-box interactive web dashboard to browse your data.

Find it on [NexusMods](https://www.nexusmods.com/valheim/mods/3374) or [Thunderstore](https://thunderstore.io/c/valheim/p/N1h1lius/ValheimDBDumper/)

---

## 🚀 Features

* **Deep Data Extraction**: Exports detailed metadata for items, creatures, crafting recipes, pickable resources, and building pieces.
* **Asset Dumping**: Automatically extracts 3D meshes (`.obj`), skin textures (`.png`), and cropped UI icons from texture atlases.
* **Prefab Hierarchies**: Serializes complete GameObject transform and component hierarchies into readable JSON files.
* **Interactive Web Dashboard**: Generates a responsive HTML front-end to navigate, search, and filter your exported data locally or via a web host.
* **Highly Configurable**: Control exactly what gets dumped using command-line modifiers to skip heavy processes like 3D model baking or icon rendering.

---

## 🛠️ Installation

1. Ensure you have **BepInEx** installed for Valheim.
2. Download the latest release of **Valheim DB Dumper**.
3. Place the `ValheimDBDumper.dll` into your `BepInEx/plugins` folder.
4. Launch the game. The mod will automatically generate a configuration file located at `BepInEx/config/n1h1lius.valheimdbdumper.cfg`.

---

## 💻 Usage & Console Commands

To use the dumper, you **must be loaded into a world with your character** (the game database needs to be fully initialized in memory).

Open the in-game console (usually `F5`) and use the following command structure:

`dumpdb <category> [modifiers]`

### Available Categories

| Category | Description |
| --- | --- |
| `all` | Exports absolutely all database structures and assets. |
| `recipes` | Exports manufacturing recipes and crafting requirements exclusively. |
| `items` | Exports weapons, armor, tools, and material data. |
| `pieces` | Exports building structures, furniture, and crafting stations. |
| `creatures` | Exports animals, monsters, boss data, and drop tables. |
| `pickables` | Exports wild spawns, plants, and ground resource nodes. |

### Optional Modifiers

You can append modifiers to skip specific extraction pipelines to save time and processing power:

| Modifier | Effect |
| --- | --- |
| `--no-icon` | Bypasses texture atlas rendering and PNG extraction completely. |
| `--no-json` | Skips data mapping and JSON metadata generation. |
| `--no-prefab` | Skips prefab hierarchy mapping and JSON generation. |
| `--no-model3d` | Skips 3D model baking (`.obj`) and skin texture mapping completely. |

**Example Command:**
`dumpdb pickables --no-icon --no-model3d`

---

## 📊 The Interactive Web Dashboard

When you run an export, the mod automatically extracts two utility files into your designated export folder (default is `Desktop/ValheimDB_Export`): an `index.html` file and a `start_server.bat` file.

This front-end dashboard allows you to visually browse, search, and filter the JSON databases you just exported.

### How to View the Dashboard

Due to standard browser security policies (CORS), opening the `index.html` file directly by double-clicking it will block the JSON data from loading locally. To view your data, you have two options:

**Option A: Local Viewing (Recommended & Fastest)**

1. Navigate to your export folder.
2. Double-click the generated `start_server.bat` file.
3. This script spins up a local live server and automatically opens the dashboard in your default browser at `http://localhost:8000`.
4. **Requirement:** You must have **Python** installed on your system for the `.bat` file to work. If you do not have Python installed, you can download it from the [Official Python Website](https://www.python.org/downloads/).

**Option B: Web Hosting**

1. Upload the entire contents of your export folder (the HTML file, along with the `data`, `icons`, `models`, `prefabs`, and `textures` folders) to any standard web host or local web server software (like XAMPP or Nginx).
2. Access it via your server's URL. No Python required for this method.

---

## 📁 Exported Data Structure

Once a full dump is completed, your export directory will be organized as follows:

* **`/data/`**: Contains the categorized JSON metadata (e.g., `creatures.json`, `items.json`) mapping stats, drops, and requirements.
* **`/icons/`**: Contains `.png` files of the UI inventory icons cropped directly from the game's sprite atlases.
* **`/prefabs/`**: Contains `.json` files detailing the exact Transform hierarchy and attached Components of every GameObject.
* **`/models/`**: Contains the baked 3D meshes in `.obj` format.
* **`/textures/`**: Contains the primary skin/material textures (`_MainTex`) used by the 3D models.
* **`index.html` & `start_server.bat**`: The web application and server launcher.

---

## ⚙️ Configuration

The export behavior can be tweaked in the `n1h1lius.valheimdbdumper.cfg` file generated by BepInEx:

* **ExportFolder**: The absolute path where the dumped files will be saved. By default, it creates a `ValheimDB_Export` folder on your Desktop.
* **DebugMode**: Enables extended debug logging for development purposes.
* **OutputLog**: Toggles internal log file generation. Logging utilizes a custom color-coded internal system mapping files, methods, and stack traces.

---

### 🤝 Contributing & Support

* **Found a bug?** Open an [Issue](https://github.com/n1h1lius/Valheim-DB-Dumper/issues).
* **Want to help?** Pull Requests are welcome!
* **Support the project:** If this mod helps you or your community, consider [Buying me a coffee](https://ko-fi.com/n1h1lius).

---

<p align="center">
  Built with ☕ and ⚡ by <b>n1h1lius</b>
</p>

