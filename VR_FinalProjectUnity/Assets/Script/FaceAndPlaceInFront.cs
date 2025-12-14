using UnityEngine;

public class FaceAndPlaceInFront : MonoBehaviour
{
    public Transform head;               // XR Main Camera
    public float distance = 0.7f;        // 离头显多远
    public float heightOffset = -0.1f;   // 稍微低一点更舒服
    public bool followWhileActive = false; // 打开期间是否一直跟随

    void OnEnable()
    {
        Place();
    }

    void LateUpdate()
    {
        if (followWhileActive) Place();
        else FaceOnly();
    }

    void Place()
    {
        if (head == null) return;

        // 放在头显前方（只用水平前方，避免抬头低头UI飞来飞去）
        Vector3 forward = head.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 pos = head.position + forward * distance;
        pos.y += heightOffset;
        transform.position = pos;

        FaceOnly();
    }

    void FaceOnly()
    {
        if (head == null) return;

        Vector3 dir = transform.position - head.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir);
    }
}
