using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal save point: Interact sets player respawn here and swaps to the lit shrine art.
/// Full save serialization comes later.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SaveShrine : MonoBehaviour
{
    [SerializeField] SpriteRenderer visual;
    [SerializeField] Sprite inactiveSprite;
    [SerializeField] Sprite activeSprite;
    [SerializeField] string shrineId = "save_cathedral_sv1";
    [SerializeField] Color inactiveTint = Color.white;
    [SerializeField] Color activeTint = Color.white;
    [SerializeField] string promptText = "按 E · 祈祷存档";
    [SerializeField] string activateFlavor = "祭火已燃——此处铭刻归途。";
    [SerializeField] string reActivateFlavor = "归途仍在此处。";

    InputAction _interact;
    PlayerController _playerInRange;
    bool _activated;
    Coroutine _flashRoutine;

    public string ShrineId => shrineId;
    public bool IsActivated => _activated;

    public void Bind(SpriteRenderer spriteRenderer, string id, Sprite off, Sprite on)
    {
        visual = spriteRenderer;
        shrineId = id;
        inactiveSprite = off;
        activeSprite = on;
        ApplyVisual();
    }

    void OnEnable()
    {
        BindInput();
        _interact?.Enable();
        ApplyVisual();
    }

    void OnDisable()
    {
        _interact?.Disable();
        if (_playerInRange != null)
            ClearPrompt();
    }

    void Update()
    {
        if (_playerInRange == null)
            return;

        if (_playerInRange.IsControlLocked)
            return;

        GameFlavorUI.SetPrompt(promptText);

        if (_interact == null || !_interact.WasPressedThisFrame())
            return;

        Activate(_playerInRange);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (player == null)
            return;

        _playerInRange = player;
        if (!player.IsControlLocked)
            GameFlavorUI.SetPrompt(promptText);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (player != null && player == _playerInRange)
        {
            _playerInRange = null;
            ClearPrompt();
        }
    }

    public void Activate(PlayerController player)
    {
        if (player == null)
            return;

        bool firstTime = !_activated;
        _activated = true;
        player.SpawnPosition = transform.position;
        ApplyVisual();
        PlayActivateFlash();

        if (firstTime)
            GameFlavorUI.ShowFlavor(activateFlavor);
        else
            GameFlavorUI.ShowFlavor(reActivateFlavor, 1.6f);

        Debug.Log($"[{shrineId}] activated — respawn set.");
    }

    void ClearPrompt()
    {
        GameFlavorUI.SetPrompt("");
    }

    void PlayActivateFlash()
    {
        if (visual == null)
            visual = GetComponent<SpriteRenderer>();
        if (visual == null)
            return;

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        var baseColor = _activated ? activeTint : inactiveTint;
        var peak = new Color(1f, 0.92f, 0.55f, 1f);
        const float up = 0.12f;
        const float down = 0.35f;

        float t = 0f;
        while (t < up)
        {
            t += Time.deltaTime;
            visual.color = Color.Lerp(baseColor, peak, t / up);
            yield return null;
        }

        t = 0f;
        while (t < down)
        {
            t += Time.deltaTime;
            visual.color = Color.Lerp(peak, baseColor, t / down);
            yield return null;
        }

        visual.color = baseColor;
        _flashRoutine = null;
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

    void ApplyVisual()
    {
        if (visual == null)
            visual = GetComponent<SpriteRenderer>();
        if (visual == null)
            return;

        var sprite = _activated ? activeSprite : inactiveSprite;
        if (sprite == null)
            sprite = inactiveSprite != null ? inactiveSprite : activeSprite;
        if (sprite != null)
            visual.sprite = sprite;

        visual.color = _activated ? activeTint : inactiveTint;
    }
}
