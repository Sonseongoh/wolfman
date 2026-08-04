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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        // 걷기 흔들림 연출 자동 장착
        if (GetComponent<WalkWobble>() == null) gameObject.AddComponent<WalkWobble>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        moveInput = Vector2.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveInput.y += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveInput.y -= 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput.x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput.x += 1f;

        // 대각선 이동이 더 빨라지지 않도록 정규화
        moveInput = moveInput.normalized;

        // 이동 방향으로 스프라이트 뒤집기 (원본이 오른쪽을 봄)
        if (sr != null && moveInput.x != 0f)
            sr.flipX = moveInput.x < 0f;
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
