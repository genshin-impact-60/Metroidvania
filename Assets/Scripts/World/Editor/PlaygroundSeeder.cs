using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only SampleScene sandbox seeder. Not used by Cathedral_Hub.
/// </summary>
public static class PlaygroundSeeder
{
    const string TileDir = "Assets/Art/Environment/Tilesets/cathedral/";
    const string FloorTilePath = TileDir + "map_tileset_cathedral_1.png";
    const string CrackedTilePath = TileDir + "map_tileset_cathedral_2.png";
    const string LedgeTilePath = TileDir + "map_tileset_cathedral_9.png";
    const string UnlitMatPath =
        "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

    public static void Rebuild(StepAPlayground playground)
    {
        if (playground == null)
            return;

        ClearSpawned(playground.transform);
        Build(playground);
    }

    static void Build(StepAPlayground playground)
    {
        ApplyCameraDefaults();

        var floor = LoadSprite(FloorTilePath, "map_tileset_cathedral_1");
        var ledge = LoadSprite(LedgeTilePath, "map_tileset_cathedral_9");
        var cracked = LoadSprite(CrackedTilePath, "map_tileset_cathedral_2");
        if (floor == null)
            floor = MakeSolidSprite(new Color(0.28f, 0.29f, 0.32f), 256, 64);
        if (ledge == null)
            ledge = floor;
        if (cracked == null)
            cracked = floor;

        var platforms = new GameObject("Platforms").transform;
        platforms.SetParent(playground.transform, false);

        const float scale = 2f;
        PlaceRow(playground, platforms, floor, startX: -10f, y: 0f, count: 5, scale: scale);
        PlaceRow(playground, platforms, cracked, startX: 1.7f, y: 0f, count: 4, scale: scale);
        PlacePlatform(playground, platforms, ledge, 2.6f, 1.55f, scale);
        PlacePlatform(playground, platforms, ledge, 6.4f, 2.65f, scale);

        float floorTop = floor.bounds.size.y * scale;
        var player = SpawnPlayer(playground, new Vector3(-6.5f, floorTop + 0.02f, 0f));
        if (player != null && playground.PreviewOathbladeIdle)
            player.EquipOathblade();

        var cam = Camera.main;
        if (cam != null && player != null)
        {
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null)
                follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.SetTarget(player.transform);
            cam.transform.position = new Vector3(-6.5f, 2.4f, -10f);
        }

        if (!Application.isPlaying)
        {
            var view = SceneView.lastActiveSceneView;
            view?.Frame(new Bounds(new Vector3(0f, 1.8f, 0f), new Vector3(26f, 12f, 1f)), false);
        }

        Debug.Log(playground.PreviewOathbladeIdle
            ? "Playground seeded (editor sandbox). Armed idle preview on."
            : "Playground seeded (editor sandbox).");
    }

    static void ClearSpawned(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
    }

    static PlayerController SpawnPlayer(StepAPlayground playground, Vector3 position)
    {
        if (playground.PlayerPrefab == null)
        {
            Debug.LogError(
                "StepAPlayground: Player Prefab is not assigned. " +
                "Use Tools/Cathedral/Create or Update Player Prefab.");
            return null;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(
            playground.PlayerPrefab.gameObject, playground.transform);
        go.name = "Player";
        go.transform.position = position;
        var editorPlayer = go.GetComponent<PlayerController>();
        editorPlayer.SnapToGround();
        editorPlayer.SpawnPosition = editorPlayer.transform.position;
        return editorPlayer;
    }

    static void ApplyCameraDefaults()
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

    static void PlaceRow(
        StepAPlayground playground, Transform parent, Sprite sprite, float startX, float y, int count, float scale)
    {
        float width = sprite.bounds.size.x * scale;
        for (int i = 0; i < count; i++)
            PlacePlatform(playground, parent, sprite, startX + i * width, y, scale);
    }

    static void PlacePlatform(
        StepAPlayground playground, Transform parent, Sprite sprite, float x, float y, float scale)
    {
        GameObject go;
        if (playground.EnvSolidPrefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(playground.EnvSolidPrefab, parent);
            go.name = sprite != null ? sprite.name : "Platform";
        }
        else
        {
            go = new GameObject(sprite != null ? sprite.name : "Platform");
            go.transform.SetParent(parent, false);
        }

        int groundLayer = GameLayers.GroundLayer;
        if (groundLayer >= 0)
            go.layer = groundLayer;
        go.transform.position = new Vector3(x, y, 0f);
        go.transform.localScale = Vector3.one * scale;

        var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        ApplySprite(sr, sprite, "Platforms", 0);

        var col = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
        float w = sprite.bounds.size.x;
        float h = sprite.bounds.size.y;
        float thickness = Mathf.Clamp(h * 0.2f, 0.16f, 0.32f);
        col.size = new Vector2(w * 0.9f, thickness);
        col.offset = new Vector2(0f, h - thickness * 0.5f);
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
        foreach (var asset in assets)
        {
            if (asset is Sprite s && s.name == spriteName)
                return s;
        }

        foreach (var asset in assets)
        {
            if (asset is Sprite s)
                return s;
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
