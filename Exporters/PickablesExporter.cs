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

    public class PickableDataDump
    {
        public string prefab_name { get; set; }
        public string pickable_name { get; set; }
        public string pickable_rawName { get; set; }
        public string drop_item_prefab { get; set; }
        public int min_amount { get; set; }
        public int max_amount { get; set; }
        public int respawn_minutes { get; set; }
    }
    
    // ╔═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗
    // ║                                                       PICKABLES EXPORTER                                                        ║
    // ╚═════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝

    public static class PickablesExporter
    {

        /// <summary>
        /// Exports pickable environment objects to JSON, icon, prefab, and 3D model artifacts.
        /// </summary>
        /// <param name="baseFolder">The base output folder for exported pickable assets.</param>
        /// <param name="args">The console event arguments used for progress logging.</param>
        /// <param name="exportJson">Whether to export pickable metadata as JSON.</param>
        /// <param name="exportIcons">Whether to export pickable icon textures as PNG files.</param>
        /// <param name="exportPrefab">Whether to export prefab component structure as JSON.</param>
        /// <param name="exportModel3d">Whether to export prefab meshes and skin textures.</param>
        /// <returns>An <see cref="System.Collections.IEnumerator"/> that yields periodically to avoid blocking game execution.</returns>
        public static IEnumerator ExportPickablesCoroutine(string baseFolder, Terminal.ConsoleEventArgs args, bool exportJson, bool exportIcons, bool exportPrefab, bool exportModel3d, Action onFinished)
        {
            string pickablesFolder = Path.Combine(baseFolder, "data", "pickables");
            string iconsFolder = Path.Combine(baseFolder, "icons");
            string prefabsFolder = Path.Combine(baseFolder, "prefabs", "pickables");
            string modelsFolder = Path.Combine(baseFolder, "models", "pickables");
            string texturesFolder = Path.Combine(baseFolder, "textures", "pickables");

            if (exportJson) Directory.CreateDirectory(pickablesFolder);
            if (exportIcons) Directory.CreateDirectory(iconsFolder);
            if (exportPrefab) Directory.CreateDirectory(prefabsFolder);
            if (exportModel3d)
            {
                Directory.CreateDirectory(modelsFolder);
                Directory.CreateDirectory(texturesFolder);
            }

            List<GameObject> allPrefabs = ZNetScene.instance.m_prefabs;
            List<PickableDataDump> exportedPickables = new List<PickableDataDump>();

            args.Context.AddString($"<color=yellow>[Valheim DBDumper] Scanning {allPrefabs.Count} game prefabs for pickables...</color>");
            Info($"Scanning {allPrefabs.Count} game prefabs for pickables...");
            yield return null;

            int operationsThisFrame = 0;
            const int MaxOperationsPerFrame = 30;

            for (int i = 0; i < allPrefabs.Count; i++)
            {
                GameObject prefab = allPrefabs[i];
                if (prefab == null) continue;

                Pickable pickable = prefab.GetComponent<Pickable>();
                if (pickable == null) continue;

                string prefabName = prefab.name;
                
                GameObject dropItemObj = pickable.m_itemPrefab;
                string dropPrefabName = dropItemObj != null ? dropItemObj.name : "None";

                // 1. Exporting icons
                if (exportIcons && dropItemObj != null)
                {
                    Directory.CreateDirectory(Path.Combine(iconsFolder, "Pickables"));
                    ItemDrop itemDrop = dropItemObj.GetComponent<ItemDrop>();
                    if (itemDrop != null && itemDrop.m_itemData?.m_shared?.m_icons?.Length > 0)
                    {
                        SaveCroppedSpriteAsPNG(itemDrop.m_itemData.m_shared.m_icons[0], Path.Combine(iconsFolder, "Pickables", $"{prefabName}.png"));
                        operationsThisFrame++;
                    }
                }

                // 2. Properties Mapping (JSON)
                if (exportJson)
                {
                    string rawName = string.IsNullOrEmpty(pickable.name) ? prefabName : pickable.name;

                    PickableDataDump data = new PickableDataDump
                    {
                        prefab_name = prefabName,
                        pickable_name = TranslateToken(rawName),
                        pickable_rawName = rawName,
                        drop_item_prefab = dropPrefabName,
                        min_amount = pickable.m_amount,
                        max_amount = pickable.m_amount, 
                        respawn_minutes = (int)pickable.m_respawnTimeMinutes
                    };

                    exportedPickables.Add(data);
                }

                // 3. Prefab Structure (JSON)
                if (exportPrefab)
                {
                    string targetFile = Path.Combine(prefabsFolder, $"{prefabName}.json");
                    DumpPrefabHierarchyToJson(prefab, targetFile);
                    operationsThisFrame++;
                }

                // 4. Model Export (.obj) + Skin Texture Export (.png)
                if (exportModel3d)
                {
                    string targetObjPath = Path.Combine(modelsFolder, $"{prefabName}.obj");
                    ExportPrefabToObj(prefab, targetObjPath);
                    
                    ExportPickableSkinTexture(prefab, texturesFolder, prefabName);
                    
                    operationsThisFrame++;
                }

                if (exportedPickables.Count % 15 == 0)
                {
                    args.Context.AddString($"<color=lightblue>[Valheim DBDumper] Processed {exportedPickables.Count} unique pickables...</color>");
                }

                if (operationsThisFrame >= MaxOperationsPerFrame)
                {
                    operationsThisFrame = 0;
                    yield return null;
                }
            }

            if (exportJson && exportedPickables.Count > 0)
            {
                string jsonOutput = JsonConvert.SerializeObject(exportedPickables, Formatting.Indented);
                File.WriteAllText(Path.Combine(pickablesFolder, "pickables.json"), jsonOutput);
            }

            args.Context.AddString($"<color=green>[Valheim DBDumper] Pickables module finished! Saved {exportedPickables.Count} environment assets and geometries.</color>");
            Success($"Pickables module finished! Saved {exportedPickables.Count} environment assets and geometries.");

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

                if (comp is Pickable)
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
        /// Exports a prefab's mesh geometry to an OBJ file for external 3D viewing.
        /// </summary>
        /// <param name="prefab">The GameObject prefab to export.</param>
        /// <param name="outputPath">The full file path where the OBJ file should be written.</param>
        private static void ExportPrefabToObj(GameObject prefab, string outputPath)
        {
            if (File.Exists(outputPath)) return;

            try
            {
                StringBuilder objBuilder = new StringBuilder();
                objBuilder.AppendLine($"# Valheim DBDumper - 3D Mesh of Pickable Resource: {prefab.name}");

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
        /// Writes mesh vertex, UV, normal, and face data into an OBJ string builder.
        /// </summary>
        /// <param name="mesh">The mesh to export.</param>
        /// <param name="name">The name used for the OBJ group.</param>
        /// <param name="sb">The StringBuilder receiving OBJ file contents.</param>
        /// <param name="localToRoot">The matrix transforming mesh local space into root object space.</param>
        /// <param name="vOffset">Reference vertex index offset for OBJ indices.</param>
        /// <param name="nOffset">Reference normal index offset for OBJ indices.</param>
        /// <param name="uvOffset">Reference UV index offset for OBJ indices.</param>
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
        /// Extracts a pickable prefab's renderer texture and saves it as a PNG.
        /// </summary>
        /// <param name="prefab">The pickable prefab whose renderer textures will be scanned.</param>
        /// <param name="texturesFolder">The folder where the output texture PNG should be saved.</param>
        /// <param name="prefabName">The name used to construct the output file name.</param>
        private static void ExportPickableSkinTexture(GameObject prefab, string texturesFolder, string prefabName)
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
        /// Reads a runtime texture into a temporary render target and writes it to a PNG file.
        /// </summary>
        /// <param name="srcTexture">The source texture to convert from GPU memory to PNG bytes.</param>
        /// <param name="outputPath">The output file path for the encoded PNG.</param>
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
            catch (Exception ex) { Error("Error saving runtime texture: " + ex.Message); }
        }

        /// <summary>
        /// Translates a Valheim localization token into a readable string when available.
        /// </summary>
        /// <param name="token">The raw localization token, such as "$name_debug".</param>
        /// <returns>The localized string if translation succeeds, otherwise the original token.</returns>
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
        /// Extracts a sprite region from its atlas texture and saves it as a PNG file.
        /// </summary>
        /// <param name="sprite">The source sprite to crop and encode.</param>
        /// <param name="outputPath">The output file path for the PNG image.</param>
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
            catch (Exception ex) { Error("Error saving cropped sprite: " + ex.Message); }
        }
    }
}
