using UnityEngine;

/// <summary>
/// 늑대인간 근거리 공격 (#38): 근접 범위의 최근접 적 방향으로 발톱을 휘두른다.
/// 휘두른 지점 원형 판정으로 광역 데미지 + 넉백. PlayerTransform이 켜고 끈다.
/// </summary>
public class MeleeAttack : MonoBehaviour
{
    [Tooltip("참격 이펙트 스프라이트 (ClawSlash)")]
    public Sprite slashSprite;

    [Tooltip("공격 포즈 스프라이트 — 프레임 배열이 비어 있을 때만 사용")]
    public Sprite attackPoseSprite;

    [Tooltip("공격 애니메이션 프레임들 (순서대로 재생). 있으면 포즈 대신 이걸 사용")]
    public Sprite[] attackFrames;

    [Tooltip("프레임 하나당 표시 시간(초)")]
    public float attackFrameTime = 0.05f;

    [Tooltip("몇 초마다 휘두를지")]
    public float swingInterval = 0.6f;

    [Tooltip("이 거리 안에 적이 있으면 휘두름")]
    public float triggerRange = 2.2f;

    [Tooltip("참격 판정 반경 (플레이어 앞쪽 지점 기준)")]
    public float hitRadius = 1.3f;

    [Tooltip("기본 데미지 (스킬 공격력 %보너스와 달 배율이 곱해짐)")]
    public int baseDamage = 10;

    float timer;
    PlayerAttack rangedAttack; // 공격력 보너스 공유용
    PlayerMovement movement;
    WalkWobble wobble;

    /// <summary>공격 애니메이션 재생 중인지 (PlayerWalkAnim이 양보 판단에 사용)</summary>
    public bool IsAnimating { get; private set; }

    void Awake()
    {
        rangedAttack = GetComponent<PlayerAttack>();
        movement = GetComponent<PlayerMovement>();
        wobble = GetComponent<WalkWobble>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < swingInterval) return;

        EnemyHealth target = FindNearest();
        if (target == null) return;

        timer = 0f;
        Swing(target.transform.position);
    }

    EnemyHealth FindNearest()
    {
        EnemyHealth[] enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        EnemyHealth nearest = null;
        float best = triggerRange;

        foreach (EnemyHealth e in enemies)
        {
            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist < best)
            {
                best = dist;
                nearest = e;
            }
        }
        return nearest;
    }

    void Swing(Vector3 targetPos)
    {
        Vector2 dir = ((Vector2)(targetPos - transform.position)).normalized;
        Vector2 hitCenter = (Vector2)transform.position + dir * 1.1f;

        // 데미지 = 기본 × (1 + 스킬 공격력 %합) × 달의 플레이어 강화 배율
        float power = 1f;
        if (GameManager.Instance != null && GameManager.Instance.CurrentMoon != null)
            power = GameManager.Instance.CurrentMoon.playerPowerMultiplier;

        float skillMult = 1f + (SkillSystem.Instance != null ? SkillSystem.Instance.damageBonus : 0f);
        int damage = Mathf.Max(1, Mathf.RoundToInt(baseDamage * skillMult * power));

        // 원형 판정 광역 — 닿은 적 전부 타격 + 넉백
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, hitRadius);
        foreach (Collider2D h in hits)
        {
            EnemyHealth enemy = h.GetComponent<EnemyHealth>();
            if (enemy != null) enemy.TakeDamage(damage, dir);
        }

        // 참격 연출 (아래로 휘두를 땐 뒤집어서 자연스럽게)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        SlashEffect.Spawn(slashSprite, hitCenter, angle, dir.x < 0f);

        PlayAttackFeedback(dir);
    }

    /// <summary>
    /// 공격 모션(꿀렁임·기울기·프레임 애니·방향 보기). 근접 휘두르기와
    /// 달빛 참격(PlayerAttack — 이 컴포넌트가 꺼진 상태)이 함께 사용한다.
    /// 플레이어 위치는 절대 건드리지 않는다 — 이동 주도권은 항상 플레이어.
    /// </summary>
    public void PlayAttackFeedback(Vector2 dir)
    {
        // 은은한 부풀기 + 공격 방향으로 기울었다 복귀
        if (wobble == null) wobble = GetComponent<WalkWobble>();
        if (wobble != null)
        {
            wobble.Punch(0.1f);
            wobble.SwingTilt(dir.x >= 0f ? -14f : 14f);
        }

        // 공격 방향 바라보기 + 포즈 교체(에셋 있으면)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (dir.x != 0f) sr.flipX = dir.x < 0f;

            if ((attackFrames != null && attackFrames.Length > 0) || attackPoseSprite != null)
            {
                StopAllCoroutines();
                StartCoroutine(PlayAttackAnim(sr));
            }
        }
    }

    System.Collections.IEnumerator PlayAttackAnim(SpriteRenderer sr)
    {
        IsAnimating = true;

        if (attackFrames != null && attackFrames.Length > 0)
        {
            // 프레임 애니메이션 재생
            foreach (Sprite frame in attackFrames)
            {
                if (frame != null) sr.sprite = frame;
                yield return new WaitForSeconds(attackFrameTime);
            }
        }
        else
        {
            // 프레임이 없으면 단일 포즈 교체
            sr.sprite = attackPoseSprite;
            yield return new WaitForSeconds(0.16f);
        }

        // 끝나면 늑대 기본 모습으로 확실히 복귀
        // (연속 공격으로 애니가 끊겨도 공격 프레임에 멈춰 있지 않게)
        PlayerTransform form = GetComponent<PlayerTransform>();
        if (form != null && form.wolfSprite != null)
            sr.sprite = form.wolfSprite;

        IsAnimating = false;
    }
}
