using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 1.0f, -10f);
    [SerializeField] float followLerp = 12f;
    [SerializeField] float orthoSize = 4.5f;

    Camera _camera;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
    }

    public void SetOrthoSize(float size)
    {
        orthoSize = Mathf.Max(0.1f, size);
        ApplyOrthoSize();
    }

    void Awake()
    {
        _camera = GetComponent<Camera>();
        ApplyOrthoSize();
    }

    void ApplyOrthoSize()
    {
        if (_camera == null)
            _camera = GetComponent<Camera>();
        if (_camera != null && _camera.orthographic)
            _camera.orthographicSize = orthoSize;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null)
                return;
            target = player.transform;
        }

        var goal = target.position + offset;
        goal.z = offset.z;
        transform.position = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-followLerp * Time.deltaTime));
    }
}
