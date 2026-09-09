using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads protagonist animation frames from per-action folders
/// under Assets/Art/Characters/Animations/{idle,run,...}.
/// </summary>
public static class PlayerAnimationLoader
{
    public const string Root = "Assets/Art/Characters/Animations/";
    public const string IdleDir = Root + "idle";
    public const string IdleOathbladeDir = Root + "idle_oathblade";
    public const string RunDir = Root + "run";
    public const string JumpDir = Root + "jump";
    public const string GetupDir = Root + "getup";

    public static Sprite[] LoadFrames(string folderPath)
    {
        var frames = new List<Sprite>();
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            return frames.ToArray();

        string normFolder = folderPath.Replace('\\', '/').TrimEnd('/');
        foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath }))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (parent != normFolder)
                continue;

            foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                    frames.Add(sprite);
            }
        }

        frames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
#endif
        return frames.ToArray();
    }

    /// <summary>
    /// Idle: rest → inhale → peak (cloak + head) → exhale. Holds keep the breath slow.
    /// </summary>
    public static Sprite[] BuildIdleCycle(Sprite[] frames)
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

    /// <summary>
    /// Getup timing: readable transitions; soft-land on idle so Unlock→Idle does not pop.
    /// Preferred (7): 0 lie → 1 stir → 2 crawl → 3 kneel → 4 half-rise → 5 mid-rise → 6 stand.
    /// Legacy (6): same without mid-rise.
    /// </summary>
    public static Sprite[] BuildGetupCycle(Sprite[] frames, Sprite[] idleFrames)
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
}
