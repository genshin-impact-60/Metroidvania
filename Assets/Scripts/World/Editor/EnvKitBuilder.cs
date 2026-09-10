using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Kit prefabs for cathedral environment pieces, plus a one-shot conversion
/// of loose scene objects into prefab instances (hand-tuned transforms stay).
/// </summary>
[InitializeOnLoad]
public static class EnvKitBuilder
{
    public const string SolidPath = "Assets/Prefabs/Tiles/EnvSolid.prefab";
    public const string DecorPath = "Assets/Prefabs/Tiles/EnvDecor.prefab";
    public const string BackgroundPath = "Assets/Prefabs/Tiles/EnvBackground.prefab";
    public const string BlockerPath = "Assets/Prefabs/Tiles/EnvBlocker.prefab";
    public const string HolyWaterPath = "Assets/Prefabs/Hazards/HolyWater.prefab";
    public const string SaveShrinePath = "Assets/Prefabs/Interact/SaveShrine.prefab";

    const string UnlitMatPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
    const string FloorSpritePath = "Assets/Art/Environment/Tilesets/cathedral/map_tileset_cathedral_1.png";
    const string WallSpritePath = "Assets/Art/Environment/Tilesets/cathedral/map_tileset_cathedral_4.png";
    const string BgSpritePath = "Assets/Art/Environment/Backgrounds/map_bg_cathedral.png";
    const string BasinSpritePath = "Assets/Art/Environment/Hazards/holywater_basin_pool.png";
    const string SaveOffSpritePath = "Assets/Art/Environment/Interact/save_cathedral_off.png";
    const string SaveOnSpritePath = "Assets/Art/Environment/Interact/save_cathedral_on.png";

    static EnvKitBuilder()
    {
        EditorApplication.delayCall += EnsureKitsAndConvert;
    }

