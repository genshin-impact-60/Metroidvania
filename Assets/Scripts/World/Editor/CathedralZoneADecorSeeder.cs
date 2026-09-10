using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Places Zone A atmosphere props under Zone_Intro_A/Decor (idempotent by name).
/// </summary>
public static class CathedralZoneADecorSeeder
{
    const string DecorPrefabPath = "Assets/Prefabs/Tiles/EnvDecor.prefab";
    const string PropRoot = "Assets/Art/Environment/Props/cathedral/";

    struct PropSpec
    {
        public string Name;
        public string SpriteFile;
        public Vector3 LocalPos;
        public float Height;
        public int SortingOrder;
        public bool FlipX;
    }

    static readonly PropSpec[] Specs =
    {
        new PropSpec
        {
            Name = "prop_cathedral_fallen_cross",
            SpriteFile = "prop_cathedral_fallen_cross.png",
            LocalPos = new Vector3(-1.85f, 1.02f, 0f),
            Height = 1.35f,
            SortingOrder = 1
        },
        new PropSpec
        {
            Name = "prop_cathedral_broken_pew",
            SpriteFile = "prop_cathedral_broken_pew.png",
            LocalPos = new Vector3(1.15f, 1.02f, 0f),
            Height = 1.25f,
            SortingOrder = 1
        },
        new PropSpec
        {
            Name = "prop_cathedral_angel",
            SpriteFile = "prop_cathedral_angel.png",
            LocalPos = new Vector3(-3.95f, 1.02f, 0f),
            Height = 2.15f,
            SortingOrder = 2
        },
        new PropSpec
        {
            Name = "prop_cathedral_broken_column",
            SpriteFile = "prop_cathedral_broken_column.png",
            LocalPos = new Vector3(6.15f, 1.02f, 0f),
            Height = 2.25f,
            SortingOrder = 1
        },
        new PropSpec
        {
            Name = "prop_cathedral_banner",
            SpriteFile = "prop_cathedral_banner.png",
            LocalPos = new Vector3(8.35f, 4.15f, 0f),
            Height = 2.35f,
            SortingOrder = 2
        },
        new PropSpec
        {
            Name = "prop_cathedral_crystal",
            SpriteFile = "prop_cathedral_crystal.png",
            LocalPos = new Vector3(4.55f, 1.02f, 0f),
            Height = 1.45f,
            SortingOrder = 3
        },
    };

    [MenuItem("Tools/Cathedral/Seed Zone A Decor")]
    public static void SeedMenu()
    {
        int added = SeedInOpenScenes();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Zone A Decor",
            added > 0
                ? $"Added {added} prop(s). Save the scene (Ctrl+S)."
                : "Decor already present (or Zone_Intro_A / Decor missing).",
            "OK");
    }

    public static int SeedInOpenScenes()
    {
        int total = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded)
                continue;
            total += SeedScene(scene);
        }

        return total;
    }

    static int SeedScene(Scene scene)
    {
        var zone = FindZone(scene);
        if (zone == null)
            return 0;

        var decor = zone.transform.Find("Decor");
        if (decor == null)
        {
            var go = new GameObject("Decor");
            go.transform.SetParent(zone.transform, false);
            decor = go.transform;
            Undo.RegisterCreatedObjectUndo(go, "Create Decor");
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DecorPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Missing {DecorPrefabPath}");
            return 0;
        }

        int added = 0;
        foreach (var spec in Specs)
        {
            if (decor.Find(spec.Name) != null)
                continue;

            var sprite = LoadSprite(PropRoot + spec.SpriteFile);
            if (sprite == null)
            {
                Debug.LogWarning($"Missing sprite {spec.SpriteFile}");
                continue;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Seed Zone A Decor");
            instance.name = spec.Name;
            instance.transform.SetParent(decor, false);
            instance.transform.localPosition = spec.LocalPos;
            instance.transform.localRotation = Quaternion.identity;

            float natural = Mathf.Max(0.01f, sprite.bounds.size.y);
            float scale = spec.Height / natural;
            float sx = spec.FlipX ? -scale : scale;
            instance.transform.localScale = new Vector3(sx, scale, scale);

            var sr = instance.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Undo.RecordObject(sr, "Seed Zone A Decor Sprite");
                sr.sprite = sprite;
                sr.sortingLayerName = "Midground";
                sr.sortingOrder = spec.SortingOrder;
                sr.color = Color.white;
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            added++;
        }

        if (added > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        return added;
    }

    static Transform FindZone(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == "Zone_Intro_A")
                return root.transform;
            var nested = root.transform.Find("Zone_Intro_A");
            if (nested != null)
                return nested;
        }

        return null;
    }

    static Sprite LoadSprite(string assetPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        return null;
    }
}
