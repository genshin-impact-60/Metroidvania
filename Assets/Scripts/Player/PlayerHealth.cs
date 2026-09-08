using System.Collections;
using UnityEngine;

/// <summary>
/// Simple HP with i-frames. On death, respawn at PlayerController.SpawnPosition.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] int maxHp = 3;
    [SerializeField] float invulnDuration = 0.85f;
    [SerializeField] float flashInterval = 0.08f;

    PlayerController _controller;
    SpriteRenderer _visual;
    int _hp;
    bool _busy;
    float _invulnUntil;
    Coroutine _flashRoutine;

    public int CurrentHp => _hp;
    public int MaxHp => maxHp;
    public bool IsDead => _busy;
    public bool IsInvulnerable => _busy || Time.time < _invulnUntil;

    void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _visual = GetComponentInChildren<SpriteRenderer>();
        _hp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector2.zero);
    }

    /// <returns>True if damage was applied.</returns>
    public bool TakeDamage(int amount, Vector2 knockback)
    {
        if (_busy || amount <= 0 || IsInvulnerable)
            return false;
        if (_controller != null && _controller.IsControlLocked)
            return false;

        _hp -= amount;
        _invulnUntil = Time.time + invulnDuration;

        if (knockback.sqrMagnitude > 0.01f)
            _controller?.ApplyHitKnockback(knockback);

        StartFlash();

        if (_hp <= 0)
            Die();

        return true;
    }

    public void Kill()
    {
        if (_busy)
            return;
        _hp = 0;
        Die();
    }

    public void Die()
    {
        if (_busy)
            return;

        _busy = true;
        StopFlash(restoreAlpha: true);
        _invulnUntil = 0f;
        _hp = maxHp;
        _controller.Respawn();
        _busy = false;
    }

    public void FullHeal()
    {
        _hp = maxHp;
    }

    void StartFlash()
    {
        if (_visual == null)
            _visual = GetComponentInChildren<SpriteRenderer>();
        if (_visual == null)
            return;

        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashWhileInvulnerable());
    }

    void StopFlash(bool restoreAlpha)
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            _flashRoutine = null;
        }

        if (restoreAlpha && _visual != null)
        {
            var c = _visual.color;
            c.a = 1f;
            _visual.color = c;
        }
    }

    IEnumerator FlashWhileInvulnerable()
    {
        while (Time.time < _invulnUntil && !_busy)
        {
            if (_visual != null)
            {
                var c = _visual.color;
                c.a = c.a > 0.6f ? 0.35f : 1f;
                _visual.color = c;
            }

            yield return new WaitForSeconds(flashInterval);
        }

        if (_visual != null)
        {
            var c = _visual.color;
            c.a = 1f;
            _visual.color = c;
        }

        _flashRoutine = null;
    }
}
