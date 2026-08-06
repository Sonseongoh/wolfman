using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 사냥 탈출 포탈 (#46). 웨이브 시작 시 확률로 열리고, 그 웨이브 동안만 유지된다.
/// 플레이어가 닿으면 살아서 귀환 — MainScene으로 이동해 정산(#6)·뱅킹(#8) 흐름을 탄다.
/// </summary>
public class EscapePortal : MonoBehaviour
{
    [Tooltip("소용돌이 회전 속도 (도/초)")]
    public float rotateSpeed = 120f;

    [Tooltip("맥동(커졌다 작아졌다) 폭")]
    public float pulseAmount = 0.08f;
    public float pulseSpeed = 3f;

    [Tooltip("열리고 닫힐 때 커지고 줄어드는 시간(초)")]
    public float openDuration = 0.4f;

    Vector3 baseScale;
    float openTimer;
    bool closing;
    bool used;

    void Start()
    {
        baseScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        if (closing) return;

        // 열림 연출(0 → 원래 크기) 후 맥동
        openTimer += Time.deltaTime;
        float open = Mathf.Clamp01(openTimer / openDuration);
        float pulse = 1f + pulseAmount * Mathf.Sin(Time.time * pulseSpeed);
        transform.localScale = baseScale * (open * pulse);
    }

    /// <summary>웨이브가 끝나면 WaveManager가 호출 — 줄어들며 닫힌다</summary>
    public void Close()
    {
        if (closing) return;
        closing = true;
        StartCoroutine(CloseRoutine());
    }

    System.Collections.IEnumerator CloseRoutine()
    {
        Vector3 from = transform.localScale;
        float t = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(from, Vector3.zero, t / openDuration);
            yield return null;
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || closing) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth hp = other.GetComponent<PlayerHealth>();
        if (hp != null && hp.IsDead) return;

        used = true;

        // Phase가 Hunt인 채로 MainScene에 도착하면 RoundController(#6)가
        // "살아서 귀환"으로 인식해 임시 골드를 금고로 확정(#8)하고 정산 화면을 띄운다
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
    }
}
