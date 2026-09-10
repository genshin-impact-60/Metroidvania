using UnityEngine;

/// <summary>
/// Cathedral Intro Zone A marker. Scene content is the source of truth.
/// Play only rebinds camera / spawn.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CathedralIntroZoneA : MonoBehaviour
{
    void Start()
    {
        BindPlaySession();
    }

    public void BindPlaySession()
    {
        var player = GetComponentInChildren<PlayerController>();
        if (player == null)
            return;

        if (Application.isPlaying)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.bodyType = RigidbodyType2D.Dynamic;
        }

        player.SpawnPosition = player.transform.position;

        var cam = Camera.main;
        if (cam == null)
            return;

        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(player.transform);
    }
}
