using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor-only Zone A seeding. Instantiates kit prefabs; never runs in a Player build.
/// </summary>
public sealed class ZoneASeeder
{
    const string TileDir = "Assets/Art/Environment/Tilesets/cathedral/";
    const string FloorTilePath = TileDir + "map_tileset_cathedral_1.png";
    const string FillTilePath = TileDir + "map_tileset_cathedral_0.png";
    const string CrackedTilePath = TileDir + "map_tileset_cathedral_2.png";
    const string WallEndTilePath = TileDir + "map_tileset_cathedral_4.png";
    const string ColumnTilePath = TileDir + "map_tileset_cathedral_5.png";
    const string LedgeTilePath = TileDir + "map_tileset_cathedral_9.png";
    const string CeilingTilePath = TileDir + "map_tileset_cathedral_15.png";
    const string CeilingTileBPath = TileDir + "map_tileset_cathedral_15b.png";
    const string CeilingTileCPath = TileDir + "map_tileset_cathedral_15c.png";
    const string PropDir = "Assets/Art/Environment/Props/cathedral/";
    const string PropAngelPath = PropDir + "prop_cathedral_angel.png";
    const string PropLanternPath = PropDir + "prop_cathedral_hanging_lantern.png";
    const string PropFencePath = PropDir + "prop_cathedral_iron_fence.png";
    const string PropPewPath = PropDir + "prop_cathedral_broken_pew.png";
    const string BgPath = "Assets/Art/Environment/Backgrounds/map_bg_cathedral.png";
    const string HolyWaterCeilingPath = "Assets/Art/Environment/Hazards/holywater_ceiling_block.png";
    const string UnlitMatPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    const float CeilingSegmentWorldW = 2.55f;
    const float CeilingStrideFactor = 0.93f;
    const float CeilingHeightAboveFloor = 4.2f;
    static readonly Color CeilingTint = new Color(0.78f, 0.80f, 0.84f, 1f);
    const float FloorStartX = -5.2f;
    const float FloorEndX = 10.2f;
    const float TileScale = 2f;

    readonly CathedralIntroZoneA zone;

    ZoneASeeder(CathedralIntroZoneA zone)
    {
        this.zone = zone;
    }

    public static void SeedIfEmpty(CathedralIntroZoneA zone)
    {
        if (zone == null)
            return;
        if (zone.HasSeededContent)
        {
            zone.BindPlaySession();
            return;
        }

        Rebuild(zone);
    }

    public static void Rebuild(CathedralIntroZoneA zone)
    {
        if (zone == null)
            return;
        new ZoneASeeder(zone).RebuildInternal();
    }

    void RebuildInternal()
    {
        ClearSpawned();
        BuildFromCode();
    }

