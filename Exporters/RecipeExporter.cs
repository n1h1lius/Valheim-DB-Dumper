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

    public class RecipeData
    {
        public string prefab_name { get; set; }
        public string item_name { get; set; }
        public string item_rawName { get; set; } 
        public string crafting_station { get; set; }
        public int min_station_level { get; set; }
        public int amount { get; set; }
        public List<RequirementData> requirements { get; set; } = new List<RequirementData>();
    }

    public class RequirementData
    {
        public string item_prefab { get; set; }
        public int amount { get; set; }
        public int amount_per_level { get; set; }
    }

    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                       RECIPES EXPORTER                                                          ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

    public static class RecipeExporter
    {

        /// <summary>
        /// Exports recipe data from the game to JSON files.
        /// </summary>
        /// <param name="baseFolder">The base folder where the export files will be saved.</param>
        /// <param name="args">The console arguments used for the export process.</param>
        /// <param name="exportJson">Indicates whether to export the recipe data as JSON files.</param>
        /// <param name="exportIcons">Indicates whether to export the recipe icons.</param>
        /// <returns>An <see cref="System.Collections.IEnumerator"/> object that allows batch execution of the export process.</returns>
        public static IEnumerator ExportRecipesCoroutine(string baseFolder, Terminal.ConsoleEventArgs args, bool exportJson, bool exportIcons, Action onFinished)
        {
            string recipesFolder = Path.Combine(baseFolder, "data", "recipes");
            string iconsFolder = Path.Combine(baseFolder, "icons");

            if (exportJson) Directory.CreateDirectory(recipesFolder);
            if (exportIcons) Directory.CreateDirectory(iconsFolder);

            List<Recipe> recipeList = ObjectDB.instance.m_recipes;
            List<RecipeData> exportedRecipes = new List<RecipeData>();

            args.Context.AddString($"<color=yellow>[Valheim DBDumper] Processing {recipeList.Count} recipes...</color>");
            Info($"Processing {recipeList.Count} recipes...");

            int operationsThisFrame = 0;
            const int MaxOperationsPerFrame = 25; 

            Directory.CreateDirectory(Path.Combine(iconsFolder, "Recipes"));

            for (int i = 0; i < recipeList.Count; i++)
            {
                Recipe recipe = recipeList[i];
                if (recipe == null || recipe.m_item == null) continue;

                string prefabName = recipe.m_item.gameObject.name;

                // 1. Export Icon conditionally using atlas cropping bounds
                if (exportIcons)
                {
                    var iconsArray = recipe.m_item.m_itemData.m_shared.m_icons;
                    if (iconsArray != null && iconsArray.Length > 0 && iconsArray[0] != null)
                    {
                        SaveCroppedSpriteAsPNG(iconsArray[0], Path.Combine(iconsFolder, "Recipes", $"{prefabName}.png"));
                        operationsThisFrame++;
                    }
                }

                // 2. Map structural data fields only if requested
                if (exportJson)
                {
                    string itemName = recipe.m_item.m_itemData.m_shared.m_name;
                    string stationName = recipe.m_craftingStation != null ? recipe.m_craftingStation.m_name : "Hand";

                    RecipeData data = new RecipeData
                    {
                        prefab_name = prefabName,
                        item_name = TranslateToken(itemName),
                        item_rawName = itemName,
                        crafting_station = stationName,
                        min_station_level = recipe.m_minStationLevel,
                        amount = recipe.m_amount
                    };

                    if (recipe.m_resources != null)
                    {
                        foreach (var req in recipe.m_resources)
                        {
                            if (req == null || req.m_resItem == null) continue;

                            data.requirements.Add(new RequirementData
                            {
                                item_prefab = req.m_resItem.gameObject.name,
                                amount = req.m_amount,
                                amount_per_level = req.m_amountPerLevel
                            });
                        }
                    }
                    exportedRecipes.Add(data);
                }

                if (i > 0 && i % 150 == 0)
                {
                    args.Context.AddString($"<color=lightblue>[Valheim DBDumper] Processed {i}/{recipeList.Count} recipes...</color>");
                }

                if (operationsThisFrame >= MaxOperationsPerFrame)
                {
                    operationsThisFrame = 0;
                    yield return null; 
                }
            }

            // 3. Serialize and output serialization files
            if (exportJson && exportedRecipes.Count > 0)
            {
                string jsonOutput = JsonConvert.SerializeObject(exportedRecipes, Formatting.Indented);
                File.WriteAllText(Path.Combine(recipesFolder, "recipes.json"), jsonOutput);
            }

            args.Context.AddString($"<color=green>[Valheim DBDumper] Recipe module finished processing successfully!</color>");
            Success("Recipe module finished processing successfully!");

            onFinished?.Invoke();
        }

        // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
        // ║                                                         HELPER METHODS                                                          ║
        // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

        /// <summary>
        /// Resolves a game localization token to its localized text.
        /// </summary>
        /// <param name="token">The string token to translate, typically starting with '$'.</param>
        /// <returns>
        /// The localized string when the token can be resolved; otherwise the original token or an empty string if the input is null or empty.
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

                var localizeMethod = localizationType.GetMethod("Localize", [typeof(string)]);
                return localizeMethod?.Invoke(instance, [token]) as string ?? token;
            }
            catch (Exception ex)
            {
                Error("Error translating token: " + ex.Message);
                return token;
            }
        }

        /// <summary>
        /// Saves a sprite's cropped atlas region to a PNG file.
        /// </summary>
        /// <param name="sprite">The source sprite whose texture region will be exported.</param>
        /// <param name="outputPath">The full file path where the PNG image will be written.</param>
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
                // Silent fallback block
                Error($"Failed to export {outputPath}: {ex.Message}");
            }
        }
    }
}
