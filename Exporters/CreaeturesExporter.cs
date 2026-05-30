using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

using static ValheimDBDumper.Tools.LogSystem;


namespace ValheimDBDumper
{
    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                        DATA STRUCTURE                                                           ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

    public class PrefabNode
    {
        public string object_name { get; set; }
        public bool is_active { get; set; }
        public string layer { get; set; }
        public string tag { get; set; }
        public List<ComponentData> components { get; set; } = new List<ComponentData>();
        public List<PrefabNode> children { get; set; } = new List<PrefabNode>();
    }

    public class ComponentData
    {
        public string type { get; set; }
        public Dictionary<string, string> fields { get; set; } = new Dictionary<string, string>();
    }

    public class CreatureDataDump
    {
        public string prefab_name { get; set; }
        public string creature_name { get; set; }
        public string creature_rawName { get; set; }
        public float max_health { get; set; }
        public string faction { get; set; } 
        public bool tolerate_water { get; set; }
        public bool tolerate_fire { get; set; }
        public List<CreatureDropData> drops { get; set; } = new List<CreatureDropData>();
    }

    public class CreatureDropData
    {
        public string item_prefab { get; set; }
        public int min_amount { get; set; }
        public int max_amount { get; set; }
        public float chance_percent { get; set; } 
        public bool level_multiplier { get; set; } 
    }

    
    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                      CREATURES EXPORTER                                                         ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
    
    public static class CreaturesExporter
    {

