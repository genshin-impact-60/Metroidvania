using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Cathedral Intro Zone A (裂隙坑).
/// <para><b>Scene hierarchy is the source of truth</b> — hand-tune in Hierarchy, then Ctrl+S.</para>
/// OnEnable only rebinds the existing Player; it never wipes the scene.
/// Use Inspector → 重置生成 only to seed an empty zone or intentionally discard hand edits.
/// Optional: 导出备份 writes ZoneA_Layout.json (not required for daily work).
/// See Docs/Rooms/cathedral_intro.md §3.A
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class CathedralIntroZoneA : MonoBehaviour
{
    const string IdlePath = "Assets/Art/Characters/protagonist_cursed_pilgrim_chibi.png";
    const string RunPath = "Assets/Art/Characters/protagonist_cursed_pilgrim_chibi_run.png";
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
    const string HolyWaterBasinPath = "Assets/Art/Environment/Hazards/holywater_basin_pool.png";
    const string HolyWaterCeilingPath = "Assets/Art/Environment/Hazards/holywater_ceiling_block.png";

    /// <summary>World width of one ceiling cornice segment (narrower than floor tiles).</summary>
    const float CeilingSegmentWorldW = 2.55f;

    /// <summary>Horizontal stride as a fraction of segment width (&lt;1 = overlap, hides seams).</summary>
    const float CeilingStrideFactor = 0.93f;

    /// <summary>Underside Y of the continuous ceiling relative to floor top.</summary>
    const float CeilingHeightAboveFloor = 4.2f;

    static readonly Color CeilingTint = new Color(0.78f, 0.80f, 0.84f, 1f);

    const float FloorStartX = -5.2f;
    const float FloorEndX = 10.2f;

    const float TileScale = 2f;

    /// <summary>Optional JSON backup / seed for 重置生成. Daily edits live in the Scene.</summary>
    [SerializeField] TextAsset bakedLayout;

    public const string LayoutAssetPath = "Assets/Scripts/World/ZoneA_Layout.json";

    [SerializeField]
    [FormerlySerializedAs("buildOnEnable")]
    [Tooltip("If the zone already has content, only rebind Player on enable. Never wipes the Scene.")]
    bool refreshOnEnable = true;

    void OnEnable()
    {
        if (refreshOnEnable)
            Build();
    }

    /// <summary>
    /// Wipes children and regenerates from bake JSON + code defaults.
    /// Prefer saving the Scene for hand edits; call this only to reset.
    /// </summary>
    [ContextMenu("Regenerate Zone A (WIPES hand edits)")]
    public void Rebuild()
    {
        ClearSpawned();
        BuildFromCode();
    }

    /// <summary>
    /// If Platforms already exist (scene content), only refresh player bindings.
    /// If empty, seeds once via BuildFromCode.
    /// </summary>
    public void Build()
    {
        if (transform.Find("Platforms") != null)
        {
            RefreshExisting();
            return;
        }

        BuildFromCode();
    }

    /// <summary>
    /// Play / domain-reload path: rebind player anim, leave Hierarchy transforms alone.
    /// </summary>
    void RefreshExisting()
    {
        ApplyCameraDefaults();

        var player = GetComponentInChildren<PlayerController>();
        if (player != null)
        {
            RefreshPlayerAnimation(player);

            if (Application.isPlaying)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.bodyType = RigidbodyType2D.Dynamic;
            }

            // Hierarchy position is the spawn/respawn source of truth.
            player.SpawnPosition = player.transform.position;

            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.GetComponent<CameraFollow>();
                if (follow == null)
                    follow = cam.gameObject.AddComponent<CameraFollow>();
                follow.SetTarget(player.transform);
            }
        }
    }

    public void BuildFromCode()
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

        var sprites = new System.Collections.Generic.Dictionary<string, Sprite>(64);
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

        var idleFrames = PlayerAnimationLoader.BuildIdleCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.IdleDir));
        var idleArmedFrames = PlayerAnimationLoader.BuildIdleCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.IdleOathbladeDir));
        var runFrames = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.RunDir);
        var jumpFrames = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.JumpDir);
        var getupFrames = PlayerAnimationLoader.BuildGetupCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.GetupDir), idleFrames);
        if (idleFrames.Length == 0)
        {
            var idle = LoadSprite(IdlePath, "protagonist_cursed_pilgrim_chibi_0")
                       ?? MakeSolidSprite(new Color(0.9f, 0.9f, 0.92f), 128, 256);
            idleFrames = new[] { idle };
        }

        if (runFrames.Length == 0)
        {
            var run = LoadSprite(RunPath, "protagonist_cursed_pilgrim_chibi_run_0") ?? idleFrames[0];
            runFrames = new[] { run };
        }

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
        bool bakeHasCeiling = LayoutHasCeiling(layout);
        if (!bakeHasCeiling)
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

        var player = CreatePlayer(idleFrames, runFrames, getupFrames, jumpFrames, idleArmedFrames);
        player.transform.SetParent(transform, true);
        float spawnX = layout != null ? layout.playerX : -3.4f;
        float spawnY = layout != null ? layout.playerY : floorTop + 0.02f;
        player.transform.position = new Vector3(spawnX, spawnY, 0f);
        player.SnapToGround();
        player.SpawnPosition = player.transform.position;

        var cam = Camera.main;
        if (cam != null)
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

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var view = UnityEditor.SceneView.lastActiveSceneView;
            view?.Frame(new Bounds(new Vector3(0.5f, 2f, 0f), new Vector3(16f, 10f, 1f)), false);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            if (gameObject.scene.IsValid())
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif
        Debug.Log("Zone A seeded. Hand-tune in Hierarchy, then Ctrl+S save the Scene. Use 重置生成 only to wipe.");
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
#if UNITY_EDITOR
        // Prefer disk: TextAsset.text often stays stale right after Bake + ImportAsset.
        string absolute = LayoutAbsolutePath;
        if (System.IO.File.Exists(absolute))
            json = System.IO.File.ReadAllText(absolute);
