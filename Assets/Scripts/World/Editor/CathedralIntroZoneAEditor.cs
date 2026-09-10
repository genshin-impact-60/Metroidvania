using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(CathedralIntroZoneA))]
public class CathedralIntroZoneAEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        var zone = (CathedralIntroZoneA)target;

        EditorGUILayout.HelpBox(
            "场景即关卡（与常见 Unity 流程一致）\n"
            + "1. 角色：Prefabs/Player/Player.prefab\n"
            + "2. 环境：Prefabs/Tiles（地板/顶棚/装饰）与 Prefabs/Hazards/HolyWater\n"
            + "3. 在 Hierarchy / Scene 里拖实例，Ctrl+S 保存\n"
            + "4. Play 只播场景。生成 / 重置只在编辑器里跑，不会进包",
            MessageType.Info);

        if (GUILayout.Button("创建 / 刷新 Player Prefab", GUILayout.Height(24)))
            PlayerPrefabBuilder.CreateOrUpdateMenu();
        if (GUILayout.Button("创建环境套件 Prefab 并转换场景物件", GUILayout.Height(24)))
            EnvKitBuilder.CreateOrUpdateMenu();

        bool hasContent = zone != null && zone.HasSeededContent;

        using (new EditorGUI.DisabledScope(hasContent))
        {
            if (GUILayout.Button("首次生成 Zone A（空场景）", GUILayout.Height(32)))
            {
                PlayerPrefabBuilder.EnsurePrefabAndWireScenes();
                EnvKitBuilder.EnsureKitsAndConvert();
                Undo.RegisterFullObjectHierarchyUndo(zone.gameObject, "Generate Zone A");
                ZoneASeeder.SeedIfEmpty(zone);
                EditorUtility.SetDirty(zone);
                if (zone.gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);
            }
        }

        if (GUILayout.Button("重置生成 Zone A…（会清空手改）", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog(
                    "重置 Zone A",
                    "将删除当前 Zone A 下所有子物体，并按备份 JSON / 代码默认重新生成。\n\n"
                    + "场景里的手调会丢失。确定？",
                    "重置生成",
                    "取消"))
            {
                PlayerPrefabBuilder.EnsurePrefabAndWireScenes();
                EnvKitBuilder.EnsureKitsAndConvert();
                Undo.RegisterFullObjectHierarchyUndo(zone.gameObject, "Regenerate Zone A");
                ZoneASeeder.Rebuild(zone);
                EditorUtility.SetDirty(zone);
                if (zone.gameObject.scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("可选备份", EditorStyles.boldLabel);
        if (GUILayout.Button("导出备份 → ZoneA_Layout.json", GUILayout.Height(24)))
        {
            if (BakeLayout(zone, out var message))
                EditorUtility.DisplayDialog("备份已导出", message, "OK");
            else
                EditorUtility.DisplayDialog("导出失败", message, "OK");
        }

        EditorGUILayout.HelpBox(
            "导出备份仅作保险 / 重置种子，日常改关只存 Scene。生成代码在 Editor 程序集，不会打进 Player 包。",
            MessageType.None);
    }

    [MenuItem("Tools/Cathedral/Export Intro Zone A Backup")]
    public static void BakeAllInOpenScenes()
    {
        int count = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var zone in root.GetComponentsInChildren<CathedralIntroZoneA>(true))
                {
                    if (BakeLayout(zone, out _))
                        count++;
                }
            }
        }

        Debug.Log(count > 0
            ? $"Exported backup for {count} CathedralIntroZoneA instance(s)."
            : "No CathedralIntroZoneA found in open scenes.");
    }

    [MenuItem("Tools/Cathedral/Regenerate Intro Zone A (WIPES)")]
    public static void RebuildAllInOpenScenes()
    {
        if (!EditorUtility.DisplayDialog(
                "重置所有 Zone A",
                "将重置当前已打开场景中的全部 CathedralIntroZoneA（清空手改）。确定？",
                "重置",
                "取消"))
            return;

        int count = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var zone in root.GetComponentsInChildren<CathedralIntroZoneA>(true))
                {
                    Undo.RegisterFullObjectHierarchyUndo(zone.gameObject, "Regenerate Zone A");
                    ZoneASeeder.Rebuild(zone);
                    EditorUtility.SetDirty(zone);
                    count++;
                }
            }

            if (count > 0)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log(count > 0
            ? $"Regenerated {count} CathedralIntroZoneA instance(s)."
            : "No CathedralIntroZoneA found in open scenes.");
    }

    public static bool BakeLayout(CathedralIntroZoneA zone, out string message)
    {
        if (zone == null)
        {
            message = "Zone is null.";
            return false;
        }

        var platforms = zone.transform.Find("Platforms");
        var decor = zone.transform.Find("Decor");
        var atmosphere = zone.transform.Find("Atmosphere");
        if (platforms == null)
        {
            message = "No Platforms child — generate the zone once first, then export.";
            return false;
        }

        var entries = new List<ZoneASpriteEntry>();
        var blockers = new List<ZoneABlockerEntry>();

        CaptureFolder(platforms, "Platforms", entries, blockers);
        if (decor != null)
            CaptureFolder(decor, "Decor", entries, blockers);
        if (atmosphere != null)
            CaptureFolder(atmosphere, "Atmosphere", entries, blockers);

        var snap = new ZoneALayoutSnapshot
        {
            entries = entries.ToArray(),
            blockers = blockers.ToArray(),
        };

        var holyRoot = FindHolyWaterRoot(zone.transform);
        if (holyRoot != null)
        {
            snap.hasHolyWater = true;
            snap.holyWaterX = holyRoot.position.x;
            CaptureHolyWaterHitbox(holyRoot, snap);
        }

        var player = zone.GetComponentInChildren<PlayerController>(true);
        if (player != null)
        {
            CapturePlayerPose(player, snap);
            CapturePlayerColliders(player, snap);
        }

        var cam = Camera.main;
        if (cam != null)
        {
            snap.camX = cam.transform.position.x;
            snap.camY = cam.transform.position.y;
            snap.camZ = cam.transform.position.z;
        }

        string json = JsonUtility.ToJson(snap, prettyPrint: true);
        string path = CathedralIntroZoneA.LayoutAssetPath;
        string absolute = CathedralIntroZoneA.LayoutAbsolutePath;
        Directory.CreateDirectory(Path.GetDirectoryName(absolute) ?? Path.Combine(Application.dataPath, "Scripts/World"));
        File.WriteAllText(absolute, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

        var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        var so = new SerializedObject(zone);
        so.FindProperty("bakedLayout").objectReferenceValue = textAsset;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(zone);
        EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);

        int customCols = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].hasCustomCollider)
                customCols++;
        }

        message =
            $"Backup {entries.Count} sprites ({customCols} custom colliders) + {blockers.Count} blocker(s) →\n{path}\n" +
            $"HolyWater={(snap.hasHolyWater ? "yes" : "no")}  " +
            $"PlayerCol={(snap.hasPlayerCollider ? "yes" : "no")}  " +
            $"Player=({snap.playerX:0.##}, {snap.playerY:0.##})";
        Debug.Log($"[Zone A] {message}");
        return true;
    }

    static Transform FindHolyWaterRoot(Transform zoneRoot)
    {
        var atmosphere = zoneRoot.Find("Atmosphere");
        if (atmosphere != null)
        {
            var hw = atmosphere.Find("HolyWater");
            if (hw != null)
                return hw;
        }

        foreach (var t in zoneRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "HolyWater")
                return t;
        }

        return null;
    }

    static void CaptureHolyWaterHitbox(Transform holyRoot, ZoneALayoutSnapshot snap)
    {
        var hit = holyRoot.Find("BurnHitbox");
        if (hit == null)
            return;

        var col = hit.GetComponent<BoxCollider2D>();
        if (col == null)
            return;

        var lp = hit.localPosition;
        snap.hasHolyWaterHitbox = true;
        snap.holyHitLx = lp.x;
        snap.holyHitLy = lp.y;
        snap.holyHitW = col.size.x;
        snap.holyHitH = col.size.y;
        snap.holyHitOx = col.offset.x;
        snap.holyHitOy = col.offset.y;
    }

    static readonly Vector3 PlayerVisualDefaultLocal = new Vector3(0.07f, -0.2f, 0f);

    static void CapturePlayerPose(PlayerController player, ZoneALayoutSnapshot snap)
    {
        var root = player.transform;
        Vector3 bakedRoot = root.position;
        var visual = root.Find("Visual");
        if (visual != null)
        {
            Vector3 visualWorld = visual.position;
            bakedRoot = visualWorld - PlayerVisualDefaultLocal;

            bool visualDragged = (visual.localPosition - PlayerVisualDefaultLocal).sqrMagnitude > 0.0001f;
            if (visualDragged)
            {
                Undo.RecordObject(root, "Bake Player Pose From Visual");
                Undo.RecordObject(visual, "Bake Player Pose From Visual");
                root.position = bakedRoot;
                visual.localPosition = PlayerVisualDefaultLocal;
                Debug.LogWarning(
                    $"[Zone A] Export: Visual was moved — corrected Player root → ({bakedRoot.x:0.##}, {bakedRoot.y:0.##})");
            }
        }

        snap.playerX = bakedRoot.x;
        snap.playerY = bakedRoot.y;
    }

    static void CapturePlayerColliders(PlayerController player, ZoneALayoutSnapshot snap)
    {
        player.GetColliderTune(out var standSize, out var standOff, out var getupSize, out var getupOff);
        snap.hasPlayerCollider = true;
        snap.standColW = standSize.x;
        snap.standColH = standSize.y;
        snap.standColOx = standOff.x;
        snap.standColOy = standOff.y;
        snap.getupColW = getupSize.x;
        snap.getupColH = getupSize.y;
        snap.getupColOx = getupOff.x;
        snap.getupColOy = getupOff.y;
    }

    static void CaptureFolder(
        Transform folder,
        string bucket,
        List<ZoneASpriteEntry> entries,
        List<ZoneABlockerEntry> blockers)
    {
        for (int i = 0; i < folder.childCount; i++)
        {
            var child = folder.GetChild(i);
            if (child.name == "WallBlocker")
            {
                var col = child.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    var ls = child.localScale;
                    blockers.Add(new ZoneABlockerEntry
                    {
                        x = child.position.x,
                        y = child.position.y,
                        w = Mathf.Abs(ls.x) * col.size.x,
                        h = Mathf.Abs(ls.y) * col.size.y,
                        ox = col.offset.x,
                        oy = col.offset.y,
                    });
                }

                continue;
            }

            // HolyWater root has no sprite; hitbox/X captured separately.
            if (child.name == "HolyWater")
                continue;

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null)
                continue;

            string role = InferRole(bucket, child.name, sr.sprite.name);
            var box = child.GetComponent<BoxCollider2D>();
            bool withCollider = child.GetComponent<Collider2D>() != null;
            var c = sr.color;
            var p = child.position;
            var s = child.localScale;

            var entry = new ZoneASpriteEntry
            {
                bucket = bucket,
                spriteName = sr.sprite.name,
                role = role,
                x = p.x,
                y = p.y,
                z = p.z,
                sx = s.x,
                sy = s.y,
                sz = s.z,
                sortingLayer = sr.sortingLayerName,
                sortingOrder = sr.sortingOrder,
                cr = c.r,
                cg = c.g,
                cb = c.b,
                ca = c.a,
                withCollider = withCollider,
            };

            if (box != null)
            {
                entry.hasCustomCollider = true;
                entry.colW = box.size.x;
                entry.colH = box.size.y;
                entry.colOx = box.offset.x;
                entry.colOy = box.offset.y;
            }

            entries.Add(entry);
        }
    }

    static string InferRole(string bucket, string goName, string spriteName)
    {
        if (goName.StartsWith("Ceiling_") || goName.StartsWith("Ceiling") ||
            spriteName.Contains("cathedral_15") || spriteName.Contains("holywater_ceiling"))
            return "ceiling";
        if (goName.StartsWith("BG_") || spriteName.Contains("map_bg_"))
            return "bg";
        if (bucket == "Decor" || spriteName.StartsWith("map_props_") || spriteName.StartsWith("prop_"))
            return "decor";
        if (spriteName.Contains("cathedral_4") || spriteName.Contains("cathedral_5"))
            return "wall";
        if (spriteName.Contains("cathedral_0"))
            return "fill";
        if (spriteName.Contains("cathedral_1"))
            return "floor";
        if (spriteName.Contains("cathedral_2") || spriteName.Contains("cathedral_9"))
            return "fissure";
        return "other";
    }
}
