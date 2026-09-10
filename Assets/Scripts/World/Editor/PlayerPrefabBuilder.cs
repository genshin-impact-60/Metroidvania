using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds Assets/Prefabs/Player/Player.prefab with serialized animation frames
/// so Play / Player builds no longer depend on AssetDatabase.
/// </summary>
[InitializeOnLoad]
public static class PlayerPrefabBuilder
{
    public const string PrefabPath = "Assets/Prefabs/Player/Player.prefab";

    const float VisualScale = 0.62f;
    static readonly Vector3 VisualLocalPos = new Vector3(0.07f, -0.2f, 0f);
    static readonly Vector2 StandingSize = new Vector2(0.5535295f, 1.6826296f);
    static readonly Vector2 StandingOffset = new Vector2(0.14008921f, 0.6407707f);
    static readonly Vector2 GetupSize = new Vector2(0.7f, 0.42f);
    static readonly Vector2 GetupOffset = new Vector2(0.1400892f, -0.013f);

    static int _retries;

    static PlayerPrefabBuilder()
    {
        EditorApplication.delayCall += EnsurePrefabAndWireScenes;
    }

    [MenuItem("Tools/Cathedral/Create or Update Player Prefab")]
    public static void CreateOrUpdateMenu()
    {
        _retries = 0;
        var prefab = CreatePrefabAsset();
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "Player Prefab",
                "Could not load animation frames. Wait for Unity to finish importing, then try again.",
                "OK");
            return;
        }

        AssignPrefabToOpenContent(prefab);
        ReplaceLoosePlayers(prefab);
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Player Prefab", $"Updated {PrefabPath} and wired open scenes.", "OK");
    }

    public static void EnsurePrefabAndWireScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += EnsurePrefabAndWireScenes;
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            prefab = CreatePrefabAsset();
            if (prefab == null)
                return;
        }

        AssignPrefabToOpenContent(prefab);
        ReplaceLoosePlayers(prefab);
    }

    public static GameObject LoadPrefabAsset()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    static GameObject CreatePrefabAsset()
    {
        var idle = PlayerAnimationLoader.BuildIdleCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.IdleDir));
        var idleArmed = PlayerAnimationLoader.BuildIdleCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.IdleOathbladeDir));
        var run = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.RunDir);
        var jump = PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.JumpDir);
        var getup = PlayerAnimationLoader.BuildGetupCycle(
            PlayerAnimationLoader.LoadFrames(PlayerAnimationLoader.GetupDir), idle);

        if (idle == null || idle.Length == 0 || run == null || run.Length == 0)
        {
            if (_retries++ < 8)
                EditorApplication.delayCall += EnsurePrefabAndWireScenes;
            else
                Debug.LogWarning("Player prefab: animation frames missing after retries.");
            return null;
        }

        string folder = Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
        {
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        var temp = BuildPlayerGameObject(idle, idleArmed, run, jump, getup);
        var prefab = PrefabUtility.SaveAsPrefabAsset(temp, PrefabPath);
        Object.DestroyImmediate(temp);
        if (prefab == null)
        {
            Debug.LogError($"Failed to save {PrefabPath}");
            return null;
        }

        Debug.Log($"Saved Player prefab → {PrefabPath}");
        return prefab;
    }

    static GameObject BuildPlayerGameObject(
        Sprite[] idle,
        Sprite[] idleArmed,
        Sprite[] run,
        Sprite[] jump,
        Sprite[] getup)
    {
        var root = new GameObject("Player");
        root.tag = "Player";
        int playerLayer = GameLayers.PlayerLayer;
        if (playerLayer >= 0)
            root.layer = playerLayer;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.gravityScale = 3.5f;

        var capsule = root.AddComponent<CapsuleCollider2D>();
        capsule.direction = CapsuleDirection2D.Vertical;
        capsule.size = StandingSize;
        capsule.offset = StandingOffset;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = VisualLocalPos;
        visual.transform.localScale = Vector3.one * VisualScale;
        if (playerLayer >= 0)
            visual.layer = playerLayer;

        var sr = visual.AddComponent<SpriteRenderer>();
        Sprite first = getup != null && getup.Length > 0 && getup[0] != null
            ? getup[0]
            : (idle != null && idle.Length > 0 ? idle[0] : null);
        ApplySprite(sr, first);

        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(root.transform, false);
        float standBottom = StandingOffset.y - StandingSize.y * 0.5f;
        groundCheck.transform.localPosition = new Vector3(StandingOffset.x, standBottom, 0f);

        var animator = root.AddComponent<PlayerSpriteAnimator>();
        root.AddComponent<PlayerHealth>();
        var controller = root.AddComponent<PlayerController>();

        var soAnim = new SerializedObject(animator);
        soAnim.FindProperty("target").objectReferenceValue = sr;
        AssignSpriteArray(soAnim.FindProperty("idleFrames"), idle);
        AssignSpriteArray(soAnim.FindProperty("idleArmedFrames"), idleArmed);
        AssignSpriteArray(soAnim.FindProperty("runFrames"), run);
        AssignSpriteArray(soAnim.FindProperty("jumpFrames"), jump);
        AssignSpriteArray(soAnim.FindProperty("getupFrames"), getup);
        soAnim.FindProperty("idleFps").floatValue = 3f;
        soAnim.FindProperty("getupFps").floatValue = 6f;
        soAnim.ApplyModifiedPropertiesWithoutUndo();

        var soCtrl = new SerializedObject(controller);
        soCtrl.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;
        soCtrl.FindProperty("visual").objectReferenceValue = sr;
        soCtrl.FindProperty("animator").objectReferenceValue = animator;
        soCtrl.FindProperty("playGetupOnStart").boolValue = true;
        soCtrl.FindProperty("visualFeetOffset").floatValue = VisualLocalPos.y;
        soCtrl.FindProperty("getupColliderSize").vector2Value = GetupSize;
        soCtrl.FindProperty("getupColliderOffset").vector2Value = GetupOffset;
        soCtrl.ApplyModifiedPropertiesWithoutUndo();

        controller.Bind(sr, groundCheck.transform);
        controller.ConfigureColliders(StandingSize, StandingOffset, GetupSize, GetupOffset);
        return root;
    }

    static void AssignSpriteArray(SerializedProperty prop, Sprite[] frames)
    {
        if (prop == null)
            return;
        if (frames == null)
        {
            prop.arraySize = 0;
            return;
        }

        prop.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
    }

    static void ApplySprite(SpriteRenderer renderer, Sprite sprite)
    {
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Entities";
        renderer.sortingOrder = 10;
        renderer.color = Color.white;
    }

    static void AssignPrefabToOpenContent(GameObject prefab)
    {
        var controller = prefab.GetComponent<PlayerController>();
        if (controller == null)
            return;

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded)
                continue;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var playground in root.GetComponentsInChildren<StepAPlayground>(true))
                    AssignIfChanged(playground, "playerPrefab", controller, scene);
            }
        }
    }

    static void AssignIfChanged(Object target, string field, PlayerController prefab, Scene scene)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null || prop.objectReferenceValue == prefab)
            return;

        prop.objectReferenceValue = prefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);
    }

    static void ReplaceLoosePlayers(GameObject prefab)
    {
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded)
                continue;

            var found = Object.FindObjectsByType<PlayerController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var seen = new HashSet<GameObject>();
            foreach (var pc in found)
            {
                if (pc == null || pc.gameObject == null || !seen.Add(pc.gameObject))
                    continue;
                if (!pc.gameObject.scene.IsValid() || pc.gameObject.scene != scene)
                    continue;
                if (EditorUtility.IsPersistent(pc))
                    continue;

                string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(pc.gameObject);
                if (assetPath == PrefabPath)
                    continue;

                var old = pc.gameObject;
                var parent = old.transform.parent;
                var pos = old.transform.position;
                var rot = old.transform.rotation;
                int sibling = old.transform.GetSiblingIndex();

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
                instance.name = "Player";
                instance.transform.SetPositionAndRotation(pos, rot);
                instance.transform.SetSiblingIndex(sibling);
                Object.DestroyImmediate(old);

                var cam = Camera.main;
                if (cam != null)
                {
                    var follow = cam.GetComponent<CameraFollow>();
                    if (follow != null)
                        follow.SetTarget(instance.transform);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"Replaced loose Player in '{scene.name}' with prefab instance. Save the scene (Ctrl+S).", instance);
            }
        }
    }
}
