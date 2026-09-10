using System;
using UnityEngine;

/// <summary>
/// Serializable Zone A layout — optional backup / regenerate seed (not the daily source of truth).
/// Daily edits live in the Unity Scene; export via Inspector → 导出备份.
/// </summary>
[Serializable]
public class ZoneALayoutSnapshot
{
    public ZoneASpriteEntry[] entries = Array.Empty<ZoneASpriteEntry>();
    public ZoneABlockerEntry[] blockers = Array.Empty<ZoneABlockerEntry>();
    public bool hasHolyWater;
    public float holyWaterX = -0.55f;

    /// <summary>When true, holy water BurnHitbox uses the baked local pose / size.</summary>
    public bool hasHolyWaterHitbox;
    public float holyHitLx;
    public float holyHitLy;
    public float holyHitW = 1f;
    public float holyHitH = 0.28f;
    public float holyHitOx;
    public float holyHitOy;

    public float playerX = -3.4f;
    public float playerY = 1.28f;
    public float camX = -3.4f;
    public float camY = 2.6f;
    public float camZ = -10f;

    /// <summary>When true, AlignPlayerPresentation uses baked standing / getup capsules.</summary>
    public bool hasPlayerCollider;
    public float standColW = 0.5535295f;
    public float standColH = 1.705085f;
    public float standColOx = 0.1400892f;
    public float standColOy = 0.629543f;
    public float getupColW = 0.7f;
    public float getupColH = 0.42f;
    public float getupColOx = 0.1400892f;
    public float getupColOy = -0.013f;

    public bool TryGetPlayerColliders(
        out Vector2 standingSize,
        out Vector2 standingOffset,
        out Vector2 getupSize,
        out Vector2 getupOffset)
    {
        standingSize = new Vector2(standColW, standColH);
        standingOffset = new Vector2(standColOx, standColOy);
        getupSize = new Vector2(getupColW, getupColH);
        getupOffset = new Vector2(getupColOx, getupColOy);
        return hasPlayerCollider;
    }
}

[Serializable]
public class ZoneASpriteEntry
{
    /// <summary>Platforms | Decor | Atmosphere</summary>
    public string bucket = "Platforms";

    /// <summary>Sprite asset name, e.g. map_tileset_cathedral_4</summary>
    public string spriteName = "";

    /// <summary>floor | wall | fill | ceiling | decor | bg | fissure | other</summary>
    public string role = "other";

    public float x, y, z;
    public float sx = 1f, sy = 1f, sz = 1f;
    public string sortingLayer = "Platforms";
    public int sortingOrder;
    public float cr = 1f, cg = 1f, cb = 1f, ca = 1f;
    public bool withCollider;

    /// <summary>When true, PlacePlatform uses colW/H + colOx/Oy instead of sprite-bounds formula.</summary>
    public bool hasCustomCollider;
    public float colW;
    public float colH;
    public float colOx;
    public float colOy;
}

[Serializable]
public class ZoneABlockerEntry
{
    public float x, y;
    public float w = 1f, h = 1f;
    public float ox;
    public float oy;
}