    void BuildFromCode()
    {
        ApplyCameraDefaults();

        var floor = LoadSprite(FloorTilePath, "map_tileset_cathedral_1")
                    ?? MakeSolidSprite(new Color(0.28f, 0.29f, 0.32f), 256, 64);
        var cracked = LoadSprite(CrackedTilePath, "map_tileset_cathedral_2") ?? floor;
        var fill = LoadSprite(FillTilePath, "map_tileset_cathedral_0") ?? floor;
        var wallEnd = LoadSprite(WallEndTilePath, "map_tileset_cathedral_4") ?? fill;
        var ledge = LoadSprite(LedgeTilePath, "map_tileset_cathedral_9") ?? floor;
        var column = LoadSprite(ColumnTilePath, "map_tileset_cathedral_5") ?? floor;
        var ceilingTile = LoadSprite(CeilingTilePath, "map_tileset_cathedral_15")
                          ?? LoadSprite(CeilingTilePath, "map_tileset_cathedral_15_0");
        var ceilingTileB = LoadSprite(CeilingTileBPath, "map_tileset_cathedral_15b")
                           ?? LoadSprite(CeilingTileBPath, null);
        var ceilingTileC = LoadSprite(CeilingTileCPath, "map_tileset_cathedral_15c")
                           ?? LoadSprite(CeilingTileCPath, null);

        var bg = LoadSprite(BgPath, "map_bg_cathedral_0");
        var propGate = LoadSprite(PropLanternPath, "prop_cathedral_hanging_lantern");
        var propAngel = LoadSprite(PropAngelPath, "prop_cathedral_angel");
        var propBanner = LoadSprite(PropFencePath, "prop_cathedral_iron_fence");
        var propRuin = LoadSprite(PropPewPath, "prop_cathedral_broken_pew");

        var sprites = new Dictionary<string, Sprite>(64);
        void Register(Sprite s)
        {
            if (s != null && !string.IsNullOrEmpty(s.name))
                sprites[s.name] = s;
        }

        Register(floor);
        Register(cracked);
        Register(fill);
        Register(wallEnd);
        Register(ledge);
        Register(column);
        Register(ceilingTile);
        Register(ceilingTileB);
        Register(ceilingTileC);
        var dripCeiling = LoadSprite(HolyWaterCeilingPath, "holywater_ceiling_block");
        Register(dripCeiling);
        Register(bg);
        Register(propGate);
        Register(propAngel);
        Register(propBanner);
        Register(propRuin);

        var platforms = Child("Platforms");
        var decor = Child("Decor");
        var atmosphere = Child("Atmosphere");

        float floorTop = floor.bounds.size.y * TileScale;
        var layout = LoadBakedLayout();
        if (layout != null && layout.entries != null && layout.entries.Length > 0)
        {
            ApplyBakedLayout(layout, platforms, decor, atmosphere, sprites);
        }
        else
        {
            Debug.LogWarning(
                "Zone A: no layout backup found — seeding floor/bg defaults. Hand-tune, then Ctrl+S the Scene.");
            PlaceBackground(atmosphere, bg);
            PlaceContinuousFloor(platforms, floor);
        }

        float holyX = layout != null ? layout.holyWaterX : -0.55f;
        if (!LayoutHasCeiling(layout))
        {
            var dryVariants = BuildCeilingDryVariants(ceilingTile, ceilingTileB, ceilingTileC);
            PlaceContinuousCeiling(platforms, dryVariants, dripCeiling, floorTop, holyX);
        }

        Vector2? hitSize = null;
        Vector2? hitOffset = null;
        Vector2? hitLocal = null;
        if (layout != null && layout.hasHolyWaterHitbox)
        {
            hitSize = new Vector2(layout.holyHitW, layout.holyHitH);
            hitOffset = new Vector2(layout.holyHitOx, layout.holyHitOy);
            hitLocal = new Vector2(layout.holyHitLx, layout.holyHitLy);
        }

        PlaceHolyWater(atmosphere, puddleX: holyX, floorTop: floorTop,
            hitboxSize: hitSize, hitboxOffset: hitOffset, hitboxLocalPos: hitLocal);

        float spawnX = layout != null ? layout.playerX : -3.4f;
        float spawnY = layout != null ? layout.playerY : floorTop + 0.02f;
        var player = SpawnPlayer(new Vector3(spawnX, spawnY, 0f));

        var cam = Camera.main;
        if (cam != null && player != null)
        {
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.SetTarget(player.transform);
            float cx = layout != null ? layout.camX : -3.4f;
            float cy = layout != null ? layout.camY : 2.6f;
            float cz = layout != null ? layout.camZ : -10f;
            cam.transform.position = new Vector3(cx, cy, cz);
        }

        if (!Application.isPlaying)
        {
            var view = SceneView.lastActiveSceneView;
            view?.Frame(new Bounds(new Vector3(0.5f, 2f, 0f), new Vector3(16f, 10f, 1f)), false);
            EditorUtility.SetDirty(zone);
            if (zone.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(zone.gameObject.scene);
        }

        Debug.Log("Zone A seeded from Editor. Hand-tune in Hierarchy, then Ctrl+S. Reset only to wipe.");
    }

    static bool LayoutHasCeiling(ZoneALayoutSnapshot layout)
    {
        if (layout?.entries == null)
            return false;
        for (int i = 0; i < layout.entries.Length; i++)
        {
            var e = layout.entries[i];
            if (e != null && e.role == "ceiling")
                return true;
        }

        return false;
    }

    ZoneALayoutSnapshot LoadBakedLayout()
    {
        string json = null;
        string absolute = CathedralIntroZoneA.LayoutAbsolutePath;
        if (File.Exists(absolute))
            json = File.ReadAllText(absolute);
        if (string.IsNullOrEmpty(json) && zone.BakedLayout != null)
            json = zone.BakedLayout.text;
        if (string.IsNullOrEmpty(json))
            return null;

        if (json.Length > 0 && json[0] == '\uFEFF')
            json = json.Substring(1);

        try
        {
            var snap = JsonUtility.FromJson<ZoneALayoutSnapshot>(json);
            if (snap == null || snap.entries == null || snap.entries.Length == 0)
            {
                Debug.LogWarning(
                    "Zone A: baked layout parsed empty — check ZoneA_Layout.json (BOM/corrupt). " +
                    $"jsonHead='{(json.Length > 24 ? json.Substring(0, 24) : json)}'");
            }

            return snap;
        }
        catch (Exception e)
        {
            Debug.LogError($"Zone A: failed to parse baked layout: {e.Message}");
            return null;
        }
    }

    void ApplyBakedLayout(
        ZoneALayoutSnapshot layout,
        Transform platforms,
        Transform decor,
        Transform atmosphere,
        Dictionary<string, Sprite> sprites)
    {
        if (layout.entries != null)
        {
            for (int i = 0; i < layout.entries.Length; i++)
            {
                var e = layout.entries[i];
                if (e == null || string.IsNullOrEmpty(e.spriteName))
                    continue;
                if (!sprites.TryGetValue(e.spriteName, out var sprite) || sprite == null)
                {
                    bool found = false;
                    foreach (var kv in sprites)
                    {
                        if (kv.Key.StartsWith(e.spriteName) || e.spriteName.StartsWith(kv.Key))
                        {
                            sprite = kv.Value;
                            found = sprite != null;
                            if (found) break;
                        }
                    }

                    if (!found)
                    {
                        Debug.LogWarning($"Zone A bake: missing sprite '{e.spriteName}', skip.");
                        continue;
                    }
                }

                Transform parent = platforms;
                if (e.bucket == "Decor")
                    parent = decor;
                else if (e.bucket == "Atmosphere")
                    parent = atmosphere;

                var scale = new Vector3(e.sx, e.sy, e.sz);
                var tint = new Color(e.cr, e.cg, e.cb, e.ca);
                bool isCeiling = e.role == "ceiling";
                string goName = isCeiling
                    ? (e.spriteName.StartsWith("Ceiling_") ? e.spriteName : $"Ceiling_{sprite.name}")
                    : sprite.name;

                if (e.role == "bg")
                {
                    var go = InstantiateEnv(zone.EnvBackgroundPrefab, parent, "BG_Cathedral");
                    go.transform.position = new Vector3(e.x, e.y, e.z);
                    go.transform.localScale = scale;
                    var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
                    ApplySprite(sr, sprite, e.sortingLayer, e.sortingOrder);
                    sr.color = tint;
                    continue;
                }

                if (e.bucket == "Decor" || e.role == "decor")
                {
                    PlaceDecor(parent, sprite, e.x, e.y, targetHeight: 1f, e.sortingOrder, scale, tint);
                    continue;
                }

                Vector2? customColSize = null;
                Vector2? customColOffset = null;
                if (e.hasCustomCollider)
                {
                    customColSize = new Vector2(e.colW, e.colH);
                    customColOffset = new Vector2(e.colOx, e.colOy);
                }

                PlacePlatform(parent, sprite, e.x, e.y, scale, e.sortingLayer, e.sortingOrder, tint,
                    withCollider: e.withCollider, objectName: goName, ceilingCollider: isCeiling,
                    customColliderSize: customColSize, customColliderOffset: customColOffset);
            }
        }

        if (layout.blockers == null)
            return;

        for (int i = 0; i < layout.blockers.Length; i++)
        {
            var b = layout.blockers[i];
            if (b == null)
                continue;
            PlaceSolidBlocker(platforms, b.x, b.y, new Vector2(b.w, b.h), new Vector2(b.ox, b.oy));
        }
    }

    void PlaceContinuousFloor(Transform parent, Sprite solid)
    {
        float solidW = solid.bounds.size.x * TileScale;
        float stride = solidW * 0.98f;
        int floorCount = Mathf.Max(1, Mathf.CeilToInt((FloorEndX - FloorStartX) / Mathf.Max(0.01f, stride)) + 1);
        PlaceRow(parent, solid, startX: FloorStartX, y: 0f, count: floorCount, scale: TileScale);
    }

    static Sprite[] BuildCeilingDryVariants(Sprite a, Sprite b, Sprite c)
    {
        var list = new List<Sprite>(3);
        if (a != null) list.Add(a);
        if (b != null) list.Add(b);
        if (c != null) list.Add(c);
        return list.Count == 0 ? Array.Empty<Sprite>() : list.ToArray();
    }

    void PlaceContinuousCeiling(
        Transform parent,
        Sprite[] dryVariants,
        Sprite dripTile,
        float floorTop,
        float dripX)
    {
        float ceilingY = floorTop + CeilingHeightAboveFloor;
        float stride = CeilingSegmentWorldW * CeilingStrideFactor;
        int variantCount = dryVariants != null ? dryVariants.Length : 0;

        if (dripTile != null)
        {
            PlaceCeilingSegment(parent, dripTile, dripX, ceilingY, CeilingSegmentWorldW,
                "Ceiling_holywater_ceiling_block", sortingOrder: 3, tint: CeilingTint);
        }

        if (variantCount == 0)
            return;

        int leftIndex = 0;
        for (float x = dripX - stride; x >= FloorStartX - stride * 0.25f; x -= stride)
        {
            int vi = Mathf.Abs(Mathf.RoundToInt(x * 17f) + leftIndex * 3) % variantCount;
            var sprite = dryVariants[vi];
            PlaceCeilingSegment(parent, sprite, x, ceilingY, CeilingSegmentWorldW,
                $"Ceiling_{sprite.name}_{leftIndex}", sortingOrder: 2, tint: CeilingTint);
            leftIndex++;
        }

        int rightIndex = 0;
        for (float x = dripX + stride; x <= FloorEndX + stride * 0.25f; x += stride)
        {
            int vi = Mathf.Abs(Mathf.RoundToInt(x * 17f) + rightIndex * 5) % variantCount;
            var sprite = dryVariants[vi];
            PlaceCeilingSegment(parent, sprite, x, ceilingY, CeilingSegmentWorldW,
                $"Ceiling_{sprite.name}_{rightIndex}", sortingOrder: 2, tint: CeilingTint);
            rightIndex++;
        }
    }

    void PlaceCeilingSegment(
        Transform parent,
        Sprite sprite,
        float centerX,
        float ceilingBottomY,
        float targetWorldW,
        string objectName,
        int sortingOrder = 2,
        Color? tint = null,
        bool flipX = false)
    {
        if (sprite == null)
            return;

        var go = InstantiateEnv(zone.EnvSolidPrefab, parent, objectName);
        int groundLayer = GameLayers.GroundLayer;
        if (groundLayer >= 0)
            go.layer = groundLayer;

        float scale = targetWorldW / Mathf.Max(0.01f, sprite.bounds.size.x);
        go.transform.localScale = new Vector3(flipX ? -scale : scale, scale, scale);
        go.transform.position = new Vector3(centerX, ceilingBottomY, 0f);

        var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, sprite, "Platforms", sortingOrder);
        if (tint.HasValue)
            sr.color = tint.Value;

        float bottom = sr.bounds.min.y;
        go.transform.position += new Vector3(0f, ceilingBottomY - bottom, 0f);

        var col = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
        Bounds wb = sr.bounds;
        Vector3 localMin = go.transform.InverseTransformPoint(new Vector3(wb.min.x, wb.min.y, 0f));
        Vector3 localMax = go.transform.InverseTransformPoint(new Vector3(wb.max.x, wb.max.y, 0f));
        float localW = Mathf.Abs(localMax.x - localMin.x);
        float localH = Mathf.Abs(localMax.y - localMin.y);
        float thickness = Mathf.Clamp(localH * 0.18f, 0.14f, 0.28f);
        col.size = new Vector2(localW * 1.04f, thickness);
        col.offset = new Vector2((localMin.x + localMax.x) * 0.5f, localMin.y + thickness * 0.5f);
    }