#endif
        if (string.IsNullOrEmpty(json) && bakedLayout != null)
            json = bakedLayout.text;
        if (string.IsNullOrEmpty(json))
            return null;

        // Strip UTF-8 BOM — JsonUtility rejects BOM-prefixed JSON (returns defaults / fails).
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

#if UNITY_EDITOR
    public static string LayoutAbsolutePath =>
        System.IO.Path.Combine(Application.dataPath, "Scripts/World/ZoneA_Layout.json");
#endif

    void ApplyBakedLayout(
        ZoneALayoutSnapshot layout,
        Transform platforms,
        Transform decor,
        Transform atmosphere,
        System.Collections.Generic.Dictionary<string, Sprite> sprites)
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
                    // Single-sprite assets may use file name without suffix.
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
                    var go = new GameObject("BG_Cathedral");
                    go.transform.SetParent(parent, false);
                    go.transform.position = new Vector3(e.x, e.y, e.z);
                    go.transform.localScale = scale;
                    var sr = go.AddComponent<SpriteRenderer>();
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

        if (layout.blockers != null)
        {
            for (int i = 0; i < layout.blockers.Length; i++)
            {
                var b = layout.blockers[i];
                if (b == null)
                    continue;
                PlaceSolidBlocker(platforms, b.x, b.y, new Vector2(b.w, b.h), new Vector2(b.ox, b.oy));
            }
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
        var list = new System.Collections.Generic.List<Sprite>(3);
        if (a != null) list.Add(a);
        if (b != null) list.Add(b);
        if (c != null) list.Add(c);
        if (list.Count == 0)
            return System.Array.Empty<Sprite>();
        return list.ToArray();
    }

    /// <summary>
    /// Continuous Zone A cornice: dry variants with slight overlap, drip mid-segment on basin X.
    /// </summary>
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

    /// <summary>
    /// Places one ceiling segment with underside at <paramref name="ceilingBottomY"/>,
    /// regardless of sprite pivot.
    /// </summary>
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

        var go = new GameObject(objectName);
        int groundLayer = GameLayers.GroundLayer;
        if (groundLayer >= 0)
            go.layer = groundLayer;

        go.transform.SetParent(parent, false);
        float scale = targetWorldW / Mathf.Max(0.01f, sprite.bounds.size.x);
        go.transform.localScale = new Vector3(flipX ? -scale : scale, scale, scale);
        go.transform.position = new Vector3(centerX, ceilingBottomY, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, sprite, "Platforms", sortingOrder);
        if (tint.HasValue)
            sr.color = tint.Value;

        float bottom = sr.bounds.min.y;
        go.transform.position += new Vector3(0f, ceilingBottomY - bottom, 0f);

        // Thin collider along the underside (works for any pivot / flip).
        var col = go.AddComponent<BoxCollider2D>();
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

        var basin = LoadSprite(HolyWaterBasinPath, "holywater_basin_pool");

        var root = new GameObject("HolyWater");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(puddleX, puddleY, 0f);

        // Floor basin — keep footprint small so the burn stays avoidable.
        var basinGo = new GameObject("BasinPool");
        basinGo.transform.SetParent(root.transform, false);
        basinGo.transform.localPosition = Vector3.zero;
        var basinSr = basinGo.AddComponent<SpriteRenderer>();
        if (basin != null)
        {
            ApplySprite(basinSr, basin, "Hazards", 1);
            float targetW = 1.35f;
            float scale = targetW / Mathf.Max(0.01f, basin.bounds.size.x);
            basinGo.transform.localScale = Vector3.one * scale;
            basinSr.color = new Color(0.92f, 0.98f, 0.94f, 0.92f);
        }
        else
        {
            basinGo.transform.localScale = new Vector3(1.2f, 0.35f, 1f);
            ApplySprite(basinSr, MakeSolidSprite(new Color(0.35f, 0.72f, 0.55f, 0.7f), 64, 24), "Hazards", 1);
        }

        float visualW = basin != null
            ? basin.bounds.size.x * basinGo.transform.localScale.x
            : 1.2f;
        float hitW = Mathf.Clamp(visualW * 0.55f, 0.7f, 1.15f);
        var hit = new GameObject("BurnHitbox");
        int hazardLayer = GameLayers.HazardLayer;
        if (hazardLayer >= 0)
            hit.layer = hazardLayer;
        hit.transform.SetParent(root.transform, false);
        var hitLocal = hitboxLocalPos ?? new Vector2(0f, 0.18f);
        hit.transform.localPosition = new Vector3(hitLocal.x, hitLocal.y, 0f);
        hit.tag = "Hazard";

        var col = hit.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = hitboxSize ?? new Vector2(hitW, 0.32f);
        col.offset = hitboxOffset ?? Vector2.zero;

        var zone = hit.AddComponent<DamageZone>();
        zone.Configure(damageAmount: 1, knockX: 5f, knockY: 4f,
            firstHitFlavor: "圣水灼肤——这里不欢迎他/她。");
    }

    void PlaceBackground(Transform parent, Sprite bg)
    {
        if (bg == null)
            return;

        var go = new GameObject("BG_Cathedral");
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(1f, 3.2f, 1f);
        float targetWidth = 22f;
        float scale = targetWidth / Mathf.Max(0.01f, bg.bounds.size.x);
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, bg, "Background", -10);
        sr.color = new Color(0.85f, 0.86f, 0.9f, 1f);
    }

    void PlaceSolidBlocker(Transform parent, float x, float y, Vector2 size, Vector2 offset = default)
    {
        var go = new GameObject("WallBlocker");
        int groundLayer = GameLayers.GroundLayer;
        if (groundLayer >= 0)
            go.layer = groundLayer;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);

        var col = go.AddComponent<BoxCollider2D>();
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

        var go = new GameObject(sprite.name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        if (localScale.HasValue)
            go.transform.localScale = localScale.Value;
        else
        {
            float scale = targetHeight / Mathf.Max(0.01f, sprite.bounds.size.y);
            go.transform.localScale = Vector3.one * scale;
        }

        var sr = go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, sprite, "Midground", sortingOrder);
        if (tint.HasValue)
            sr.color = tint.Value;
    }

    void ClearSpawned()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroySafe(transform.GetChild(i).gameObject);
    }

    Transform Child(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    void RefreshPlayerAnimation(PlayerController controller)
    {
        var idleFrames = PlayerAnimationLoader.BuildIdleCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.IdleDir));
        var idleArmedFrames = PlayerAnimationLoader.BuildIdleCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.IdleOathbladeDir));
        var runFrames = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.RunDir);
        var jumpFrames = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.JumpDir);
        var getupFrames = PlayerAnimationLoader.BuildGetupCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.GetupDir), idleFrames);
        if (idleFrames.Length == 0 && runFrames.Length == 0 && getupFrames.Length == 0 && jumpFrames.Length == 0)
            return;
        if (runFrames.Length == 0)
            runFrames = idleFrames;
        bool keepArmed = controller.HasOathblade;
        controller.SetAnimationFrames(idleFrames, runFrames, getupFrames, jumpFrames, idleArmedFrames, keepArmed);
        var anim = controller.GetComponent<PlayerSpriteAnimator>();
        anim?.ConfigureIdleTiming(3f);
        anim?.ConfigureGetupTiming(6f);
        AlignPlayerPresentation(controller);
    }

    void AlignPlayerPresentation(PlayerController controller)
    {
        AlignPlayerPresentation(controller, LoadBakedLayout());
    }

    static void AlignPlayerPresentation(PlayerController controller, ZoneALayoutSnapshot layout)
    {
        if (controller == null)
            return;

        // Defaults: Scene-tuned body capsule (hair/cloak stay outside). Optional layout backup can override.
        var standingSize = new Vector2(0.5535295f, 1.705085f);
        var standingOffset = new Vector2(0.1400892f, 0.629543f);
        const float getupH = 0.42f;
        float standBottom = standingOffset.y - standingSize.y * 0.5f;
        var getupSize = new Vector2(0.7f, getupH);
        var getupOffset = new Vector2(standingOffset.x, standBottom + getupH * 0.5f);

        if (layout != null &&
            layout.TryGetPlayerColliders(out var bakedStand, out var bakedStandOff, out var bakedGetup, out var bakedGetupOff))
        {
            standingSize = bakedStand;
            standingOffset = bakedStandOff;
            getupSize = bakedGetup;
            getupOffset = bakedGetupOff;
        }

        controller.ConfigureColliders(
            standingSize: standingSize,
            standingOffset: standingOffset,
            getupSize: getupSize,
            getupOffset: getupOffset);
        ApplyVisualPresentationScale(controller);
        if (Application.isPlaying)
            controller.SnapToGround();
    }

    const float PlayerVisualScale = 0.62f;

    static void ApplyVisualPresentationScale(PlayerController controller)
    {
        if (controller == null)
            return;
        var sr = controller.GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
            return;
        float sign = Mathf.Sign(sr.transform.localScale.x);
        if (sign == 0f)
            sign = 1f;
        sr.transform.localScale = new Vector3(PlayerVisualScale * sign, PlayerVisualScale, PlayerVisualScale);
    }

    static void DestroySafe(UnityEngine.Object obj)
    {
        if (obj == null)
            return;
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
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

    PlayerController CreatePlayer(
        Sprite[] idleFrames,
        Sprite[] runFrames,
        Sprite[] getupFrames,
        Sprite[] jumpFrames,
        Sprite[] idleArmedFrames = null)
    {
        var root = new GameObject("Player");
        root.tag = "Player";
        int playerLayer = GameLayers.PlayerLayer;
        if (playerLayer >= 0)
            root.layer = playerLayer;
        root.transform.position = Vector3.zero;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = Application.isPlaying ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var capsule = root.AddComponent<CapsuleCollider2D>();
        capsule.direction = CapsuleDirection2D.Vertical;
        var layout = LoadBakedLayout();
        var standingSize = new Vector2(0.5535295f, 1.705085f);
        var standingOffset = new Vector2(0.1400892f, 0.629543f);
        if (layout != null &&
            layout.TryGetPlayerColliders(out var bakedStand, out var bakedStandOff, out _, out _))
        {
            standingSize = bakedStand;
            standingOffset = bakedStandOff;
        }

        capsule.size = standingSize;
        capsule.offset = standingOffset;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        visual.transform.localScale = Vector3.one * PlayerVisualScale;

        var sr = visual.AddComponent<SpriteRenderer>();
        var first = getupFrames != null && getupFrames.Length > 0 && getupFrames[0] != null
            ? getupFrames[0]
            : (idleFrames != null && idleFrames.Length > 0 ? idleFrames[0] : null);
        ApplySprite(sr, first, "Entities", 10);

        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(root.transform, false);
        // Final pose comes from PlayerController.SyncGroundCheckToCapsule after ConfigureColliders.
        float standBottom = standingOffset.y - standingSize.y * 0.5f;
        groundCheck.transform.localPosition = new Vector3(standingOffset.x, standBottom, 0f);

        root.AddComponent<PlayerSpriteAnimator>();
        root.AddComponent<PlayerHealth>();
        var controller = root.AddComponent<PlayerController>();
        controller.Bind(sr, groundCheck.transform);
        controller.SetAnimationFrames(idleFrames, runFrames, getupFrames, jumpFrames, idleArmedFrames);
        var anim = root.GetComponent<PlayerSpriteAnimator>();
        anim?.ConfigureIdleTiming(3f);
        anim?.ConfigureGetupTiming(6f);
        AlignPlayerPresentation(controller);
        return controller;
    }

    void PlaceRow(Transform parent, Sprite sprite, float startX, float y, int count, float scale)
    {
        // 0.98 stride = slight visual overlap; kills hairline seams from sprite padding.
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
        var go = new GameObject(objectName ?? (sprite != null ? sprite.name : "Platform"));
        if (withCollider)
        {
            int groundLayer = GameLayers.GroundLayer;
            if (groundLayer >= 0)
                go.layer = groundLayer;
        }

        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = localScale;

        var sr = go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, sprite, sortingLayer, order);
        if (tint.HasValue)
            sr.color = tint.Value;

        if (!withCollider || sprite == null)
            return;

        var col = go.AddComponent<BoxCollider2D>();
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

    static Material _spriteMaterial;

    static void ApplySprite(SpriteRenderer renderer, Sprite sprite, string sortingLayer, int order)
    {
        renderer.sprite = sprite;
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;
        renderer.color = Color.white;
        if (_spriteMaterial == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                         ?? Shader.Find("Sprites/Default");
            if (shader != null)
                _spriteMaterial = new Material(shader) { name = "ZoneASpriteUnlit" };
        }

        if (_spriteMaterial != null)
            renderer.sharedMaterial = _spriteMaterial;
    }

    static Sprite LoadSprite(string assetPath, string spriteName)
    {
#if UNITY_EDITOR
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets == null || assets.Length == 0)
            return null;

        if (!string.IsNullOrEmpty(spriteName))
        {
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            }

            // Single-sprite textures often use the file name; also accept *_0 auto-slices.
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
#endif
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
