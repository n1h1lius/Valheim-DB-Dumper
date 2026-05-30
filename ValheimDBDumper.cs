using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.IO;
using System.Linq;


using static ValheimDBDumper.Tools.LogSystem;

namespace ValheimDBDumper
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class ValheimDBDumper : BaseUnityPlugin
    {
        // ───────── BEPINEX ─────────
        private const string ModGUID = "n1h1lius.valheimdbdumper";
        private const string ModName = "Valheim-DB-Dumper";
        private const string ModVersion = "1.0.0";
        public static ValheimDBDumper Instance { get; private set; }
        
        // ───────── CONFIG ENTRIES ─────────
        public static ConfigEntry<string> ConfigExportPath { get; private set; }
        public static ConfigEntry<bool> ConfigDebugMode { get; private set; }
        public static ConfigEntry<bool> ConfigOutputLog { get; private set; }

        // ───────── INTERNAL MOD REFERENCES ─────────
        private static int _activeExports = 0;


    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                        UNITY METHODS                                                            ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        private void Awake()
        {
            Instance = this;

            // ───────── CONFIG ENTRIES BINDS ─────────

            string defaultDesktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ValheimDB_Export");
            
            ConfigExportPath = Config.Bind(
                "General", 
                "ExportFolder", 
                defaultDesktopPath, 
                "Absolute path where the dumper files will be exported. Default: Desktop/ValheimDB_Export"
            );

            ConfigDebugMode = Config.Bind(
                "Debug", 
                "DebugMode", 
                false, 
                "Enable debug mode for development purposes."
            );

            ConfigOutputLog = Config.Bind(
                "Debug", 
                "OutputLog", 
                true, 
                "Enable logging to the log file."
            );

            // ───────── LOG SYSTEM INIT ─────────

            Init();
            SetDebugMode(ConfigDebugMode.Value);
            SetOutput(ConfigOutputLog.Value, false);

            Log($"Export folder configured at: {ConfigExportPath.Value}");
            Success($"{ModName} v{ModVersion} loaded.");

            // ───────── HARMONY PATCHER ─────────

            Harmony harmony = new Harmony(ModGUID);
            harmony.PatchAll();
        }


        // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
        // ║                                                    VALHEIM CONSOLE PATCH                                                        ║
        // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.Awake))]
        public static class Terminal_Awake_Patch
        {
            public static void Postfix(Terminal __instance)
            {
                new Terminal.ConsoleCommand("dumpdb", "Exports game database with filters", (Terminal.ConsoleEventArgs args) =>
                {
                    // If no valid arguments are passed, display the interactive help menu
                    if (args.Args == null || args.Args.Length < 2)
                    {
                        PrintHelp(args);
                        return;
                    }

                    if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
                    {
                        args.Context.AddString("<color=red>[Valheim DBDumper] Error: You must be inside a world with your character to dump data.</color>");
                        return;
                    }

                    try
                    {
                        // Retrieve the directory from the BepInEx configuration file
                        string exportRootFolder = ConfigExportPath.Value;
                        if (!Directory.Exists(exportRootFolder))
                        {
                            Directory.CreateDirectory(exportRootFolder);
                        }

                        // Export HTML and BAT files
                        ExtractEmbeddedTools(exportRootFolder);
                    

                        // Parse arguments and flags safely ignoring case sensitivity
                        string mainTarget = args.Args[1].ToLower(); // The main category parameter (e.g., 'all', 'recipes')

                        _activeExports = (mainTarget == "all") ? 5 : 1;
                        
                        bool exportIcons = !args.Args.Any(a => a.Equals("--no-icon", StringComparison.OrdinalIgnoreCase));
                        bool exportJson = !args.Args.Any(a => a.Equals("--no-json", StringComparison.OrdinalIgnoreCase));
                        bool exportPrefab = !args.Args.Any(a => a.Equals("--no-prefab", StringComparison.OrdinalIgnoreCase));
                        bool exportModel3d = !args.Args.Any(a => a.Equals("--no-model3d", StringComparison.OrdinalIgnoreCase));
                        
                        if (mainTarget == "all") {Log("Exporting all database structures...");}

                        // Route the execution target
                        if (mainTarget == "all" || mainTarget == "recipes")
                        {
                            if (mainTarget != "all") {Log("Exporting manufacturing recipes...");}
                            Instance.StartCoroutine(RecipeExporter.ExportRecipesCoroutine(exportRootFolder, args, exportJson, exportIcons, () => CheckFinished(args)));
                        }

                        if (mainTarget == "all" || mainTarget == "items")
                        {
                            if (mainTarget != "all") {Log("Exporting items...");}
                            Instance.StartCoroutine(ItemsExporter.ExportItemsCoroutine(exportRootFolder, args, exportJson, exportIcons, exportPrefab, exportModel3d, () => CheckFinished(args)));
                        }

                        if (mainTarget == "all" || mainTarget == "pieces")
                        {
                            if (mainTarget != "all") {Log("Exporting pieces...");}
                            Instance.StartCoroutine(PiecesExporter.ExportPiecesCoroutine(exportRootFolder, args, exportJson, exportIcons, () => CheckFinished(args)));
                        }

                        if (mainTarget == "all" || mainTarget == "creatures")
                        {
                            if (mainTarget != "all") {Log("Exporting creatures...");}
                            Instance.StartCoroutine(CreaturesExporter.ExportCreaturesCoroutine(exportRootFolder, args, exportJson, exportIcons, exportPrefab, exportModel3d, () => CheckFinished(args)));
                        }

                        // NEW: Router pointing to our brand new Pickables module
                        if (mainTarget == "all" || mainTarget == "pickables")
                        {
                            if (mainTarget != "all") {Log("Exporting pickables...");}
                            Instance.StartCoroutine(PickablesExporter.ExportPickablesCoroutine(exportRootFolder, args, exportJson, exportIcons, exportPrefab, exportModel3d, () => CheckFinished(args)));
                        }
                        else if (mainTarget != "all" && mainTarget != "recipes" && mainTarget != "items" && mainTarget != "pieces" && mainTarget != "creatures" && mainTarget != "pickables")
                        {
                            args.Context.AddString($"<color=red>[Valheim DBDumper] Unknown category '{mainTarget}'.</color>");
                            PrintHelp(args);
                        }
                    }
                    catch (Exception ex)
                    {
                        args.Context.AddString($"<color=red>[Valheim DBDumper] Critical error: {ex.Message}</color>");
                        Error($"Dump failed: {ex}");
                    }
                });
            }

        // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
        // ║                                                       INTERNAL METHODS                                                          ║
        // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

            private static void PrintHelp(Terminal.ConsoleEventArgs args)
            {
                args.Context.AddString("");
                args.Context.AddString("<color=yellow>=== Valheim DBDumper - Help Panel ===</color>");
                args.Context.AddString("");
                args.Context.AddString("Usage: <color=#00FFD9>dumpdb</color> <color=green><category></color> <color=orange>[modifiers]</color>");
                args.Context.AddString("");
                args.Context.AddString("<b>Available Categories:</b>");
                args.Context.AddString("");
                args.Context.AddString("  <color=green>all</color> - Exports absolutely all database structures.");
                args.Context.AddString("  <color=green>recipes</color> - Exports manufacturing recipes exclusively.");
                args.Context.AddString("  <color=green>items</color> - Exports weapons, armor, and material data.");
                args.Context.AddString("  <color=green>pieces</color> - Exports building structures and stations.");
                args.Context.AddString("  <color=green>creatures</color> - Exports animals, monsters, and boss drop tables.");
                args.Context.AddString("  <color=green>pickables</color> - Exports wild spawns, plants, and ground resource nodes.");
                args.Context.AddString("");
                args.Context.AddString("<b>Optional Modifiers:</b>");
                args.Context.AddString("");
                args.Context.AddString("  <color=orange>--no-icon</color> - Bypasses texture atlas rendering and PNG extraction completely.");
                args.Context.AddString("  <color=orange>--no-json</color> - Skips data mapping and JSON generation completely.");
                args.Context.AddString("  <color=orange>--no-prefab</color> - Skips prefab data mapping and JSON generation completely.");
                args.Context.AddString("  <color=orange>--no-model3d</color> - Skips Models + Textures mapping and data extraction completely.");
                args.Context.AddString("");
                args.Context.AddString("Example: <color=#00FFD9>dumpdb</color> <color=green>pickables</color> <color=orange>--no-icon</color>");
            }

            private static void ExtractEmbeddedTools(string destinationFolder)
            {
                string[] resources = { "ValheimDBDumper.Tools.index.html", "ValheimDBDumper.Tools.start_server.bat" };
                Directory.CreateDirectory(destinationFolder);

                foreach (string res in resources)
                {
                    using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(res))
                    {
                        if (stream == null) continue;
                        string fileName = res.Substring(res.LastIndexOf('.') + 1).Contains("html") ? "index.html" : "start_server.bat";
                        string outputPath = Path.Combine(destinationFolder, fileName);
                        
                        using (FileStream fileStream = new FileStream(outputPath, FileMode.Create))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
            }

            private static void CheckFinished(Terminal.ConsoleEventArgs args)
            {
                _activeExports--;
                if (_activeExports <= 0)
                {
                    args.Context.AddString("<color=green>[Valheim DBDumper] Process Completed Successfuly!</color>");
                    Success("All export tasks completed successfully.");
                }
            }
        }
    }
}
