using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal save point: Interact sets player respawn here and brightens the shrine.
/// Full save serialization comes later.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SaveShrine : MonoBehaviour
{
    [SerializeField] SpriteRenderer visual;
    [SerializeField] string shrineId = "save_cathedral_sv1";
    [SerializeField] Color inactiveTint = new Color(0.72f, 0.74f, 0.78f, 1f);
    [SerializeField] Color activeTint = new Color(1f, 0.92f, 0.7f, 1f);

    InputAction _interact;
    PlayerController _playerInRange;
    bool _activated;

    public string ShrineId => shrineId;
    public bool IsActivated => _activated;

    public void Bind(SpriteRenderer spriteRenderer, string id)
    {
        visual = spriteRenderer;
        shrineId = id;
        ApplyTint();
    }

    void OnEnable()
    {
        BindInput();
        _interact?.Enable();
        ApplyTint();
    }

    void OnDisable()
    {
        _interact?.Disable();
    }

    void Update()
    {
        if (_playerInRange == null || _playerInRange.IsControlLocked)
            return;
        if (_interact == null || !_interact.WasPressedThisFrame())
            return;

        Activate(_playerInRange);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (player != null)
            _playerInRange = player;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (player != null && player == _playerInRange)
            _playerInRange = null;
    }

    public void Activate(PlayerController player)
    {
        if (player == null)
            return;

        _activated = true;
        player.SpawnPosition = transform.position;
        ApplyTint();
        Debug.Log($"[{shrineId}] activated — respawn set. (E to re-confirm)");
    }

    void BindInput()
    {
        if (_interact != null)
            return;

        var actions = InputSystem.actions;
        if (actions == null)
            return;

        _interact = actions.FindAction("Player/Interact") ?? actions.FindAction("Interact");
    }

    void ApplyTint()
    {
        if (visual != null)
            visual.color = _activated ? activeTint : inactiveTint;
    }
}
