using UnityEngine;

/// <summary>
/// 걷기 프레임 애니메이션 (팀 제작 시트). 이동 중일 때 프레임을 순환 재생하고,
/// 멈추면 기본(대기) 스프라이트로 복귀. 공격 애니메이션(MeleeAttack)이 재생 중일 땐 양보한다.
/// </summary>
public class PlayerWalkAnim : MonoBehaviour
{
    [Tooltip("걷기 프레임들 (WolfWalk_0~3)")]
    public Sprite[] frames;

    [Tooltip("프레임 하나당 표시 시간(초)")]
    public float frameTime = 0.1f;

    SpriteRenderer sr;
    Rigidbody2D rb;
    PlayerTransform form;
    MeleeAttack melee;
    float timer;
    int index;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        form = GetComponent<PlayerTransform>();
        melee = GetComponent<MeleeAttack>();
    }

    void Update()
    {
        if (frames == null || frames.Length == 0 || sr == null) return;

        // 공격 애니메이션이 진행 중이면 걷기가 스프라이트를 덮어쓰지 않는다
        if (melee != null && melee.IsAnimating) return;

        bool moving = rb != null && rb.linearVelocity.sqrMagnitude > 0.05f;

        if (moving)
        {
            timer += Time.deltaTime;
            if (timer >= frameTime)
            {
                timer -= frameTime;
                index = (index + 1) % frames.Length;
            }
            sr.sprite = frames[index];
        }
        else
        {
            timer = 0f;
            index = 0;
            if (form != null && form.wolfSprite != null)
                sr.sprite = form.wolfSprite;
        }
    }
}
