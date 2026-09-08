using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds Cathedral Intro Zone A (裂隙坑): spawn, getup, sealed left wall, fissure mood, optional holy water.
/// Hand-tune Hierarchy, then Inspector → Bake Hierarchy → Layout (writes ZoneA_Layout.json).
/// Rebuild Zone A clears children and reapplies the baked layout.
/// See Docs/Rooms/cathedral_intro.md §3.A
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class CathedralIntroZoneA : MonoBehaviour
{
    const string IdlePath = "Assets/Art/Characters/protagonist_cursed_pilgrim_chibi.png";
    const string RunPath = "Assets/Art/Characters/protagonist_cursed_pilgrim_chibi_run.png";
    const string IdleSheetPath = "Assets/Art/Characters/Animations/protagonist_cursed_pilgrim_chibi_idle_sheet.png";
    const string IdleOathbladeSheetPath = "Assets/Art/Characters/Animations/protagonist_cursed_pilgrim_chibi_idle_oathblade_sheet.png";
    const string RunSheetPath = "Assets/Art/Characters/Animations/protagonist_cursed_pilgrim_chibi_run_sheet.png";
    const string JumpSheetPath = "Assets/Art/Characters/Animations/protagonist_cursed_pilgrim_chibi_jump_sheet.png";
    const string GetupSheetPath = "Assets/Art/Characters/Animations/protagonist_cursed_pilgrim_chibi_getup_sheet.png";
    const string TileDir = "Assets/Art/Environment/Tilesets/cathedral/";
    const string FloorTilePath = TileDir + "map_tileset_cathedral_1.png";
    const string FillTilePath = TileDir + "map_tileset_cathedral_0.png";
    const string CrackedTilePath = TileDir + "map_tileset_cathedral_2.png";
    const string WallEndTilePath = TileDir + "map_tileset_cathedral_4.png";
    const string ColumnTilePath = TileDir + "map_tileset_cathedral_5.png";
    const string LedgeTilePath = TileDir + "map_tileset_cathedral_9.png";
    const string PropsPath = "Assets/Art/Environment/Props/map_props_cathedral.png";
    const string BgPath = "Assets/Art/Environment/Backgrounds/map_bg_cathedral.png";
    const string HolyWaterDecalPath = "Assets/Art/Environment/Hazards/map_hazard_holywater_floor_decal.png";
    const string HolyWaterFlatPath = "Assets/Art/Environment/Hazards/map_hazard_holywater_cathedral_flat.png";

    const float TileScale = 2f;

    /// <summary>Baked Hierarchy layout (JSON). Rebuild prefers this over hard-coded constants.</summary>
    [SerializeField] TextAsset bakedLayout;

    public const string LayoutAssetPath = "Assets/Scripts/World/ZoneA_Layout.json";

    [SerializeField] bool buildOnEnable = true;

    void OnEnable()
    {
        if (buildOnEnable)
            Build();
    }

    [ContextMenu("Rebuild Zone A")]
    public void Rebuild()
    {
        ClearSpawned();
        BuildFromCode();
    }

    /// <summary>
    /// Keeps Hierarchy transforms when layout already exists.
    /// Full wipe → code defaults only via Context Menu "Rebuild Zone A".
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
    /// Play / domain-reload path: rebind anim + drip FX, leave all transforms alone.
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

        RebindHolyWaterDripFx();
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

        var bg = LoadSprite(BgPath, "map_bg_cathedral_0");
        var propGate = LoadSprite(PropsPath, "map_props_cathedral_3");
        var propAngel = LoadSprite(PropsPath, "map_props_cathedral_0");
        var propBanner = LoadSprite(PropsPath, "map_props_cathedral_6");
        var propRuin = LoadSprite(PropsPath, "map_props_cathedral_4");

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
        Register(bg);
        Register(propGate);
        Register(propAngel);
        Register(propBanner);
        Register(propRuin);

        var idleFrames = BuildIdleCycle(LoadSheet(IdleSheetPath));
        var idleArmedFrames = BuildIdleCycle(LoadSheet(IdleOathbladeSheetPath));
        var runFrames = LoadSheet(RunSheetPath);
        var jumpFrames = LoadSheet(JumpSheetPath);
        var getupFrames = BuildGetupCycle(LoadSheet(GetupSheetPath), idleFrames);
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
            ApplyBakedLayout(layout, platforms, decor, atmosphere, sprites, floorTop);
        }
        else
        {
            Debug.LogWarning(
                "Zone A: no baked layout found. Use Inspector → Bake Hierarchy → Layout, then Rebuild.");
            PlaceBackground(atmosphere, bg);
            PlaceContinuousFloor(platforms, floor);
        }

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
        }
