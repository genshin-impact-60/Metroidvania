using UnityEngine;

/// <summary>
/// Trigger damage + knockback. Relies on PlayerHealth i-frames so Stay is safe.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DamageZone : MonoBehaviour
{
    [SerializeField] int damage = 1;
    [SerializeField] float knockbackX = 5f;
    [SerializeField] float knockbackY = 4f;
    [SerializeField] bool ignoreWhileControlLocked = true;
    [SerializeField] string firstHitMessage;

    bool _loggedMessage;

    public void Configure(int damageAmount, float knockX, float knockY, string firstHitFlavor = null)
    {
        damage = damageAmount;
        knockbackX = knockX;
        knockbackY = knockY;
        firstHitMessage = firstHitFlavor;
    }

    void OnTriggerEnter2D(Collider2D other) => TryHit(other);

    void OnTriggerStay2D(Collider2D other) => TryHit(other);

    void TryHit(Collider2D other)
    {
        var health = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
        if (health == null)
            return;

        var controller = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (ignoreWhileControlLocked && controller != null && controller.IsControlLocked)
            return;

        float sign = Mathf.Sign(other.transform.position.x - transform.position.x);
        if (Mathf.Abs(sign) < 0.01f)
            sign = 1f;

        if (!health.TakeDamage(damage, new Vector2(sign * knockbackX, knockbackY)))
            return;

        if (!_loggedMessage && !string.IsNullOrEmpty(firstHitMessage))
        {
            _loggedMessage = true;
            Debug.Log(firstHitMessage);
        }
    }
}
