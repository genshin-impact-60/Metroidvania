using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] Vector3 offset = new Vector3(0f, 1.0f, -10f);
    [SerializeField] float followLerp = 12f;

    public void SetTarget(Transform followTarget)
    {
        target = followTarget;
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
