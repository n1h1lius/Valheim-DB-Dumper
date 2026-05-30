using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

using static ValheimDBDumper.Tools.LogSystem;

namespace ValheimDBDumper
{
    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                        DATA STRUCTURE                                                           ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
    public class PieceDataDump
    {
        public string prefab_name { get; set; }
        public string piece_name { get; set; }
        public string piece_rawName { get; set; }
        public string description { get; set; }
        public string category { get; set; }
        public string primary_tool { get; set; }
        public string required_station { get; set; }
        public List<PieceRequirementData> resources { get; set; } = new List<PieceRequirementData>();
    }

    public class PieceRequirementData
    {
        public string item_prefab { get; set; }
        public int amount { get; set; }
        public bool recover { get; set; }
    }

    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                       PIECES EXPORTER                                                           ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

    public static class PiecesExporter
    {
        /// <summary>
        /// Exports piece data from the game into JSON and optional icon files.
        /// </summary>
        /// <param name="baseFolder">The base output folder for exported piece data and icons.</param>
        /// <param name="args">The console event arguments used for progress logging.</param>
        /// <param name="exportJson">Whether to export piece metadata as JSON.</param>
        /// <param name="exportIcons">Whether to export piece icon sprites as PNG files.</param>
        /// <returns>An <see cref="System.Collections.IEnumerator"/> that yields periodically to avoid blocking game execution.</returns>
        public static IEnumerator ExportPiecesCoroutine(string baseFolder, Terminal.ConsoleEventArgs args, bool exportJson, bool exportIcons, Action onFinished)
        {
            string piecesFolder = Path.Combine(baseFolder, "data", "pieces");
            string iconsFolder = Path.Combine(baseFolder, "icons"); 

            if (exportJson) Directory.CreateDirectory(piecesFolder);
            if (exportIcons) Directory.CreateDirectory(iconsFolder);

            List<GameObject> itemPrefabs = ObjectDB.instance.m_items;
            List<PieceDataDump> exportedPieces = new List<PieceDataDump>();
            HashSet<string> processedPrefabs = new HashSet<string>();

            args.Context.AddString("<color=yellow>[Valheim DBDumper] Scanning tools for building pieces...</color>");
            Info("Scanning tools for building pieces...");
            yield return null;

            int operationsThisFrame = 0;
            const int MaxOperationsPerFrame = 25;

            foreach (GameObject itemObj in itemPrefabs)
            {
                if (itemObj == null) continue;

                ItemDrop itemDrop = itemObj.GetComponent<ItemDrop>();
                if (itemDrop == null || itemDrop.m_itemData == null) continue;

                PieceTable pieceTable = itemDrop.m_itemData.m_shared.m_buildPieces;
                if (pieceTable == null || pieceTable.m_pieces == null) continue;

                Directory.CreateDirectory(Path.Combine(iconsFolder, "Pieces"));

                string toolName = itemObj.name;

                foreach (GameObject pieceObj in pieceTable.m_pieces)
                {
                    if (pieceObj == null) continue;

                    string prefabName = pieceObj.name;
                    if (processedPrefabs.Contains(prefabName)) continue;

                    Piece piece = pieceObj.GetComponent<Piece>();
                    if (piece == null) continue;

                    // 1. Export Icon conditionally using atlas cropping
                    if (exportIcons)
                    {
                        if (piece.m_icon != null)
                        {
                            SaveCroppedSpriteAsPNG(piece.m_icon, Path.Combine(iconsFolder, "Pieces", $"{prefabName}.png"));
                            operationsThisFrame++;
                        }
                    }

                    // 2. Map data fields
                    if (exportJson)
                    {
                        string stationName = piece.m_craftingStation != null ? piece.m_craftingStation.m_name : "None";

                        PieceDataDump data = new PieceDataDump
                        {
                            prefab_name = prefabName,
                            piece_name = TranslateToken(piece.m_name),
                            piece_rawName = piece.m_name,
                            description = TranslateToken(piece.m_description),
                            category = piece.m_category.ToString(),
                            primary_tool = toolName,
                            required_station = stationName
                        };

                        // Read crafting building resource costs
                        if (piece.m_resources != null)
                        {
                            foreach (Piece.Requirement req in piece.m_resources)
                            {
                                if (req == null || req.m_resItem == null) continue;

                                data.resources.Add(new PieceRequirementData
                                {
                                    item_prefab = req.m_resItem.gameObject.name,
                                    amount = req.m_amount,
                                    recover = req.m_recover
                                });
                            }
                        }

                        exportedPieces.Add(data);
                    }

                    processedPrefabs.Add(prefabName);

                    // Frame trace tracking reporting using lightblue
                    if (processedPrefabs.Count % 50 == 0)
                    {
                        args.Context.AddString($"<color=lightblue>[Valheim DBDumper] Processed {processedPrefabs.Count} unique building pieces...</color>");
                    }

                    if (operationsThisFrame >= MaxOperationsPerFrame)
                    {
                        operationsThisFrame = 0;
                        yield return null;
                    }
                }
            }

            // 3. Serialize output
            if (exportJson && exportedPieces.Count > 0)
            {
                string jsonOutput = JsonConvert.SerializeObject(exportedPieces, Formatting.Indented);
                File.WriteAllText(Path.Combine(piecesFolder, "pieces.json"), jsonOutput);
            }

            args.Context.AddString($"<color=green>[Valheim DBDumper] Pieces module finished! Saved {exportedPieces.Count} unique structures.</color>");
            Success($"Pieces module finished! Saved {exportedPieces.Count} unique structures.");

            onFinished?.Invoke();
        }

        // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
        // ║                                                         HELPER METHODS                                                          ║
        // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        
        /// <summary>
        /// Resolves a Valheim localization token to its translated display string.
        /// </summary>
        /// <param name="token">The localization token to translate, typically starting with '$'.</param>
        /// <returns>
        /// The translated string when available; otherwise the original token, or an empty string if the input is null or empty.
        /// </returns>
        private static string TranslateToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return "";
            if (!token.StartsWith("$")) return token;

            try
            {
                Type localizationType = typeof(Terminal).Assembly.GetType("Localization") 
                    ?? Type.GetType("Valheim.Localization, assembly_valheim");
                    
                if (localizationType == null) return token;

                var instanceProp = localizationType.GetProperty("instance", BindingFlags.Static | BindingFlags.Public);
                object instance = instanceProp?.GetValue(null);
                if (instance == null) return token;

                var localizeMethod = localizationType.GetMethod("Localize", new Type[] { typeof(string) });
                return localizeMethod?.Invoke(instance, new object[] { token }) as string ?? token;
            }
            catch (Exception ex)
            {
                Error("Error translating token: " + ex.Message);
                return token;
            }
        }

        /// <summary>
        /// Crops a sprite from its source texture atlas and saves the result as a PNG file.
        /// </summary>
        /// <param name="sprite">The source sprite to crop and export.</param>
        /// <param name="outputPath">The full file path where the exported PNG should be written.</param>
        private static void SaveCroppedSpriteAsPNG(Sprite sprite, string outputPath)
        {
            if (File.Exists(outputPath)) return;

            try
            {
                Texture2D atlasTexture = sprite.texture;
                if (atlasTexture == null) return;

                Rect rect = sprite.textureRect;
                int width = (int)rect.width;
                int height = (int)rect.height;

                RenderTexture rt = RenderTexture.GetTemporary(
                    atlasTexture.width, 
                    atlasTexture.height, 
                    0, 
                    RenderTextureFormat.Default, 
                    RenderTextureReadWrite.Linear
                );
                
                Graphics.Blit(atlasTexture, rt);
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = rt;

                Texture2D croppedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                croppedTexture.ReadPixels(new Rect(rect.x, rect.y, width, height), 0, 0);
                croppedTexture.Apply();

                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(rt);

                byte[] bytes = croppedTexture.EncodeToPNG();
                File.WriteAllBytes(outputPath, bytes);

                UnityEngine.Object.Destroy(croppedTexture);
            }
            catch (Exception ex)
            {
                // Silent block
                Error("Error saving cropped sprite as PNG: " + ex.Message);
            }
        }
    }
}