#endif
        Debug.Log("Zone A (裂隙坑) ready. Bake Hierarchy / Bake Colliders → Layout to save hand edits; Rebuild reapplies the bake.");
    }

    ZoneALayoutSnapshot LoadBakedLayout()
    {
        string json = null;
        if (bakedLayout != null)
            json = bakedLayout.text;
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(json) && System.IO.File.Exists(LayoutAssetPath))
            json = System.IO.File.ReadAllText(LayoutAssetPath);
#endif
        if (string.IsNullOrEmpty(json))
            return null;
        try
        {
            return JsonUtility.FromJson<ZoneALayoutSnapshot>(json);
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
        System.Collections.Generic.Dictionary<string, Sprite> sprites,
        float floorTop)
    {
        float ceilingY = floorTop + 4.2f;
        if (layout.entries != null)
        {
            for (int i = 0; i < layout.entries.Length; i++)
            {
                var e = layout.entries[i];
                if (e == null || string.IsNullOrEmpty(e.spriteName))
                    continue;
                if (!sprites.TryGetValue(e.spriteName, out var sprite) || sprite == null)
                {
                    Debug.LogWarning($"Zone A bake: missing sprite '{e.spriteName}', skip.");
                    continue;
                }

                Transform parent = platforms;
                if (e.bucket == "Decor")
                    parent = decor;
                else if (e.bucket == "Atmosphere")
                    parent = atmosphere;

                var scale = new Vector3(e.sx, e.sy, e.sz);
                var tint = new Color(e.cr, e.cg, e.cb, e.ca);
                bool isCeiling = e.role == "ceiling";
                string goName = isCeiling ? $"Ceiling_{sprite.name}" : sprite.name;

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

                if (isCeiling)
                    ceilingY = Mathf.Max(ceilingY, e.y);
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

        if (layout.hasHolyWater)
        {
            Vector2? hitSize = null;
            Vector2? hitOffset = null;
            Vector2? hitLocal = null;
            if (layout.hasHolyWaterHitbox)
            {
                hitSize = new Vector2(layout.holyHitW, layout.holyHitH);
                hitOffset = new Vector2(layout.holyHitOx, layout.holyHitOy);
                hitLocal = new Vector2(layout.holyHitLx, layout.holyHitLy);
            }

            PlaceHolyWater(atmosphere, puddleX: layout.holyWaterX, floorTop: floorTop, ceilingY: ceilingY,
                hitboxSize: hitSize, hitboxOffset: hitOffset, hitboxLocalPos: hitLocal);
        }
    }

    /// <summary>
    /// Domain reload clears private drip sprite refs; rebind art without moving transforms.
    /// </summary>
    void RebindHolyWaterDripFx()
    {
        var drip = GetComponentInChildren<HolyWaterDripFx>(true);
        if (drip == null)
            return;

        SpriteRenderer puddleSr = null;
        var holyRoot = drip.transform.parent;
        if (holyRoot != null)
        {
            var puddle = holyRoot.Find("BurnPuddle");
            if (puddle != null)
                puddleSr = puddle.GetComponent<SpriteRenderer>();
        }

        var dripList = new List<Sprite>(3);
        var drop0 = LoadSprite(HolyWaterDecalPath, "holywater_drop_0");
        var drop1 = LoadSprite(HolyWaterDecalPath, "holywater_drop_1");
        var drop2 = LoadSprite(HolyWaterDecalPath, "holywater_drop_2");
        if (drop0 != null) dripList.Add(drop0);
        if (drop1 != null) dripList.Add(drop1);
        if (drop2 != null) dripList.Add(drop2);

        var impact = LoadSprite(HolyWaterDecalPath, "holywater_impact");
        var ripple = LoadSprite(HolyWaterFlatPath, "holywater_flat_ripple")
                     ?? impact;

        drip.RebindArt(
            dripFrames: dripList.ToArray(),
            impact: impact,
            ripple: ripple,
            material: puddleSr != null ? puddleSr.sharedMaterial : null,
            puddle: puddleSr);
    }

    void PlaceContinuousFloor(Transform parent, Sprite solid)
    {
        float solidW = solid.bounds.size.x * TileScale;
        const float floorStart = -5.2f;
        const float floorEnd = 10.2f;
        float stride = solidW * 0.98f;
        int floorCount = Mathf.Max(1, Mathf.CeilToInt((floorEnd - floorStart) / Mathf.Max(0.01f, stride)) + 1);
        PlaceRow(parent, solid, startX: floorStart, y: 0f, count: floorCount, scale: TileScale);
    }

    void PlaceCeilingTile(
        Transform parent,
        Sprite sprite,
        float x,
        float y,
        float scale,
        bool withCollider = true,
        Color? tint = null,
        int sortingOrder = 1)
    {
        PlacePlatform(parent, sprite, x, y, Vector3.one * scale, "Platforms", sortingOrder, tint,
            withCollider: withCollider, objectName: sprite != null ? $"Ceiling_{sprite.name}" : "Ceiling",
            ceilingCollider: true);
    }

    void PlaceHolyWater(
        Transform parent,
        float puddleX,
        float floorTop,
        float ceilingY,
        Vector2? hitboxSize = null,
        Vector2? hitboxOffset = null,
        Vector2? hitboxLocalPos = null)
    {
        float puddleY = floorTop + 0.02f;
        float fallDistance = Mathf.Max(0.5f, ceilingY - puddleY - 0.15f);

        var burn = LoadSprite(HolyWaterDecalPath, "holywater_burn");
        var burnSmall = LoadSprite(HolyWaterDecalPath, "holywater_burn_small");
        var drop0 = LoadSprite(HolyWaterDecalPath, "holywater_drop_0");
        var drop1 = LoadSprite(HolyWaterDecalPath, "holywater_drop_1");
        var drop2 = LoadSprite(HolyWaterDecalPath, "holywater_drop_2");
        var impact = LoadSprite(HolyWaterDecalPath, "holywater_impact");
        var ripple = LoadSprite(HolyWaterFlatPath, "holywater_flat_ripple")
                     ?? LoadSprite(HolyWaterDecalPath, "holywater_impact");
        var ceilingDrip = LoadSprite(HolyWaterFlatPath, "holywater_ceiling_drip_a")
                          ?? LoadSprite(HolyWaterFlatPath, "holywater_ceiling_drip_b");

        var root = new GameObject("HolyWater");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(puddleX, puddleY, 0f);

        // Floor burn decal — slightly desaturated so it reads as a stain, not a neon UI disc.
        var puddle = new GameObject("BurnPuddle");
        puddle.transform.SetParent(root.transform, false);
        puddle.transform.localPosition = Vector3.zero;
        var puddleSr = puddle.AddComponent<SpriteRenderer>();
        if (burn != null)
        {
            ApplySprite(puddleSr, burn, "Hazards", 1);
            float targetW = 1.15f;
            float scale = targetW / Mathf.Max(0.01f, burn.bounds.size.x);
            puddle.transform.localScale = Vector3.one * scale;
            puddleSr.color = new Color(0.78f, 0.92f, 0.88f, 0.82f);
        }
        else
        {
            puddle.transform.localScale = new Vector3(1.05f, 0.2f, 1f);
            ApplySprite(puddleSr, MakeSolidSprite(new Color(0.45f, 0.78f, 0.72f, 0.65f), 64, 16), "Hazards", 1);
            puddleSr.color = new Color(0.42f, 0.82f, 0.74f, 0.5f);
        }

        // Secondary smaller stain offset for irregular burn edge.
        if (burnSmall != null)
        {
            var small = new GameObject("BurnPuddle_Small");
            small.transform.SetParent(root.transform, false);
            small.transform.localPosition = new Vector3(0.48f, 0.01f, 0f);
            var smallSr = small.AddComponent<SpriteRenderer>();
            ApplySprite(smallSr, burnSmall, "Hazards", 0);
            float scale = 0.5f / Mathf.Max(0.01f, burnSmall.bounds.size.x);
            small.transform.localScale = Vector3.one * scale;
            smallSr.color = new Color(0.8f, 0.92f, 0.88f, 0.55f);
        }

        // Ceiling drip stone from flat sheet.
        if (ceilingDrip != null)
        {
            var ceil = new GameObject("CeilingDripStone");
            ceil.transform.SetParent(root.transform, false);
            ceil.transform.position = new Vector3(puddleX, ceilingY + 0.05f, 0f);
            var ceilSr = ceil.AddComponent<SpriteRenderer>();
            ApplySprite(ceilSr, ceilingDrip, "Midground", 6);
            float scale = 0.95f / Mathf.Max(0.01f, ceilingDrip.bounds.size.y);
            ceil.transform.localScale = Vector3.one * scale;
        }

        // Damage trigger — match visible burn width more closely; avoidable, not whole-pit.
        // Bake Colliders can override size / offset / local pose.
        float visualW = burn != null ? burn.bounds.size.x * puddle.transform.localScale.x : 1.05f;
        float hitW = Mathf.Clamp(visualW * 0.85f, 0.75f, 1.25f);
        var hit = new GameObject("BurnHitbox");
        int hazardLayer = GameLayers.HazardLayer;
        if (hazardLayer >= 0)
            hit.layer = hazardLayer;
        hit.transform.SetParent(root.transform, false);
        var hitLocal = hitboxLocalPos ?? new Vector2(0f, 0.12f);
        hit.transform.localPosition = new Vector3(hitLocal.x, hitLocal.y, 0f);
        hit.tag = "Hazard";

        var col = hit.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = hitboxSize ?? new Vector2(hitW, 0.28f);
        col.offset = hitboxOffset ?? Vector2.zero;

        var zone = hit.AddComponent<DamageZone>();
        zone.Configure(damageAmount: 1, knockX: 5f, knockY: 4f,
            firstHitFlavor: "圣水灼肤——这里不欢迎他/她。");

        // Animated drips: charge at ceiling → fall frames → impact + ripple + puddle pulse.
        var dripList = new List<Sprite>(3);
        if (drop0 != null) dripList.Add(drop0);
        if (drop1 != null) dripList.Add(drop1);
        if (drop2 != null) dripList.Add(drop2);
        var dripFrames = dripList.ToArray();

        var dripGo = new GameObject("Drips");
        dripGo.transform.SetParent(root.transform, false);
        dripGo.transform.position = new Vector3(puddleX, ceilingY - 0.15f, 0f);
        var drip = dripGo.AddComponent<HolyWaterDripFx>();
        drip.Configure(
            localSpawn: Vector3.zero,
            distance: fallDistance,
            dripFrames: dripFrames,
            impact: impact,
            ripple: ripple,
            material: puddleSr.sharedMaterial,
            worldHeight: 0.45f,
            puddle: puddleSr,
            intervalLo: 1.15f,
            intervalHi: 1.65f);
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
        var idleFrames = BuildIdleCycle(LoadSheet(IdleSheetPath));
        var idleArmedFrames = BuildIdleCycle(LoadSheet(IdleOathbladeSheetPath));
        var runFrames = LoadSheet(RunSheetPath);
        var jumpFrames = LoadSheet(JumpSheetPath);
        var getupFrames = BuildGetupCycle(LoadSheet(GetupSheetPath), idleFrames);
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

        // Defaults: Scene-tuned body capsule (hair/cloak stay outside). Bake Colliders overrides.
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
        if (Application.isPlaying)
            controller.SnapToGround();
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
        cam.orthographicSize = 7f;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        if (cam.transform.position.z > -5f)
            cam.transform.position = new Vector3(-3f, 2.5f, -10f);
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
        visual.transform.localScale = Vector3.one * 0.53f;

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

    static Sprite[] LoadSheet(string assetPath)
    {
        var frames = new List<Sprite>();
#if UNITY_EDITOR
        foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is Sprite sprite)
                frames.Add(sprite);
        }

        frames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
#endif
        return frames.ToArray();
    }

    static Sprite[] BuildIdleCycle(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0)
            return frames;

        if (frames.Length >= 4 &&
            frames[0] != null && frames[1] != null && frames[2] != null && frames[3] != null)
        {
            var rest = frames[0];
            var inhale = frames[1];
            var peak = frames[2];
            var exhale = frames[3];
            return new[] { rest, rest, inhale, peak, peak, exhale };
        }

        return frames;
    }

    static Sprite[] BuildGetupCycle(Sprite[] frames, Sprite[] idleFrames)
    {
        if (frames == null || frames.Length == 0)
            return frames;

        Sprite idle = null;
        if (idleFrames != null && idleFrames.Length > 0)
            idle = idleFrames[0];

        if (frames.Length >= 7 &&
            frames[0] != null && frames[1] != null && frames[2] != null &&
            frames[3] != null && frames[4] != null && frames[5] != null && frames[6] != null)
        {
            var lie = frames[0];
            var stir = frames[1];
            var crawl = frames[2];
            var kneel = frames[3];
            var half = frames[4];
            var mid = frames[5];
            var stand = frames[6];
            var settle = idle != null ? idle : stand;
            return new[]
            {
                lie, lie,
                stir, stir,
                crawl, crawl,
                kneel, kneel,
                half, half,
                mid, mid,
                stand, stand,
                settle, settle
            };
        }

        if (frames.Length >= 6 &&
            frames[0] != null && frames[1] != null && frames[2] != null &&
            frames[3] != null && frames[4] != null && frames[5] != null)
        {
            var lie = frames[0];
            var stir = frames[1];
            var crawl = frames[2];
            var kneel = frames[3];
            var rise = frames[4];
            var stand = frames[5];
            var settle = idle != null ? idle : stand;
            return new[]
            {
                lie, lie,
                stir, stir,
                crawl, crawl,
                kneel, kneel,
                rise, rise,
                stand, stand,
                settle, settle
            };
        }

        if (idle != null)
        {
            var withSettle = new Sprite[frames.Length + 2];
            frames.CopyTo(withSettle, 0);
            withSettle[frames.Length] = idle;
            withSettle[frames.Length + 1] = idle;
            return withSettle;
        }

        return frames;
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
