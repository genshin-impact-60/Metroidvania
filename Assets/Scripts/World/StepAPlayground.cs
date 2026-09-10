using UnityEngine;

/// <summary>
/// Editor sandbox in SampleScene. Play only rebinds camera / spawn.
/// Rebuild lives in Editor/PlaygroundSeeder — not a second campaign entry.
/// </summary>
[DefaultExecutionOrder(-50)]
public class StepAPlayground : MonoBehaviour
{
    [SerializeField] [Tooltip("Preview armed idle after getup (残誓). Zone A intro stays unarmed.")]
    bool previewOathbladeIdle = true;
    [SerializeField] PlayerController playerPrefab;
    [SerializeField] GameObject envSolidPrefab;

    public bool PreviewOathbladeIdle => previewOathbladeIdle;
    public PlayerController PlayerPrefab => playerPrefab;
    public GameObject EnvSolidPrefab => envSolidPrefab;
    public bool HasSeededContent => transform.Find("Platforms") != null;

    void Start()
    {
        var existing = GetComponentInChildren<PlayerController>();
        if (existing == null)
            return;
        existing.SpawnPosition = existing.transform.position;
        var cam = Camera.main;
        if (cam == null)
            return;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.SetTarget(existing.transform);
    }
}
