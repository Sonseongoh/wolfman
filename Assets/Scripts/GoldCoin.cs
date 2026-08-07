using UnityEngine;

/// <summary>
/// 적 처치 시 떨어지는 골드 코인 (#8 연계 — XP 보석을 대체).
/// 자석 범위 안이면 플레이어에게 끌려오고, 주우면 주머니(TempGold)에 들어간다.
/// 우상단 주머니 잔액이 굴러 올라가는 연출은 GoldPanelUI가 처리.
/// </summary>
public class GoldCoin : MonoBehaviour
{
    [Tooltip("이 코인의 가치 (EnemyHealth가 스폰할 때 goldDrop으로 설정)")]
    public int value = 1;

    [Tooltip("이 거리 안에 오면 플레이어에게 끌려감")]
    public float magnetRange = 1.1f;

    public float magnetSpeed = 7f;

    Transform player;

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
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
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        CurrencyManager.Instance?.AddTempGold(value);
        Destroy(gameObject);
    }
}