        /// <summary>
        /// Exports creature data from the game into JSON, icon, prefab, and 3D model artifacts.
        /// </summary>
        /// <param name="baseFolder">The base output folder for exported creature assets.</param>
        /// <param name="args">The console event arguments used for progress logging.</param>
        /// <param name="exportJson">Whether to export creature metadata as JSON.</param>
        /// <param name="exportIcons">Whether to export creature icon textures.</param>
        /// <param name="exportPrefab">Whether to export prefab component structure as JSON.</param>
        /// <param name="exportModel3d">Whether to export creature meshes and skin textures.</param>
        /// <returns>An <see cref="System.Collections.IEnumerator"/> that yields periodically to avoid blocking game execution.</returns>
        public static IEnumerator ExportCreaturesCoroutine(string baseFolder, Terminal.ConsoleEventArgs args, bool exportJson, bool exportIcons, bool exportPrefab, bool exportModel3d, Action onFinished)
        {
            string creaturesFolder = Path.Combine(baseFolder, "data", "creatures");
            string iconsFolder = Path.Combine(baseFolder, "icons");
            string prefabsFolder = Path.Combine(baseFolder, "prefabs", "creatures");
            string modelsFolder = Path.Combine(baseFolder, "models", "creatures");
            string texturesFolder = Path.Combine(baseFolder, "textures", "creatures");

            if (exportJson) Directory.CreateDirectory(creaturesFolder);
            if (exportIcons) Directory.CreateDirectory(iconsFolder);
            if (exportPrefab) Directory.CreateDirectory(prefabsFolder);
            if (exportModel3d) 
            {
                Directory.CreateDirectory(modelsFolder);
                Directory.CreateDirectory(texturesFolder);
            }

            List<GameObject> allPrefabs = ZNetScene.instance.m_prefabs;
            List<CreatureDataDump> exportedCreatures = new List<CreatureDataDump>();

            args.Context.AddString($"<color=yellow>[Valheim DBDumper] Scanning {allPrefabs.Count} game prefabs for creatures...</color>");
            Info($"Scanning {allPrefabs.Count} game prefabs for creatures...");
            yield return null;

            int operationsThisFrame = 0;
            const int MaxOperationsPerFrame = 30;

            for (int i = 0; i < allPrefabs.Count; i++)
            {
                GameObject prefab = allPrefabs[i];
                if (prefab == null) continue;

                Character character = prefab.GetComponent<Character>();
                if (character == null) continue;

                string prefabName = prefab.name;
                string creatureRawName = character.m_name;

                if (prefabName.ToLower().Contains("player")) continue;

                // 1. Trophy Icons [It is the most aesthetic available option]
                CharacterDrop dropComponent = prefab.GetComponent<CharacterDrop>();
                if (exportIcons && dropComponent != null && dropComponent.m_drops != null)
                {
                    Directory.CreateDirectory(Path.Combine(iconsFolder, "Creatures"));
                    foreach (var drop in dropComponent.m_drops)
                    {
                        if (drop.m_prefab != null && drop.m_prefab.name.ToLower().Contains("trophy"))
                        {
                            ItemDrop trophyItem = drop.m_prefab.GetComponent<ItemDrop>();
                            if (trophyItem != null && trophyItem.m_itemData?.m_shared?.m_icons?.Length > 0)
                            {
                                SaveCroppedSpriteAsPNG(trophyItem.m_itemData.m_shared.m_icons[0], Path.Combine(iconsFolder, "Creatures", $"{prefabName}.png"));
                                operationsThisFrame++;
                                break; 
                            }
                        }
                    }
                }

                // 2. Creature Metadata (JSON)
                if (exportJson)
                {
                    CreatureDataDump data = new CreatureDataDump
                    {
                        prefab_name = prefabName,
                        creature_name = TranslateToken(creatureRawName),
                        creature_rawName = creatureRawName,
                        max_health = character.m_health,
                        faction = character.m_faction.ToString(),
                        tolerate_water = character.m_tolerateWater,
                        tolerate_fire = character.m_tolerateFire
                    };

                    if (dropComponent != null && dropComponent.m_drops != null)
                    {
                        foreach (CharacterDrop.Drop drop in dropComponent.m_drops)
                        {
                            if (drop == null || drop.m_prefab == null) continue;

                            data.drops.Add(new CreatureDropData
                            {
                                item_prefab = drop.m_prefab.name,
                                min_amount = drop.m_amountMin,
                                max_amount = drop.m_amountMax,
                                chance_percent = drop.m_chance * 100f, 
                                level_multiplier = drop.m_levelMultiplier
                            });
                        }
                    }

                    exportedCreatures.Add(data);
                }

                // 3. Components Structure (JSON)
                if (exportPrefab)
                {
                    string targetFile = Path.Combine(prefabsFolder, $"{prefabName}.json");
                    DumpPrefabHierarchyToJson(prefab, targetFile);
                    operationsThisFrame++;
                }

                // 4. 3D Model (.obj) + Skin Texture Extractor (.png)
                if (exportModel3d)
                {
                    string targetObjPath = Path.Combine(modelsFolder, $"{prefabName}.obj");
                    ExportPrefabToObj(prefab, targetObjPath);
                    
                    ExportCreatureSkinTexture(prefab, texturesFolder, prefabName);
                    
                    operationsThisFrame++;
                }

                if (exportedCreatures.Count % 20 == 0)
                {
                    args.Context.AddString($"<color=#00FFD9>[Valheim DBDumper] Processed {exportedCreatures.Count} unique creatures...</color>");
                }

                if (operationsThisFrame >= MaxOperationsPerFrame)
                {
                    operationsThisFrame = 0;
                    yield return null;
                }
            }

            if (exportJson && exportedCreatures.Count > 0)
            {
                string jsonOutput = JsonConvert.SerializeObject(exportedCreatures, Formatting.Indented);
                File.WriteAllText(Path.Combine(creaturesFolder, "creatures.json"), jsonOutput);
            }

            args.Context.AddString($"<color=green>[Valheim DBDumper] Creatures module finished! Geometries and skin textures successfully dumped.</color>");
            Success("Creatures module finished! Geometries and skin textures successfully dumped.");

            onFinished?.Invoke();
        }

    
        // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
        // ║                                                         HELPER METHODS                                                          ║
        // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        
        /// <summary>
        /// Extracts the creature's skin texture from its renderer hierarchy and saves it as a PNG.
        /// </summary>
        /// <param name="prefab">The creature prefab whose renderer textures will be scanned.</param>
        /// <param name="texturesFolder">The folder where the exported texture PNG should be written.</param>
        /// <param name="prefabName">The name used to construct the output file name.</param>
        private static void ExportCreatureSkinTexture(GameObject prefab, string texturesFolder, string prefabName)
        {
            string targetTexturePath = Path.Combine(texturesFolder, $"{prefabName}.png");
            if (File.Exists(targetTexturePath)) return;

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.material == null) continue;

                // This should be the main texture [Not sure] (_MainTex)
                Texture mainTex = renderer.material.mainTexture;
                if (mainTex == null && renderer.material.HasProperty("_MainTex"))
                {
                    mainTex = renderer.material.GetTexture("_MainTex");
                }

