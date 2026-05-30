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

    public class ItemDataDump
    {
        public string prefab_name { get; set; }
        public string item_name { get; set; }
        public string item_rawName { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public float weight { get; set; }
        public int max_stack { get; set; }
        public float max_durability { get; set; }
        public float armor { get; set; }
        public float blunt_damage { get; set; }
        public float slash_damage { get; set; }
        public float pierce_damage { get; set; }
        public float chop_damage { get; set; }
        public float pickaxe_damage { get; set; }
        public float fire_damage { get; set; }
        public float frost_damage { get; set; }
        public float lightning_damage { get; set; }
        public float poison_damage { get; set; }
        public float spirit_damage { get; set; }
    }

    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                         ITEMS EXPORTER                                                          ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

    public static class ItemsExporter
    {
        /// <summary>
        /// Exports item prefabs to JSON, icon, prefab, and 3D model artifacts.
        /// </summary>
        /// <param name="baseFolder">The base output folder for exported item assets.</param>
        /// <param name="args">The console event arguments used for progress logging.</param>
        /// <param name="exportJson">Whether to export item metadata as JSON.</param>
        /// <param name="exportIcons">Whether to export item icon textures as PNG files.</param>
        /// <param name="exportPrefab">Whether to export prefab component structure as JSON.</param>
        /// <param name="exportModel3d">Whether to export item meshes and skin textures.</param>
        /// <returns>An <see cref="System.Collections.IEnumerator"/> that yields periodically to avoid blocking game execution.</returns>
        public static IEnumerator ExportItemsCoroutine(string baseFolder, Terminal.ConsoleEventArgs args, bool exportJson, bool exportIcons, bool exportPrefab, bool exportModel3d, Action onFinished)
        {
            string itemsFolder = Path.Combine(baseFolder, "data", "items");
            string iconsFolder = Path.Combine(baseFolder, "icons"); 
            string prefabsFolder = Path.Combine(baseFolder, "prefabs", "items");
            string modelsFolder = Path.Combine(baseFolder, "models", "items");
            string texturesFolder = Path.Combine(baseFolder, "textures", "items");

            if (exportJson) Directory.CreateDirectory(itemsFolder);
            if (exportIcons) Directory.CreateDirectory(iconsFolder);
            if (exportPrefab) Directory.CreateDirectory(prefabsFolder);
            if (exportModel3d)
            {
                Directory.CreateDirectory(modelsFolder);
                Directory.CreateDirectory(texturesFolder);
            }

            List<GameObject> itemPrefabs = ObjectDB.instance.m_items;
            List<ItemDataDump> exportedItems = new List<ItemDataDump>();

            args.Context.AddString($"<color=yellow>[Valheim DBDumper] Processing {itemPrefabs.Count} items into unified pipeline...</color>");
            Info($"Processing {itemPrefabs.Count} items into unified pipeline...");
            yield return null;

            int operationsThisFrame = 0;
            const int MaxOperationsPerFrame = 25;

            for (int i = 0; i < itemPrefabs.Count; i++)
            {
                GameObject prefab = itemPrefabs[i];
                if (prefab == null) continue;

                ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null || itemDrop.m_itemData == null) continue;

                ItemDrop.ItemData.SharedData shared = itemDrop.m_itemData.m_shared;
                string prefabName = prefab.name;

                if (exportIcons)
                {
                    Directory.CreateDirectory(Path.Combine(iconsFolder, "Items"));
                    var iconsArray = shared.m_icons;
                    if (iconsArray != null && iconsArray.Length > 0 && iconsArray[0] != null)
                    {
                        SaveCroppedSpriteAsPNG(iconsArray[0], Path.Combine(iconsFolder, "Items", $"{prefabName}.png"));
                        operationsThisFrame++;
                    }
                }

                if (exportJson)
                {
                    ItemDataDump data = new ItemDataDump
                    {
                        prefab_name = prefabName,
                        item_name = TranslateToken(shared.m_name),
                        item_rawName = shared.m_name,
                        description = TranslateToken(shared.m_description),
                        type = shared.m_itemType.ToString(),
                        weight = shared.m_weight,
                        max_stack = shared.m_maxStackSize,
                        max_durability = shared.m_maxDurability,
                        armor = shared.m_armor,
                        
                        blunt_damage = shared.m_damages.m_blunt,
                        slash_damage = shared.m_damages.m_slash,
                        pierce_damage = shared.m_damages.m_pierce,
                        chop_damage = shared.m_damages.m_chop,
                        pickaxe_damage = shared.m_damages.m_pickaxe,
                        
                        fire_damage = shared.m_damages.m_fire,
                        frost_damage = shared.m_damages.m_frost,
                        lightning_damage = shared.m_damages.m_lightning,
                        poison_damage = shared.m_damages.m_poison,
                        spirit_damage = shared.m_damages.m_spirit
                    };

                    exportedItems.Add(data);
                }

                if (exportPrefab)
                {
                    string targetFile = Path.Combine(prefabsFolder, $"{prefabName}.json");
                    DumpPrefabHierarchyToJson(prefab, targetFile);
                    operationsThisFrame++;
                }

                if (exportModel3d)
                {
                    string targetObjPath = Path.Combine(modelsFolder, $"{prefabName}.obj");
                    ExportPrefabToObj(prefab, targetObjPath);
                    ExportItemSkinTexture(prefab, texturesFolder, prefabName);
                    operationsThisFrame++;
                }

                if (i > 0 && i % 150 == 0)
                {
                    args.Context.AddString($"<color=lightblue>[Valheim DBDumper] Processed {i}/{itemPrefabs.Count} unique items...</color>");
                }

                if (operationsThisFrame >= MaxOperationsPerFrame)
                {
                    operationsThisFrame = 0;
                    yield return null;
                }
            }

            if (exportJson && exportedItems.Count > 0)
            {
                string jsonOutput = JsonConvert.SerializeObject(exportedItems, Formatting.Indented);
                File.WriteAllText(Path.Combine(itemsFolder, "items.json"), jsonOutput);
            }

            args.Context.AddString($"<color=green>[Valheim DBDumper] Item module finished processing successfully! Models and textures sync'd.</color>");
            Success($"Item module finished processing successfully! Models and textures sync'd.");

            onFinished?.Invoke();
        }

        // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
        // ║                                                         HELPER METHODS                                                          ║
        // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝
        
        /// <summary>
        /// Serializes a prefab's transform and component hierarchy into JSON and writes it to a file.
        /// </summary>
        /// <param name="prefab">The root prefab GameObject to serialize.</param>
        /// <param name="outputPath">The full path where the JSON hierarchy file should be written.</param>
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
        /// Builds a recursive <see cref="PrefabNode"/> representation of a transform hierarchy.
        /// </summary>
        /// <param name="current">The current transform node being converted into a <see cref="PrefabNode"/>.</param>
        /// <returns>A <see cref="PrefabNode"/> containing metadata, component summaries, and child nodes.</returns>
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

                if (comp is ItemDrop)
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
                        catch (Exception ex) { Error("Error dumping prefab hierarchy: " + ex.Message); }
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
        /// Exports a prefab's mesh geometry to an OBJ file for external viewing.
        /// </summary>
        /// <param name="prefab">The GameObject prefab whose mesh data will be exported.</param>
        /// <param name="outputPath">The full file path where the OBJ file should be written.</param>
        private static void ExportPrefabToObj(GameObject prefab, string outputPath)
        {
            if (File.Exists(outputPath)) return;

            try
            {
                StringBuilder objBuilder = new StringBuilder();
                objBuilder.AppendLine($"# Valheim DBDumper - 3D Mesh of Item: {prefab.name}");

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
                        Warn($"Error processing mesh {smr.name}: {ex.Message}");
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
        /// Writes mesh vertex, UV, normal, and face data into an OBJ builder string.
        /// </summary>
        /// <param name="mesh">The mesh asset to convert into OBJ geometry.</param>
        /// <param name="name">The mesh name used to group faces in the OBJ file.</param>
        /// <param name="sb">The StringBuilder collecting the OBJ file contents.</param>
        /// <param name="localToRoot">The transform matrix from mesh local space into the prefab root space.</param>
        /// <param name="vOffset">A running vertex index offset for mesh face indexing.</param>
        /// <param name="nOffset">A running normal index offset for mesh face indexing.</param>
        /// <param name="uvOffset">A running UV index offset for mesh face indexing.</param>
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
        /// Finds an item's primary texture from renderers and saves it as a PNG file.
        /// </summary>
        /// <param name="prefab">The item prefab to search for a skin or main texture.</param>
        /// <param name="texturesFolder">The directory where the exported texture PNG should be stored.</param>
        /// <param name="prefabName">The prefab name used to name the exported texture file.</param>
        private static void ExportItemSkinTexture(GameObject prefab, string texturesFolder, string prefabName)
        {
            string targetTexturePath = Path.Combine(texturesFolder, $"{prefabName}.png");
            if (File.Exists(targetTexturePath)) return;

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.material == null) continue;

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
        /// Renders a runtime Unity texture to a PNG file by copying it through a temporary RenderTexture.
        /// </summary>
        /// <param name="srcTexture">The source runtime texture to capture and encode.</param>
        /// <param name="outputPath">The destination file path for the encoded PNG image.</param>
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
            catch (Exception ex) { Error($"Failed to export {outputPath}: {ex.Message}"); }
        }

        /// <summary>
        /// Resolves a localization token to its translated text if the token begins with '$'.
        /// </summary>
        /// <param name="token">The raw localization token or plain text value.</param>
        /// <returns>The translated string when available, otherwise the original token.</returns>
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
        /// Extracts a sprite's sub-region from its atlas texture and saves it as a PNG file.
        /// </summary>
        /// <param name="sprite">The sprite whose texture region will be captured.</param>
        /// <param name="outputPath">The destination file path for the cropped PNG.</param>
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

                RenderTexture rt = RenderTexture.GetTemporary(atlasTexture.width, atlasTexture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
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
            catch (Exception ex) { Error($"Failed to export {outputPath}: {ex.Message}"); }
        }
    }
}
