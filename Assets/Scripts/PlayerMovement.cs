using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("이동 속도 (유닛/초)")]
    public float moveSpeed = 5f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector2 moveInput;
    Vector2 lungeVelocity;
    float lungeTimer; // 발톱 휘두를 때 짧은 돌진 (#38)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // 걷기 흔들림 연출 자동 장착
        if (GetComponent<WalkWobble>() == null) gameObject.AddComponent<WalkWobble>();
    }

    void Update()
    {
        moveInput = Vector2.zero;

        // 키보드 (PC) — 모바일엔 키보드가 없으므로 null이어도 계속 진행
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveInput.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveInput.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput.x += 1f;

            // 대각선 이동이 더 빨라지지 않도록 정규화
            moveInput = moveInput.normalized;
        }

        // 키보드 입력이 없으면 가상 조이스틱 (#63 — 모바일). 아날로그 강도 유지 (살짝 밀면 천천히)
        if (moveInput == Vector2.zero)
            moveInput = VirtualJoystick.Direction;

        // 이동 방향으로 스프라이트 뒤집기 (원본이 오른쪽을 봄)
        if (sr != null && moveInput.x != 0f)
            sr.flipX = moveInput.x < 0f;
    }

    /// <summary>지정 방향으로 짧게 돌진. 현재 미사용 — 추후 회피/돌진 스킬용으로 보관</summary>
    public void Lunge(Vector2 direction, float force, float duration)
    {
        lungeVelocity = direction.normalized * force;
        lungeTimer = duration;

        // 공격 방향 바라보기
        if (sr != null && direction.x != 0f)
            sr.flipX = direction.x < 0f;
    }

    void FixedUpdate()
    {
        // 돌진 중에는 입력보다 돌진이 우선
        if (lungeTimer > 0f)
        {
            lungeTimer -= Time.fixedDeltaTime;
            rb.linearVelocity = lungeVelocity;
            return;
        }

        rb.linearVelocity = moveInput * moveSpeed;
    }
}
