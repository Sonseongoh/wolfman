using UnityEngine;

/// <summary>
/// 회복 오브 (#32): 적 처치 시 낮은 확률로 드랍. 가까이 가면 끌려오고 먹으면 체력 회복.
/// </summary>
public class HealthPickup : MonoBehaviour
{
    [Tooltip("회복량")]
    public int healAmount = 1;

    [Tooltip("이 거리 안에 오면 플레이어에게 끌려감")]
    public float magnetRange = 1.1f;

    public float magnetSpeed = 7f;

    Transform player;
    Vector3 baseScale;

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (player == null) return;

        // 스킬(달의 인력)로 늘어난 획득 범위 반영
        float range = magnetRange + (SkillSystem.Instance != null ? SkillSystem.Instance.magnetBonus : 0f);

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist < range)
        {
            transform.position = Vector2.MoveTowards(
                transform.position, player.position, magnetSpeed * Time.deltaTime);
        }

        // 심장박동처럼 살짝 두근거리는 연출
        float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 6f);
        transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, baseScale.z);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth hp = other.GetComponent<PlayerHealth>();
            if (hp != null) hp.Heal(healAmount);
            Destroy(gameObject);
        }
    }
}
