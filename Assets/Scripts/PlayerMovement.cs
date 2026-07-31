using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Tooltip("이동 속도 (유닛/초)")]
    public float moveSpeed = 5f;

    Rigidbody2D rb;
    Vector2 moveInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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
    }

    void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }
}
