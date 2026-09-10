using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class StepAPlayground : MonoBehaviour
{
    const string IdlePath = "Assets/Art/Characters/protagonist_cursed_pilgrim_chibi.png";
    const string RunPath = "Assets/Art/Characters/protagonist_cursed_pilgrim_chibi_run.png";
    const string TileDir = "Assets/Art/Environment/Tilesets/cathedral/";
    const string FloorTilePath = TileDir + "map_tileset_cathedral_1.png";
    const string CrackedTilePath = TileDir + "map_tileset_cathedral_2.png";
    const string LedgeTilePath = TileDir + "map_tileset_cathedral_9.png";

    [SerializeField] bool buildOnEnable = true;
    [SerializeField] [Tooltip("Preview armed idle after getup (残誓). Zone A intro stays unarmed.")]
    bool previewOathbladeIdle = true;

    void OnEnable()
    {
        if (buildOnEnable)
            Build();
    }

    [ContextMenu("Rebuild Playground")]
    public void Rebuild()
    {
        ClearSpawned();
        Build();
    }

    public void Build()
    {
        if (transform.Find("Platforms") != null)
        {
            var controller = GetComponentInChildren<PlayerController>();
            var anim = GetComponentInChildren<PlayerSpriteAnimator>();
            if (controller != null && anim != null && anim.HasRunCycle)
            {
                RefreshPlayerAnimation(controller);
                return;
            }

            ClearSpawned();
        }

        ApplyCameraDefaults();

        var idle = LoadSprite(IdlePath, "protagonist_cursed_pilgrim_chibi_0");
        var run = LoadSprite(RunPath, "protagonist_cursed_pilgrim_chibi_run_0");
        var floor = LoadSprite(FloorTilePath, "map_tileset_cathedral_1");
        var ledge = LoadSprite(LedgeTilePath, "map_tileset_cathedral_9");
        var cracked = LoadSprite(CrackedTilePath, "map_tileset_cathedral_2");

        var idleFrames = PlayerAnimationLoader.BuildIdleCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.IdleDir));
        var idleArmedFrames = PlayerAnimationLoader.BuildIdleCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.IdleOathbladeDir));
        var runFrames = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.RunDir);
        var jumpFrames = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.JumpDir);
        var getupRaw = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.GetupDir);
        if (idle == null)
            idle = MakeSolidSprite(new Color(0.9f, 0.9f, 0.92f), 128, 256);
        if (run == null)
            run = idle;
        if (idleFrames.Length == 0)
            idleFrames = new[] { idle };
        if (runFrames.Length == 0)
            runFrames = new[] { run };
        var getupFrames = PlayerAnimationLoader.BuildGetupCycle(getupRaw, idleFrames);
        if (floor == null)
            floor = MakeSolidSprite(new Color(0.28f, 0.29f, 0.32f), 256, 64);
        if (ledge == null)
            ledge = floor;
        if (cracked == null)
            cracked = floor;

        var platforms = new GameObject("Platforms").transform;
        platforms.SetParent(transform, false);

        const float scale = 2f;
        PlaceRow(platforms, floor, startX: -10f, y: 0f, count: 5, scale: scale);
        PlaceRow(platforms, cracked, startX: 1.7f, y: 0f, count: 4, scale: scale);
        PlacePlatform(platforms, ledge, 2.6f, 1.55f, scale);
        PlacePlatform(platforms, ledge, 6.4f, 2.65f, scale);

        float floorTop = floor.bounds.size.y * scale;
        var player = CreatePlayer(idleFrames, runFrames, getupFrames, jumpFrames, idleArmedFrames, previewOathbladeIdle);
        player.transform.SetParent(transform, true);
        // Capsule bottom is at local y=0; sit just above the platform top.
        player.transform.position = new Vector3(-6.5f, floorTop + 0.02f, 0f);
        player.SnapToGround();
        player.SpawnPosition = player.transform.position;

        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.SetTarget(player.transform);
            cam.transform.position = new Vector3(-6.5f, 2.4f, -10f);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            var view = UnityEditor.SceneView.lastActiveSceneView;
            view?.Frame(new Bounds(new Vector3(0f, 1.8f, 0f), new Vector3(26f, 12f, 1f)), false);
        }
