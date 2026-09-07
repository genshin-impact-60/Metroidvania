using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class StepAPlayground : MonoBehaviour
{
    const string IdlePath = "Assets/Art/Characters/protagonist_cursed_pilgrim_chibi.png";
    const string RunPath = "Assets/Art/Characters/protagonist_cursed_pilgrim_chibi_run.png";
    const string IdleSheetPath = "Assets/Art/Characters/Animations/protagonist_cursed_pilgrim_chibi_idle_sheet.png";
    const string RunSheetPath = "Assets/Art/Characters/Animations/protagonist_cursed_pilgrim_chibi_run_sheet.png";
    const string TilesetPath = "Assets/Art/Environment/map_tileset_cathedral.png";

    [SerializeField] bool buildOnEnable = true;

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
        var floor = LoadSprite(TilesetPath, "map_tileset_cathedral_1");
        var ledge = LoadSprite(TilesetPath, "map_tileset_cathedral_9");
        var cracked = LoadSprite(TilesetPath, "map_tileset_cathedral_2");

        var idleFrames = BuildIdleCycle(LoadSheet(IdleSheetPath));
        var runFrames = LoadSheet(RunSheetPath);
        if (idle == null)
            idle = MakeSolidSprite(new Color(0.9f, 0.9f, 0.92f), 128, 256);
        if (run == null)
            run = idle;
        if (idleFrames.Length == 0)
            idleFrames = new[] { idle };
        if (runFrames.Length == 0)
            runFrames = new[] { run };
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
        var player = CreatePlayer(idleFrames, runFrames);
        player.transform.SetParent(transform, true);
        player.transform.position = new Vector3(-6.5f, floorTop + 0.02f, 0f);
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
        Debug.Log("Step A playground ready. Press Play, then move with A/D and jump with Space.");
    }

    void ClearSpawned()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroySafe(transform.GetChild(i).gameObject);
    }

    void RefreshPlayerAnimation(PlayerController controller)
    {
        var idleFrames = BuildIdleCycle(LoadSheet(IdleSheetPath));
        var runFrames = LoadSheet(RunSheetPath);
        if (idleFrames.Length == 0 && runFrames.Length == 0)
            return;
        if (runFrames.Length == 0)
            runFrames = idleFrames;
        controller.SetAnimationFrames(idleFrames, runFrames);
        var anim = controller.GetComponent<PlayerSpriteAnimator>();
        anim?.ConfigureIdleTiming(3f);
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
        cam.orthographicSize = 7f;
        cam.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        if (cam.transform.position.z > -5f)
            cam.transform.position = new Vector3(0f, 2f, -10f);
    }

    PlayerController CreatePlayer(Sprite[] idleFrames, Sprite[] runFrames)
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
        capsule.size = new Vector2(0.46f, 1.36f);
        capsule.offset = new Vector2(0f, 0.7f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.53f;

        var sr = visual.AddComponent<SpriteRenderer>();
        var first = idleFrames != null && idleFrames.Length > 0 ? idleFrames[0] : null;
        ApplySprite(sr, first, "Entities", 10);

        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(root.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, 0.06f, 0f);

        root.AddComponent<PlayerSpriteAnimator>();
        var controller = root.AddComponent<PlayerController>();
        controller.Bind(sr, groundCheck.transform);
        controller.SetAnimationFrames(idleFrames, runFrames);
        var anim = root.GetComponent<PlayerSpriteAnimator>();
        anim?.ConfigureIdleTiming(3f);
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

    /// <summary>
    /// Idle sheet: rest → inhale → peak (cloak + head) → exhale. Holds keep the breath slow.
    /// </summary>
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
