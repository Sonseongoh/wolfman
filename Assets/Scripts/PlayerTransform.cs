using UnityEngine;

/// <summary>
/// 플레이어 형태 관리 (#38, #53).
/// 어떤 형태인지는 씬이 아니라 <see cref="PlayerFormRule"/> 이 페이즈를 보고 정한다 —
/// 마을(운영)이면 인간, 그 외(사냥)면 늑대인간.
/// - 늑대인간 = 근거리 발톱(MeleeAttack)
/// - 석궁(PlayerAttack)은 어느 형태에서도 꺼둔다. 팀 결정(2026-08-05)으로 사냥은 발톱 고정이고,
///   원거리는 추후 무기형 스킬(#30)로 되살릴 여지만 남겨둔 상태다.
/// </summary>
public class PlayerTransform : MonoBehaviour
{
    [Tooltip("인간 상태 스프라이트 (마을 페이즈)")]
    public Sprite humanSprite;

    [Tooltip("늑대인간 상태 스프라이트 (사냥 페이즈)")]
    public Sprite wolfSprite;

    /// <summary>지금 이 플레이어가 취하고 있는 형태.</summary>
    public PlayerForm CurrentForm { get; private set; }

    /// <summary>
    /// 스프라이트를 현재 형태의 유휴(대기) 모습으로 되돌린다.
    /// 걷기(#41)와 공격(#38) 애니메이션이 끝날 때 함께 쓰는 복귀 지점이다 —
    /// 돌아갈 곳을 애니메이션이 각자 정하면, 형태가 인간이어도 늑대로 돌아가버린다 (#53).
    /// </summary>
    public void RestoreIdleSprite()
    {
        if (sr == null || IdleSprite == null) return;

        sr.sprite = IdleSprite;
    }

    /// <summary>현재 형태의 유휴 스프라이트.</summary>
    Sprite IdleSprite => CurrentForm == PlayerForm.Human ? humanSprite : wolfSprite;

    SpriteRenderer sr;
    PlayerAttack rangedAttack;
    MeleeAttack meleeAttack;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rangedAttack = GetComponent<PlayerAttack>();
        meleeAttack = GetComponent<MeleeAttack>();
    }

    void Start()
    {
        // GameManager 는 사냥 씬에 살면서 씬을 넘어 유지된다. 마을 씬을 단독으로 재생하면
        // 아직 없을 수 있고, 그때 페이즈는 "모른다"(null) — 규칙이 그 경우의 답도 쥐고 있다.
        RoundPhase? phase = GameManager.Instance != null
            ? GameManager.Instance.Phase
            : (RoundPhase?)null;

        Apply(PlayerFormRule.For(phase));
    }

    void Apply(PlayerForm form)
    {
        CurrentForm = form;

        RestoreIdleSprite();

        // 인간(마을)에서는 전투 능력이 켜지지 않는다. 컴포넌트 자체가 없는 구성(마을 플레이어)이면
        // 조용히 넘어간다 — 마을엔 적이 없어서 전투 컴포넌트를 아예 빼는 게 검증된 구성이다.
        if (meleeAttack != null) meleeAttack.enabled = form == PlayerForm.Werewolf;
        if (rangedAttack != null) rangedAttack.enabled = false;
    }
}