#endif
        Debug.Log(previewOathbladeIdle
            ? "Step A playground ready. Armed idle preview on — getup then 残誓 idle. A/D move, Space jump."
            : "Step A playground ready. Press Play — getup plays first, then move with A/D and jump with Space.");
    }

    void ClearSpawned()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroySafe(transform.GetChild(i).gameObject);
    }

    void RefreshPlayerAnimation(PlayerController controller)
    {
        var idleFrames = PlayerAnimationLoader.BuildIdleCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.IdleDir));
        var idleArmedFrames = PlayerAnimationLoader.BuildIdleCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.IdleOathbladeDir));
        var runFrames = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.RunDir);
        var jumpFrames = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.JumpDir);
        var getupRaw = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.GetupDir);
        var getupFrames = PlayerAnimationLoader.BuildGetupCycle(getupRaw, idleFrames);
        if (idleFrames.Length == 0 && runFrames.Length == 0 && getupFrames.Length == 0 && jumpFrames.Length == 0)
            return;
        if (runFrames.Length == 0)
            runFrames = idleFrames;
        bool keepArmed = controller.HasOathblade || previewOathbladeIdle;
        controller.SetAnimationFrames(idleFrames, runFrames, getupFrames, jumpFrames, idleArmedFrames, keepArmed);
        var anim = controller.GetComponent<PlayerSpriteAnimator>();
        anim?.ConfigureIdleTiming(3f);
        anim?.ConfigureGetupTiming(6f);
        AlignPlayerPresentation(controller);
    }

    static void AlignPlayerPresentation(PlayerController controller)
    {
        if (controller == null)
            return;

        // Defaults match Zone A; Bake Layout writes ZoneA_Layout.json overrides.
        var standingSize = new Vector2(0.5535295f, 1.705085f);
        var standingOffset = new Vector2(0.1400892f, 0.629543f);
        const float getupH = 0.42f;
        float standBottom = standingOffset.y - standingSize.y * 0.5f;
        var getupSize = new Vector2(0.7f, getupH);
        var getupOffset = new Vector2(standingOffset.x, standBottom + getupH * 0.5f);

        var layout = TryLoadZoneALayout();
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
        // Feet Y comes from PlayerController.visualFeetOffset (Inspector), not a hardcoded value.
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

    static ZoneALayoutSnapshot TryLoadZoneALayout()
    {
#if UNITY_EDITOR
        string path = CathedralIntroZoneA.LayoutAssetPath;
        if (System.IO.File.Exists(path))
        {
            try
            {
                return JsonUtility.FromJson<ZoneALayoutSnapshot>(System.IO.File.ReadAllText(path));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Step A: failed to read Zone A layout for player colliders: {e.Message}");
            }
        }
#endif
        return null;
    }

    static void DestroySafe(Object obj)
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
        cam.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        if (cam.transform.position.z > -5f)
            cam.transform.position = new Vector3(0f, 2f, -10f);

        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null)
            follow.SetOrthoSize(4.5f);
    }

    PlayerController CreatePlayer(
        Sprite[] idleFrames,
        Sprite[] runFrames,
        Sprite[] getupFrames,
        Sprite[] jumpFrames,
        Sprite[] idleArmedFrames = null,
        bool equipOathblade = false)
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
        capsule.size = new Vector2(0.5535295f, 1.705085f);
        capsule.offset = new Vector2(0.1400892f, 0.629543f);

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
        float standBottom = 0.629543f - 1.705085f * 0.5f;
        groundCheck.transform.localPosition = new Vector3(0.1400892f, standBottom, 0f);

        root.AddComponent<PlayerSpriteAnimator>();
        var controller = root.AddComponent<PlayerController>();
        controller.Bind(sr, groundCheck.transform);
        controller.SetAnimationFrames(idleFrames, runFrames, getupFrames, jumpFrames, idleArmedFrames, equipOathblade);
        var anim = root.GetComponent<PlayerSpriteAnimator>();
        anim?.ConfigureIdleTiming(3f);
        anim?.ConfigureGetupTiming(6f);
        AlignPlayerPresentation(controller);
        return controller;
    }

    void PlaceRow(Transform parent, Sprite sprite, float startX, float y, int count, float scale)
    {
        float width = sprite.bounds.size.x * scale;
        for (int i = 0; i < count; i++)
            PlacePlatform(parent, sprite, startX + i * width, y, scale);
    }

    void PlacePlatform(Transform parent, Sprite sprite, float x, float y, float scale)
    {
        var go = new GameObject(sprite != null ? sprite.name : "Platform");
        int groundLayer = GameLayers.GroundLayer;
        if (groundLayer >= 0)
            go.layer = groundLayer;
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = Vector3.one * scale;

        var sr = go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, sprite, "Platforms", 0);

        var col = go.AddComponent<BoxCollider2D>();
        float w = sprite.bounds.size.x;
        float h = sprite.bounds.size.y;
        float thickness = Mathf.Clamp(h * 0.2f, 0.16f, 0.32f);
        col.size = new Vector2(w * 0.9f, thickness);
        col.offset = new Vector2(0f, h - thickness * 0.5f);
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
                _spriteMaterial = new Material(shader) { name = "StepASpriteUnlit" };
        }

        if (_spriteMaterial != null)
            renderer.sharedMaterial = _spriteMaterial;
    }

    static Sprite LoadSprite(string assetPath, string spriteName)
    {
#if UNITY_EDITOR
        var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite && sprite.name == spriteName)
                return sprite;
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
