using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Tooltip("따라갈 대상 (Player)")]
    public Transform target;

    [Tooltip("클수록 카메라가 빠르게 따라붙음")]
    public float smoothSpeed = 8f;

    float shakeIntensity; // 현재 흔들림 세기 (시간이 지나며 감쇠)
    float shakeDecay;     // 초당 감쇠량

    void Awake()
    {
        Instance = this;
    }

    /// <summary>카메라 흔들기 (#31). intensity = 최대 흔들림 거리(유닛), duration = 지속 시간(초)</summary>
    public static void Shake(float intensity, float duration)
    {
        if (Instance == null) return;

        // 더 강한 흔들림이 이미 진행 중이면 유지
        if (intensity > Instance.shakeIntensity)
        {
            Instance.shakeIntensity = intensity;
            Instance.shakeDecay = duration > 0f ? intensity / duration : intensity * 10f;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 카메라는 z가 -10이어야 2D 씬이 보이므로 x, y만 따라감
        Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
        Vector3 pos = Vector3.Lerp(transform.position, goal, smoothSpeed * Time.deltaTime);

        // 흔들림: 무작위 오프셋을 더하고 점점 잦아듦
        if (shakeIntensity > 0f)
        {
            Vector2 offset = Random.insideUnitCircle * shakeIntensity;
            pos += new Vector3(offset.x, offset.y, 0f);
            shakeIntensity = Mathf.Max(0f, shakeIntensity - shakeDecay * Time.deltaTime);
        }

        transform.position = pos;
    }
}
