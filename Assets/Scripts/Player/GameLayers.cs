using UnityEngine;

public static class GameLayers
{
    public const string Player = "Player";
    public const string Ground = "Ground";
    public const string Hazard = "Hazard";
    public const string Enemy = "Enemy";
    public const string Interact = "Interact";
    public const string Pickup = "Pickup";

    public static int PlayerLayer => LayerMask.NameToLayer(Player);
    public static int GroundLayer => LayerMask.NameToLayer(Ground);
    public static int GroundMask => LayerMask.GetMask(Ground);
}