    void PlaceHolyWater(
        Transform parent,
        float puddleX,
        float floorTop,
        Vector2? hitboxSize = null,
        Vector2? hitboxOffset = null,
        Vector2? hitboxLocalPos = null)
    {
        float puddleY = floorTop + 0.02f;
        var root = InstantiateEnv(zone.HolyWaterPrefab, parent, "HolyWater");
        root.transform.position = new Vector3(puddleX, puddleY, 0f);

        var hit = root.transform.Find("BurnHitbox");
        if (hit == null)
            return;

        var hitLocal = hitboxLocalPos ?? (Vector2)hit.localPosition;
        hit.localPosition = new Vector3(hitLocal.x, hitLocal.y, 0f);

        var col = hit.GetComponent<BoxCollider2D>();
        if (col == null)
            return;
        if (hitboxSize.HasValue)
            col.size = hitboxSize.Value;
        if (hitboxOffset.HasValue)
            col.offset = hitboxOffset.Value;
    }

    void PlaceBackground(Transform parent, Sprite bg)
    {
        if (bg == null)
            return;

        var go = InstantiateEnv(zone.EnvBackgroundPrefab, parent, "BG_Cathedral");
        go.transform.position = new Vector3(1f, 3.2f, 1f);
        float targetWidth = 22f;
        float scale = targetWidth / Mathf.Max(0.01f, bg.bounds.size.x);
        go.transform.localScale = Vector3.one * scale;

        var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, bg, "Background", -10);
        sr.color = new Color(0.85f, 0.86f, 0.9f, 1f);
    }

    void PlaceSolidBlocker(Transform parent, float x, float y, Vector2 size, Vector2 offset = default)
    {
        var go = InstantiateEnv(zone.EnvBlockerPrefab, parent, "WallBlocker");
        int groundLayer = GameLayers.GroundLayer;
        if (groundLayer >= 0)
            go.layer = groundLayer;
        go.transform.position = new Vector3(x, y, 0f);

        var col = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
        col.size = size;
        col.offset = offset;
    }

    void PlaceDecor(
        Transform parent,
        Sprite sprite,
        float x,
        float y,
        float targetHeight,
        int sortingOrder,
        Vector3? localScale = null,
        Color? tint = null)
    {
        if (sprite == null)
            return;

        var go = InstantiateEnv(zone.EnvDecorPrefab, parent, sprite.name);
        go.transform.position = new Vector3(x, y, 0f);
        if (localScale.HasValue)
            go.transform.localScale = localScale.Value;
        else
        {
            float scale = targetHeight / Mathf.Max(0.01f, sprite.bounds.size.y);
            go.transform.localScale = Vector3.one * scale;
        }

        var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, sprite, "Midground", sortingOrder);
        if (tint.HasValue)
            sr.color = tint.Value;
    }

    void ClearSpawned()
    {
        var t = zone.transform;
        for (int i = t.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(t.GetChild(i).gameObject);
    }

    Transform Child(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(zone.transform, false);
        return go.transform;
    }

    static GameObject InstantiateEnv(GameObject prefab, Transform parent, string name)
    {
        if (prefab == null)
        {
            var fallback = new GameObject(name);
            fallback.transform.SetParent(parent, false);
            return fallback;
        }

        var editorGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        editorGo.name = name;
        return editorGo;
    }

    PlayerController SpawnPlayer(Vector3 position)
    {
        if (zone.PlayerPrefab == null)
        {
            Debug.LogError(
                "CathedralIntroZoneA: Player Prefab is not assigned. " +
                "Use Tools/Cathedral/Create or Update Player Prefab.");
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(zone.PlayerPrefab.gameObject, zone.transform);
        go.name = "Player";
        go.transform.position = position;
        var editorPlayer = go.GetComponent<PlayerController>();
        editorPlayer.SnapToGround();
        editorPlayer.SpawnPosition = editorPlayer.transform.position;
        return editorPlayer;
    }

    void ApplyCameraDefaults()
    {
        var cam = Camera.main;
        if (cam == null)
            return;

        cam.orthographic = true;
        cam.orthographicSize = 4.5f;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        if (cam.transform.position.z > -5f)
            cam.transform.position = new Vector3(-3f, 2.5f, -10f);

        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null)
            follow.SetOrthoSize(4.5f);
    }

    void PlaceRow(Transform parent, Sprite sprite, float startX, float y, int count, float scale)
    {
        float width = sprite.bounds.size.x * scale * 0.98f;
        for (int i = 0; i < count; i++)
            PlacePlatform(parent, sprite, startX + i * width, y, scale);
    }

    void PlacePlatform(
        Transform parent,
        Sprite sprite,
        float x,
        float y,
        float scale,
        string sortingLayer = "Platforms",
        int order = 0,
        Color? tint = null,
        bool withCollider = true,
        bool flipX = false)
    {
        PlacePlatform(parent, sprite, x, y,
            new Vector3(flipX ? -scale : scale, scale, scale),
            sortingLayer, order, tint, withCollider);
    }

    void PlacePlatform(
        Transform parent,
        Sprite sprite,
        float x,
        float y,
        Vector3 localScale,
        string sortingLayer = "Platforms",
        int order = 0,
        Color? tint = null,
        bool withCollider = true,
        string objectName = null,
        bool ceilingCollider = false,
        Vector2? customColliderSize = null,
        Vector2? customColliderOffset = null)
    {
        var go = InstantiateEnv(
            withCollider ? zone.EnvSolidPrefab : zone.EnvDecorPrefab,
            parent,
            objectName ?? (sprite != null ? sprite.name : "Platform"));
        if (withCollider)
        {
            int groundLayer = GameLayers.GroundLayer;
            if (groundLayer >= 0)
                go.layer = groundLayer;
        }

        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = localScale;

        var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, sprite, sortingLayer, order);
        if (tint.HasValue)
            sr.color = tint.Value;

        if (!withCollider || sprite == null)
        {
            var extra = go.GetComponent<BoxCollider2D>();
            if (extra != null)
                Undo.DestroyObjectImmediate(extra);
            return;
        }

        var col = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
        if (customColliderSize.HasValue)
        {
            col.size = customColliderSize.Value;
            col.offset = customColliderOffset ?? Vector2.zero;
            return;
        }

        float w = sprite.bounds.size.x;
        float h = sprite.bounds.size.y;
        if (ceilingCollider)
        {
            float thickness = Mathf.Clamp(h * 0.18f, 0.14f, 0.28f);
            col.size = new Vector2(w * 1.04f, thickness);
            col.offset = new Vector2(0f, thickness * 0.5f);
        }
        else
        {
            float thickness = Mathf.Clamp(h * 0.2f, 0.16f, 0.32f);
            col.size = new Vector2(w * 1.04f, thickness);
            col.offset = new Vector2(0f, h - thickness * 0.5f);
        }
    }

    static void ApplySprite(SpriteRenderer renderer, Sprite sprite, string sortingLayer, int order)
    {
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;
        renderer.color = Color.white;
        var mat = AssetDatabase.LoadAssetAtPath<Material>(UnlitMatPath);
        if (mat != null)
            renderer.sharedMaterial = mat;
    }

    static Sprite LoadSprite(string assetPath, string spriteName)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets == null || assets.Length == 0)
            return null;

        if (!string.IsNullOrEmpty(spriteName))
        {
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            }

            foreach (var asset in assets)
            {
                if (asset is Sprite sprite &&
                    (sprite.name == spriteName + "_0" || sprite.name.StartsWith(spriteName)))
                    return sprite;
            }
        }

        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        return null;
    }

    static Sprite MakeSolidSprite(Color color, int width, int height)
    {
        var texture = new Texture2D(width, height);
        var pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), 256f);
    }
}
