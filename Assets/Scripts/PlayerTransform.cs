using UnityEngine;

/// <summary>
/// 플레이어 형태 관리 (#38).
/// 팀 결정(2026-08-05): 전투(사냥)에서는 항상 늑대인간 형태로 진행.
/// - 늑대인간 = 근거리 발톱(MeleeAttack), 석궁(PlayerAttack)은 비활성 (추후 무기형 스킬로 활용 여지)
/// - 인간 스프라이트는 마을(운영) 페이즈에서 사용 예정
/// </summary>
public class PlayerTransform : MonoBehaviour
{
    [Tooltip("인간 상태 스프라이트 (마을 페이즈용 보관)")]
    public Sprite humanSprite;

    [Tooltip("늑대인간 상태 스프라이트 (전투 기본)")]
    public Sprite wolfSprite;

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
        // 전투 씬 = 항상 늑대인간
        if (wolfSprite != null && sr != null) sr.sprite = wolfSprite;
        if (meleeAttack != null) meleeAttack.enabled = true;
        if (rangedAttack != null) rangedAttack.enabled = false;
    }
}
