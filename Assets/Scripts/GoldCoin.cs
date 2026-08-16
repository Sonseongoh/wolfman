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

    [Header("가치별 모습 (1G=동화, 2G=은화, 3G+=금화, 5G+는 큼직하게)")]
    public Sprite bronzeSprite;
    public Sprite silverSprite;
    public Sprite goldSprite;

    Transform player;
    Vector3 baseScale;
    float bobOffset; // 개체마다 맥동 위상을 다르게 (다 같이 깜빡이면 어색)

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        baseScale = transform.localScale;
        bobOffset = Random.value * Mathf.PI * 2f;

        // 가치별 모습 (EnemyHealth가 Instantiate 직후 value를 설정한 뒤라 여기서 판정 가능)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (value >= 3 && goldSprite != null) sr.sprite = goldSprite;
            else if (value == 2 && silverSprite != null) sr.sprite = silverSprite;
            else if (bronzeSprite != null) sr.sprite = bronzeSprite;
        }

        // 정예급(5G+)은 한눈에 띄게 큼직한 금화
        if (value >= 5) baseScale *= 1.25f;
    }

    void Update()
    {
        // 바닥에서 은은하게 맥동 — 돈이 떨어져 있다는 게 눈에 띄게
        transform.localScale = baseScale * (1f + 0.07f * Mathf.Sin(Time.time * 5f + bobOffset));

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
        // 몸통 트리거로만 줍는다 (#143) — 플레이어 콜라이더가 둘(발밑 솔리드+몸통 트리거)이라
        // 가드가 없으면 같은 물리 스텝에 두 번 들어와 골드가 이중 적립될 수 있다.
        if (!other.isTrigger) return;

        if (!other.CompareTag("Player")) return;

        CurrencyManager.Instance?.AddTempGold(value);
        Destroy(gameObject);
    }
}
