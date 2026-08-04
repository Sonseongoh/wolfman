using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Tooltip("맞을 수 있는 횟수")]
    public int maxHp = 2;

    [Tooltip("죽을 때 떨어뜨릴 경험치 보석 프리팹")]
    public GameObject gemPrefab;

    [Header("타격감")]
    [Tooltip("피격 시 밀려나는 힘")]
    public float knockbackForce = 6f;
    public float knockbackDuration = 0.15f;

    int hp;
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

    /// <summary>hitDirection: 공격이 날아온 방향 (넉백용). 생략 시 넉백 없음.</summary>
    public void TakeDamage(int amount, Vector2 hitDirection = default)
    {
        hp -= amount;

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
            if (gemPrefab != null)
                Instantiate(gemPrefab, transform.position, Quaternion.identity);

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
