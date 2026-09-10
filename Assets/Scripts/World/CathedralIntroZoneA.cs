using UnityEngine;

/// <summary>
/// Cathedral Intro Zone A marker. Scene content is the source of truth.
/// Play only rebinds camera / spawn. Seeding lives in Editor/ZoneASeeder.
/// </summary>
[DefaultExecutionOrder(-50)]
public class CathedralIntroZoneA : MonoBehaviour
{
    [SerializeField] TextAsset bakedLayout;
    [SerializeField] PlayerController playerPrefab;
    [SerializeField] GameObject envSolidPrefab;
    [SerializeField] GameObject envDecorPrefab;
    [SerializeField] GameObject envBackgroundPrefab;
    [SerializeField] GameObject envBlockerPrefab;
    [SerializeField] GameObject holyWaterPrefab;
    [SerializeField] GameObject saveShrinePrefab;

    public const string LayoutAssetPath = "Assets/Scripts/World/ZoneA_Layout.json";

    public static string LayoutAbsolutePath =>
        System.IO.Path.Combine(Application.dataPath, "Scripts/World/ZoneA_Layout.json");

    public TextAsset BakedLayout => bakedLayout;
    public PlayerController PlayerPrefab => playerPrefab;
    public GameObject EnvSolidPrefab => envSolidPrefab;
    public GameObject EnvDecorPrefab => envDecorPrefab;
    public GameObject EnvBackgroundPrefab => envBackgroundPrefab;
    public GameObject EnvBlockerPrefab => envBlockerPrefab;
    public GameObject HolyWaterPrefab => holyWaterPrefab;
    public GameObject SaveShrinePrefab => saveShrinePrefab;
    public bool HasSeededContent => transform.Find("Platforms") != null;

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
