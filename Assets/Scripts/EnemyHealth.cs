using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Tooltip("체력 (플레이어 기본 데미지 10 스케일 기준 — 기본 적 10 = 한 방)")]
    public int maxHp = 10;

    [Tooltip("죽을 때 떨어뜨릴 골드 코인 프리팹 (#8 — XP 보석 대체)")]
    public GameObject gemPrefab;

    [Tooltip("코인 1개의 가치 (주워야 주머니에 들어감)")]
    public int goldDrop = 1;

    [Tooltip("낮은 확률로 떨어뜨릴 회복 오브 프리팹 (#32)")]
    public GameObject healthDropPrefab;

    [Tooltip("회복 오브 드랍 확률 (0.05 = 5%)")]
    [Range(0f, 1f)] public float healthDropChance = 0.05f;

    [Header("타격감")]
    [Tooltip("피격 시 밀려나는 힘")]
    public float knockbackForce = 6f;
    public float knockbackDuration = 0.15f;

    int hp;
    bool isDead; // 같은 프레임에 여러 발 맞아도 사망 처리는 한 번만
    SpriteRenderer sr;
    Color originalColor;
    EnemyChase chase;

    void Awake()
    {
        hp = maxHp;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
        chase = GetComponent<EnemyChase>();
    }

    /// <summary>달 배율 등으로 체력 보정 (스폰 직후 호출 — 현재 체력도 함께 재설정)</summary>
    public void ApplyHpMultiplier(float multiplier)
    {
        maxHp = Mathf.Max(1, Mathf.RoundToInt(maxHp * multiplier));
        hp = maxHp;
    }

    /// <summary>hitDirection: 공격이 날아온 방향 (넉백용). 생략 시 넉백 없음.</summary>
    public void TakeDamage(int amount, Vector2 hitDirection = default)
    {
        if (isDead) return;

        hp -= amount;

        // 데미지 숫자 (#31)
        DamageNumber.Spawn(transform.position, amount.ToString(), new Color(1f, 0.9f, 0.4f));

        // 흰색 섬광
        if (sr != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashWhite());
        }

        // 넉백
        if (chase != null && hitDirection != default)
            chase.ApplyKnockback(hitDirection, knockbackForce, knockbackDuration);
        if (hp <= 0)
        {
            isDead = true;

            if (gemPrefab != null)
            {
                // 달의 드랍 배수 적용 (하베스트문 = 코인 2개)
                int drops = 1;
                if (GameManager.Instance != null && GameManager.Instance.CurrentMoon != null)
                    drops = Mathf.Max(1, Mathf.RoundToInt(GameManager.Instance.CurrentMoon.dropMultiplier));

                for (int i = 0; i < drops; i++)
                {
                    Vector3 offset = i == 0 ? Vector3.zero : (Vector3)(Random.insideUnitCircle * 0.4f);
                    GameObject c = Instantiate(gemPrefab, transform.position + offset, Quaternion.identity);

                    GoldCoin coin = c.GetComponent<GoldCoin>();
                    if (coin != null) coin.value = goldDrop;
                }
            }

            // 회복 오브 드랍 (#32)
            if (healthDropPrefab != null && Random.value < healthDropChance)
                Instantiate(healthDropPrefab,
                    transform.position + (Vector3)(Random.insideUnitCircle * 0.3f),
                    Quaternion.identity);

            if (WaveManager.Instance != null)
                WaveManager.Instance.NotifyEnemyDied();

            DeathPop.Spawn(sr); // 처치 연출
            Destroy(gameObject);
        }
    }

    System.Collections.IEnumerator FlashWhite()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        sr.color = originalColor;
    }
}
