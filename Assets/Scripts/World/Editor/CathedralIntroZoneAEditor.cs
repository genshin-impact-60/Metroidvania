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

        if (GUILayout.Button("Bake Hierarchy → Layout", GUILayout.Height(32)))
        {
            if (BakeLayout(zone, out var message))
                EditorUtility.DisplayDialog("Zone A Layout Baked", message, "OK");
            else
                EditorUtility.DisplayDialog("Bake Failed", message, "OK");
        }

        if (GUILayout.Button("Bake Colliders → Layout", GUILayout.Height(32)))
        {
            if (BakeLayout(zone, out var message, collidersOnlyHint: true))
                EditorUtility.DisplayDialog("Zone A Colliders Baked", message, "OK");
            else
                EditorUtility.DisplayDialog("Bake Failed", message, "OK");
        }

        if (GUILayout.Button("Rebuild Zone A", GUILayout.Height(32)))
        {
            Undo.RegisterFullObjectHierarchyUndo(zone.gameObject, "Rebuild Zone A");
            zone.Rebuild();
            EditorUtility.SetDirty(zone);
        }

        EditorGUILayout.HelpBox(
            "Cathedral Intro · Zone A（裂隙坑）\n" +
            "手调 Hierarchy / 碰撞后点 Bake 写入 ZoneA_Layout.json。\n" +
            "站立胶囊：Player CapsuleCollider2D；getup：PlayerController Getup Collider。\n" +
            "Rebuild Zone A 按烘焙布局+碰撞重建。",
            MessageType.Info);
    }

    [MenuItem("Tools/Cathedral/Bake Intro Zone A Layout")]
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
            ? $"Baked layout for {count} CathedralIntroZoneA instance(s)."
            : "No CathedralIntroZoneA found in open scenes.");
    }

    [MenuItem("Tools/Cathedral/Bake Intro Zone A Colliders")]
    public static void BakeCollidersAllInOpenScenes()
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
                    if (BakeLayout(zone, out _, collidersOnlyHint: true))
                        count++;
                }
            }
        }

        Debug.Log(count > 0
            ? $"Baked colliders for {count} CathedralIntroZoneA instance(s)."
            : "No CathedralIntroZoneA found in open scenes.");
    }

    [MenuItem("Tools/Cathedral/Rebuild Intro Zone A")]
    public static void RebuildAllInOpenScenes()
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
                    Undo.RegisterFullObjectHierarchyUndo(zone.gameObject, "Rebuild Zone A");
                    zone.Rebuild();
                    EditorUtility.SetDirty(zone);
                    count++;
                }
            }

            if (count > 0)
                EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log(count > 0
            ? $"Rebuilt {count} CathedralIntroZoneA instance(s)."
            : "No CathedralIntroZoneA found in open scenes.");
    }

    public static bool BakeLayout(CathedralIntroZoneA zone, out string message, bool collidersOnlyHint = false)
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
            message = "No Platforms child — nothing to bake. Rebuild once first, then hand-tune.";
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
        else
        {
            var holy = zone.GetComponentInChildren<HolyWaterDripFx>(true);
            if (holy != null)
            {
                snap.hasHolyWater = true;
                var root = holy.transform.parent != null ? holy.transform.parent : holy.transform;
                snap.holyWaterX = root.position.x;
                CaptureHolyWaterHitbox(root, snap);
            }
        }

        var player = zone.GetComponentInChildren<PlayerController>(true);
        if (player != null)
        {
            snap.playerX = player.transform.position.x;
            snap.playerY = player.transform.position.y;
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
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/Scripts/World");
        File.WriteAllText(path, json, Encoding.UTF8);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

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

        string focus = collidersOnlyHint ? "colliders + layout" : "layout + colliders";
        message =
            $"Saved {entries.Count} sprites ({customCols} custom colliders) + {blockers.Count} blocker(s) →\n{path}\n" +
            $"({focus}) HolyWater={(snap.hasHolyWater ? "yes" : "no")}  " +
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
        if (goName.StartsWith("Ceiling_") || goName.StartsWith("Ceiling"))
            return "ceiling";
        if (goName.StartsWith("BG_") || spriteName.Contains("map_bg_"))
            return "bg";
        if (bucket == "Decor" || spriteName.StartsWith("map_props_"))
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
