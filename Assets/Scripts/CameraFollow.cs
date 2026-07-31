using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("따라갈 대상 (Player)")]
    public Transform target;

    [Tooltip("클수록 카메라가 빠르게 따라붙음")]
    public float smoothSpeed = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        // 카메라는 z가 -10이어야 2D 씬이 보이므로 x, y만 따라감
        Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, goal, smoothSpeed * Time.deltaTime);
    }
}