                if (mainTex != null && mainTex.width > 32 && mainTex.height > 32)
                {
                    SaveRuntimeTextureAsPNG(mainTex, targetTexturePath);
                    break;
                }
            }
        }

        /// <summary>
        /// Writes a runtime texture to disk as a PNG image by rendering it through a temporary RenderTexture.
        /// </summary>
        /// <param name="srcTexture">The source Unity texture to export.</param>
        /// <param name="outputPath">The full file path where the PNG should be saved.</param>
        private static void SaveRuntimeTextureAsPNG(Texture srcTexture, string outputPath)
        {
            try
            {
                int width = srcTexture.width;
                int height = srcTexture.height;

                RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                Graphics.Blit(srcTexture, rt);
                
                RenderTexture previousActive = RenderTexture.active;
                RenderTexture.active = rt;

                Texture2D unblockedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                unblockedTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                unblockedTexture.Apply();

                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(rt);

                byte[] bytes = unblockedTexture.EncodeToPNG();
                File.WriteAllBytes(outputPath, bytes);

                UnityEngine.Object.Destroy(unblockedTexture);
            }
            catch (Exception ex) { Error("Error saving creature skin texture: " + ex.Message + "\n"); }
        }

        /// <summary>
        /// Dumps a prefab's transform and component hierarchy into a JSON file.
        /// </summary>
        /// <param name="prefab">The root prefab GameObject to serialize.</param>
        /// <param name="outputPath">The full path where the JSON hierarchy file will be created.</param>
        private static void DumpPrefabHierarchyToJson(GameObject prefab, string outputPath)
        {
            try
            {
                PrefabNode rootNode = BuildPrefabNodeElement(prefab.transform);
                string jsonOutput = JsonConvert.SerializeObject(rootNode, Formatting.Indented);
                File.WriteAllText(outputPath, jsonOutput);
            }
            catch (Exception ex) { Error("Error dumping prefab hierarchy: " + ex.Message); }
        }

        /// <summary>
        /// Builds a recursive representation of a GameObject transform hierarchy for JSON serialization.
        /// </summary>
        /// <param name="current">The current transform node being converted into a <see cref="PrefabNode"/>.</param>
        /// <returns>A <see cref="PrefabNode"/> containing the current object's metadata, components, and child nodes.</returns>
        private static PrefabNode BuildPrefabNodeElement(Transform current)
        {
            PrefabNode node = new PrefabNode
            {
                object_name = current.name,
                is_active = current.gameObject.activeSelf,
                layer = LayerMask.LayerToName(current.gameObject.layer),
                tag = current.gameObject.tag
            };

            Component[] components = current.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp == null) continue;
                ComponentData compData = new ComponentData { type = comp.GetType().Name };

                if (comp is Character || comp is Humanoid || comp is MonsterAI)
                {
                    FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                    foreach (FieldInfo field in fields)
                    {
                        try
                        {
                            object val = field.GetValue(comp);
                            if (val != null && !(val is GameObject) && !(val is Component))
                            {
                                compData.fields[field.Name] = val.ToString();
                            }
                        }
                        catch (Exception ex) { Error("Error getting field value: " + ex.Message); }
                    }
                }
                node.components.Add(compData);
            }

            for (int i = 0; i < current.childCount; i++)
            {
                node.children.Add(BuildPrefabNodeElement(current.GetChild(i)));
            }

            return node;
        }

        /// <summary>
        /// Exports the prefab's mesh geometry to an OBJ file for external 3D viewing.
        /// </summary>
        /// <param name="prefab">The GameObject prefab to export.</param>
        /// <param name="outputPath">The full file path where the OBJ file will be written.</param>
        private static void ExportPrefabToObj(GameObject prefab, string outputPath)
        {
            if (File.Exists(outputPath)) return;

            try
            {
                StringBuilder objBuilder = new StringBuilder();
                objBuilder.AppendLine($"# Valheim DBDumper - 3D Mesh of {prefab.name}");

                int vertexOffset = 1;
                int normalOffset = 1;
                int uvOffset = 1;

                Matrix4x4 rootInverseMatrix = prefab.transform.worldToLocalMatrix;

                SkinnedMeshRenderer[] skinnedRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (var smr in skinnedRenderers)
                {
                    if (smr == null || smr.sharedMesh == null) continue;

                    Mesh bakedMesh = new Mesh();
                    try
                    {
                        smr.BakeMesh(bakedMesh);
                        Matrix4x4 localToRoot = rootInverseMatrix * smr.transform.localToWorldMatrix;
                        ProcessMeshAsset(bakedMesh, smr.name, objBuilder, localToRoot, ref vertexOffset, ref normalOffset, ref uvOffset);
                    }
                    catch (Exception ex)
                    {
                        Warn("Error baking mesh: " + ex.Message);
                        Matrix4x4 localToRoot = rootInverseMatrix * smr.transform.localToWorldMatrix;
                        ProcessMeshAsset(smr.sharedMesh, smr.name, objBuilder, localToRoot, ref vertexOffset, ref normalOffset, ref uvOffset);
                    }
                    finally
                    {
                        if (bakedMesh != null) UnityEngine.Object.Destroy(bakedMesh);
                    }
                }

                MeshFilter[] meshFilters = prefab.GetComponentsInChildren<MeshFilter>(true);
                foreach (var mf in meshFilters)
                {
                    if (mf == null || mf.sharedMesh == null) continue;

                    Matrix4x4 localToRoot = rootInverseMatrix * mf.transform.localToWorldMatrix;
                    ProcessMeshAsset(mf.sharedMesh, mf.name, objBuilder, localToRoot, ref vertexOffset, ref normalOffset, ref uvOffset);
                }

                if (vertexOffset > 1)
                {
                    File.WriteAllText(outputPath, objBuilder.ToString());
                }
            }
            catch (Exception ex) { Error("Error exporting prefab to OBJ: " + ex.Message); }
        }

        /// <summary>
        /// Writes mesh vertices, texture coordinates, normals, and faces into an OBJ string builder.
        /// </summary>
        /// <param name="mesh">The mesh to export.</param>
        /// <param name="name">The name used for the OBJ group.</param>
        /// <param name="sb">The StringBuilder receiving OBJ file contents.</param>
        /// <param name="localToRoot">The transformation matrix from mesh local space to root object space.</param>
        /// <param name="vOffset">Reference index offset for vertex entries.</param>
        /// <param name="nOffset">Reference index offset for normal entries.</param>
        /// <param name="uvOffset">Reference index offset for UV entries.</param>
        private static void ProcessMeshAsset(Mesh mesh, string name, StringBuilder sb, Matrix4x4 localToRoot, ref int vOffset, ref int nOffset, ref int uvOffset)
        {
            sb.AppendLine($"\ng {name}");

            Vector3[] vertices = mesh.vertices;
            foreach (Vector3 v in vertices)
            {
                Vector3 transformedVertex = localToRoot.MultiplyPoint3x4(v);
                sb.AppendLine($"v {transformedVertex.x} {transformedVertex.y} {transformedVertex.z}");
            }

            Vector2[] uvs = mesh.uv;
            foreach (Vector2 uv in uvs)
            {
                sb.AppendLine($"vt {uv.x} {uv.y}");
            }

            Vector3[] normals = mesh.normals;
            foreach (Vector3 n in normals)
            {
                Vector3 transformedNormal = localToRoot.MultiplyVector(n).normalized;
                sb.AppendLine($"vn {transformedNormal.x} {transformedNormal.y} {transformedNormal.z}");
            }

            bool hasUVs = uvs.Length > 0;
            bool hasNormals = normals.Length > 0;

            for (int materialIndex = 0; materialIndex < mesh.subMeshCount; materialIndex++)
            {
                int[] triangles = mesh.GetTriangles(materialIndex);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int v1 = triangles[i] + vOffset;
                    int v2 = triangles[i + 1] + vOffset;
                    int v3 = triangles[i + 2] + vOffset;

                    int uv1 = triangles[i] + uvOffset;
                    int uv2 = triangles[i + 1] + uvOffset;
                    int uv3 = triangles[i + 2] + uvOffset;

                    int n1 = triangles[i] + nOffset;
                    int n2 = triangles[i + 1] + nOffset;
                    int n3 = triangles[i + 2] + nOffset;

                    if (hasUVs && hasNormals)
                    {
                        sb.AppendLine($"f {v3}/{uv3}/{n3} {v2}/{uv2}/{n2} {v1}/{uv1}/{n1}");
                    }
                    else if (hasUVs)
                    {
                        sb.AppendLine($"f {v3}/{uv3} {v2}/{uv2} {v1}/{uv1}");
                    }
                    else if (hasNormals)
                    {
                        sb.AppendLine($"f {v3}//{n3} {v2}//{n2} {v1}//{n1}");
                    }
                    else
                    {
                        sb.AppendLine($"f {v3} {v2} {v1}");
                    }
                }
            }

            vOffset += vertices.Length;
            uvOffset += uvs.Length;
            nOffset += normals.Length;
        }

        /// <summary>
        /// Resolves a Valheim localization token to a display string using the game's localization system.
        /// </summary>
        /// <param name="token">The string token to translate, usually beginning with '$'.</param>
        /// <returns>
        /// The localized string when available; otherwise the original token, or an empty string if the token is null or empty.
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
            catch (Exception ex) { Error("Error translating token: " + ex.Message); return token; }
        }

        /// <summary>
        /// Crops a sprite from its source texture atlas and saves it as a PNG image file.
        /// </summary>
        /// <param name="sprite">The sprite to crop and export.</param>
        /// <param name="outputPath">The full file path where the PNG should be written.</param>
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
            catch (Exception ex) { Error("Error saving creature skin texture: " + ex.Message); }
        }
    }
}