    [MenuItem("Tools/Cathedral/Create Environment Kit Prefabs")]
    public static void CreateOrUpdateMenu()
    {
        EnsureKitsAndConvert();
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Environment Kit",
            "Created/updated kit prefabs and converted loose environment pieces.\nSave the scene (Ctrl+S).",
            "OK");
    }

    public static void EnsureKitsAndConvert()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += EnsureKitsAndConvert;
            return;
        }

        var solid = LoadOrCreate(SolidPath, CreateSolid);
        var decor = LoadOrCreate(DecorPath, CreateDecor);
        var background = LoadOrCreate(BackgroundPath, CreateBackground);
        var blocker = LoadOrCreate(BlockerPath, CreateBlocker);
        var holy = LoadOrCreate(HolyWaterPath, CreateHolyWater);
        LoadOrCreate(SaveShrinePath, CreateSaveShrine);
        WireSaveShrineSprites();

        AssignToOpenContent(solid);
        ConvertLoosePieces(solid, decor, background, blocker, holy);
    }

    public static GameObject LoadSolid() => AssetDatabase.LoadAssetAtPath<GameObject>(SolidPath);

    static GameObject LoadOrCreate(string path, System.Func<GameObject> factory)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
        var temp = factory();
        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        if (prefab == null)
            Debug.LogError($"Failed to save {path}");
        else
            Debug.Log($"Saved kit prefab → {path}");
        return prefab;
    }

    static void EnsureFolder(string path)
    {
        if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
            return;
        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var name = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static Material Unlit()
    {
        return AssetDatabase.LoadAssetAtPath<Material>(UnlitMatPath);
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

    static void ApplySprite(SpriteRenderer sr, Sprite sprite, string sortingLayer, int order)
    {
        sr.sprite = sprite;
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = order;
        sr.color = Color.white;
        var mat = Unlit();
        if (mat != null)
            sr.sharedMaterial = mat;
    }

    static GameObject CreateSolid()
    {
        var go = new GameObject("EnvSolid");
        int ground = GameLayers.GroundLayer;
        if (ground >= 0)
            go.layer = ground;

        var sr = go.AddComponent<SpriteRenderer>();
        var sprite = LoadSprite(FloorSpritePath);
        ApplySprite(sr, sprite, "Platforms", 0);

        var col = go.AddComponent<BoxCollider2D>();
        if (sprite != null)
        {
            float w = sprite.bounds.size.x;
            float h = sprite.bounds.size.y;
            float thickness = Mathf.Clamp(h * 0.2f, 0.16f, 0.32f);
            col.size = new Vector2(w * 1.04f, thickness);
            col.offset = new Vector2(0f, h - thickness * 0.5f);
        }

        return go;
    }

    static GameObject CreateDecor()
    {
        var go = new GameObject("EnvDecor");
        var sr = go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, LoadSprite(WallSpritePath), "Platforms", 4);
        return go;
    }

    static GameObject CreateBackground()
    {
        var go = new GameObject("EnvBackground");
        var sr = go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, LoadSprite(BgSpritePath), "Background", -10);
        sr.color = new Color(0.85f, 0.86f, 0.9f, 1f);
        return go;
    }

    static GameObject CreateBlocker()
    {
        var go = new GameObject("EnvBlocker");
        int ground = GameLayers.GroundLayer;
        if (ground >= 0)
            go.layer = ground;
        go.AddComponent<BoxCollider2D>().size = Vector2.one;
        return go;
    }

    static GameObject CreateHolyWater()
    {
        var root = new GameObject("HolyWater");
        var basin = LoadSprite(BasinSpritePath);

        var basinGo = new GameObject("BasinPool");
        basinGo.transform.SetParent(root.transform, false);
        var basinSr = basinGo.AddComponent<SpriteRenderer>();
        ApplySprite(basinSr, basin, "Hazards", 1);
        if (basin != null)
        {
            float scale = 1.35f / Mathf.Max(0.01f, basin.bounds.size.x);
            basinGo.transform.localScale = Vector3.one * scale;
            basinSr.color = new Color(0.92f, 0.98f, 0.94f, 0.92f);
        }

        float visualW = basin != null
            ? basin.bounds.size.x * basinGo.transform.localScale.x
            : 1.2f;
        float hitW = Mathf.Clamp(visualW * 0.55f, 0.7f, 1.15f);

        var hit = new GameObject("BurnHitbox");
        int hazard = GameLayers.HazardLayer;
        if (hazard >= 0)
            hit.layer = hazard;
        hit.tag = "Hazard";
        hit.transform.SetParent(root.transform, false);
        hit.transform.localPosition = new Vector3(0f, 0.18f, 0f);

        var col = hit.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(hitW, 0.32f);

        var zone = hit.AddComponent<DamageZone>();
        zone.Configure(1, 5f, 4f, "圣水灼肤——这里不欢迎他/她。");
        return root;
    }

    static GameObject CreateSaveShrine()
    {
        var go = new GameObject("SaveShrine");
        int interact = GameLayers.InteractLayer;
        if (interact >= 0)
            go.layer = interact;
        go.tag = "Interact";

        var off = LoadSprite(SaveOffSpritePath);
        var on = LoadSprite(SaveOnSpritePath);
        var sr = go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, off, "Entities", 5);

        if (off != null)
        {
            float scale = 2.2f / Mathf.Max(0.01f, off.bounds.size.y);
            go.transform.localScale = Vector3.one * scale;
        }

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        if (off != null)
        {
            col.size = off.bounds.size * 0.55f;
            col.offset = new Vector2(0f, off.bounds.size.y * 0.35f);
        }
        else
        {
            col.size = new Vector2(1.2f, 2f);
            col.offset = new Vector2(0f, 1f);
        }

        var shrine = go.AddComponent<SaveShrine>();
        shrine.Bind(sr, "save_cathedral_sv1", off, on);
        return go;
    }

    static void WireSaveShrineSprites()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SaveShrinePath);
        if (prefab == null)
            return;

        var shrine = prefab.GetComponent<SaveShrine>();
        var sr = prefab.GetComponent<SpriteRenderer>();
        if (shrine == null || sr == null)
            return;

        var off = LoadSprite(SaveOffSpritePath);
        var on = LoadSprite(SaveOnSpritePath);
        if (off == null && on == null)
            return;

        var so = new SerializedObject(shrine);
        so.FindProperty("visual").objectReferenceValue = sr;
        so.FindProperty("inactiveSprite").objectReferenceValue = off;
        so.FindProperty("activeSprite").objectReferenceValue = on;
        so.FindProperty("inactiveTint").colorValue = Color.white;
        so.FindProperty("activeTint").colorValue = Color.white;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (off != null)
        {
            sr.sprite = off;
            sr.color = Color.white;
            EditorUtility.SetDirty(sr);
        }

        EditorUtility.SetDirty(shrine);
        EditorUtility.SetDirty(prefab);
    }

    static void AssignToOpenContent(GameObject solid)
    {
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded)
                continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var playground in root.GetComponentsInChildren<StepAPlayground>(true))
                    AssignPlayground(playground, solid, scene);
            }
        }
    }

    static void AssignPlayground(StepAPlayground playground, GameObject solid, Scene scene)
    {
        var so = new SerializedObject(playground);
        SetRef(so, "envSolidPrefab", solid);
        if (so.ApplyModifiedPropertiesWithoutUndo())
        {
            EditorUtility.SetDirty(playground);
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }
    }

    static void SetRef(SerializedObject so, string field, Object value)
    {
        var prop = so.FindProperty(field);
        if (prop != null && prop.objectReferenceValue != value)
            prop.objectReferenceValue = value;
    }

    static void ConvertLoosePieces(
        GameObject solid,
        GameObject decor,
        GameObject background,
        GameObject blocker,
        GameObject holy)
    {
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded)
                continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var zone in root.GetComponentsInChildren<CathedralIntroZoneA>(true))
                    ConvertFolderChildren(zone.transform, solid, decor, background, blocker, holy, scene);
                foreach (var playground in root.GetComponentsInChildren<StepAPlayground>(true))
                    ConvertFolderChildren(playground.transform, solid, decor, background, blocker, holy, scene);
            }
        }
    }

    static void ConvertFolderChildren(
        Transform owner,
        GameObject solid,
        GameObject decor,
        GameObject background,
        GameObject blocker,
        GameObject holy,
        Scene scene)
    {
        string[] folders = { "Platforms", "Decor", "Atmosphere" };
        bool dirty = false;
        foreach (var folderName in folders)
        {
            var folder = owner.Find(folderName);
            if (folder == null)
                continue;

            var children = new List<Transform>();
            for (int i = 0; i < folder.childCount; i++)
                children.Add(folder.GetChild(i));

            foreach (var child in children)
            {
                if (child == null)
                    continue;
                if (ReplaceChild(child.gameObject, solid, decor, background, blocker, holy))
                    dirty = true;
            }
        }

        if (dirty && scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Converted loose environment pieces under '{owner.name}'. Save the scene (Ctrl+S).", owner);
        }
    }

    static bool ReplaceChild(
        GameObject old,
        GameObject solid,
        GameObject decor,
        GameObject background,
        GameObject blocker,
        GameObject holy)
    {
        if (old == null || EditorUtility.IsPersistent(old))
            return false;
        if (old.GetComponent<PlayerController>() != null)
            return false;

        if (old.name == "HolyWater")
            return ReplaceWithPrefab(old, holy, CopyHolyWater);

        if (old.name == "WallBlocker" || (old.GetComponent<SpriteRenderer>() == null && old.GetComponent<BoxCollider2D>() != null))
            return ReplaceWithPrefab(old, blocker, CopyBlocker);

        if (old.name.StartsWith("BG_") || SortingIsBackground(old))
            return ReplaceWithPrefab(old, background, CopySpritePiece);

        if (old.GetComponent<BoxCollider2D>() != null && old.GetComponent<SpriteRenderer>() != null)
            return ReplaceWithPrefab(old, solid, CopySpritePiece);

        if (old.GetComponent<SpriteRenderer>() != null)
            return ReplaceWithPrefab(old, decor, CopySpritePiece);

        return false;
    }

    static bool SortingIsBackground(GameObject go)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        return sr != null && sr.sortingLayerName == "Background";
    }

    static bool ReplaceWithPrefab(GameObject old, GameObject prefab, System.Action<GameObject, GameObject> copy)
    {
        if (prefab == null)
            return false;

        string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(old);
        if (assetPath == AssetDatabase.GetAssetPath(prefab))
            return false;

        var parent = old.transform.parent;
        int sibling = old.transform.GetSiblingIndex();
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = old.name;
        instance.transform.SetSiblingIndex(sibling);
        instance.transform.position = old.transform.position;
        instance.transform.rotation = old.transform.rotation;
        instance.transform.localScale = old.transform.localScale;
        instance.layer = old.layer;
        if (!string.IsNullOrEmpty(old.tag) && old.tag != "Untagged")
            instance.tag = old.tag;

        copy?.Invoke(old, instance);
        Object.DestroyImmediate(old);
        return true;
    }

    static void CopySpritePiece(GameObject old, GameObject instance)
    {
        var oldSr = old.GetComponent<SpriteRenderer>();
        var newSr = instance.GetComponent<SpriteRenderer>();
        if (oldSr != null && newSr != null)
        {
            newSr.sprite = oldSr.sprite;
            newSr.sortingLayerID = oldSr.sortingLayerID;
            newSr.sortingOrder = oldSr.sortingOrder;
            newSr.color = oldSr.color;
            newSr.flipX = oldSr.flipX;
            newSr.flipY = oldSr.flipY;
            var mat = Unlit();
            if (mat != null)
                newSr.sharedMaterial = mat;
        }

        var oldCol = old.GetComponent<BoxCollider2D>();
        var newCol = instance.GetComponent<BoxCollider2D>();
        if (oldCol != null && newCol != null)
        {
            newCol.size = oldCol.size;
            newCol.offset = oldCol.offset;
            newCol.isTrigger = oldCol.isTrigger;
        }
    }

    static void CopyBlocker(GameObject old, GameObject instance)
    {
        var oldCol = old.GetComponent<BoxCollider2D>();
        var newCol = instance.GetComponent<BoxCollider2D>();
        if (oldCol != null && newCol != null)
        {
            newCol.size = oldCol.size;
            newCol.offset = oldCol.offset;
            newCol.isTrigger = oldCol.isTrigger;
        }
    }

    static void CopyHolyWater(GameObject old, GameObject instance)
    {
        CopyChildSprite(old.transform.Find("BasinPool"), instance.transform.Find("BasinPool"));
        var oldHit = old.transform.Find("BurnHitbox");
        var newHit = instance.transform.Find("BurnHitbox");
        if (oldHit == null || newHit == null)
            return;

        newHit.localPosition = oldHit.localPosition;
        newHit.gameObject.layer = oldHit.gameObject.layer;
        newHit.gameObject.tag = oldHit.gameObject.tag;

        var oldCol = oldHit.GetComponent<BoxCollider2D>();
        var newCol = newHit.GetComponent<BoxCollider2D>();
        if (oldCol != null && newCol != null)
        {
            newCol.size = oldCol.size;
            newCol.offset = oldCol.offset;
            newCol.isTrigger = oldCol.isTrigger;
        }

        var oldZone = oldHit.GetComponent<DamageZone>();
        var newZone = newHit.GetComponent<DamageZone>();
        if (oldZone != null && newZone != null)
        {
            var soOld = new SerializedObject(oldZone);
            var soNew = new SerializedObject(newZone);
            soNew.FindProperty("damage").intValue = soOld.FindProperty("damage").intValue;
            soNew.FindProperty("knockbackX").floatValue = soOld.FindProperty("knockbackX").floatValue;
            soNew.FindProperty("knockbackY").floatValue = soOld.FindProperty("knockbackY").floatValue;
            soNew.FindProperty("ignoreWhileControlLocked").boolValue =
                soOld.FindProperty("ignoreWhileControlLocked").boolValue;
            soNew.FindProperty("firstHitMessage").stringValue = soOld.FindProperty("firstHitMessage").stringValue;
            soNew.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void CopyChildSprite(Transform oldChild, Transform newChild)
    {
        if (oldChild == null || newChild == null)
            return;
        newChild.localPosition = oldChild.localPosition;
        newChild.localScale = oldChild.localScale;
        var oldSr = oldChild.GetComponent<SpriteRenderer>();
        var newSr = newChild.GetComponent<SpriteRenderer>();
        if (oldSr == null || newSr == null)
            return;
        newSr.sprite = oldSr.sprite;
        newSr.sortingLayerID = oldSr.sortingLayerID;
        newSr.sortingOrder = oldSr.sortingOrder;
        newSr.color = oldSr.color;
        var mat = Unlit();
        if (mat != null)
            newSr.sharedMaterial = mat;
    }
}
