using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLevel : MonoBehaviour
{
    public int level = 1;

    int xp;
    int xpToNext = 5;
    bool choosing; // 레벨업 선택창이 떠 있는 상태

    /// <summary>선택창이 떠 있는지 (SkillSystem이 순서 조율에 사용)</summary>
    public bool IsChoosing => choosing;

    PlayerMovement movement;
    PlayerAttack attack;
    PlayerHealth health;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<PlayerHealth>();
    }

    public void AddXP(int amount)
    {
        xp += amount;
        if (!choosing && xp >= xpToNext) LevelUp();
    }

    void LevelUp()
    {
        xp -= xpToNext;
        level++;
        xpToNext = 5 + level * 3; // 레벨이 오를수록 필요 경험치 증가
        choosing = true;
        Time.timeScale = 0f; // 선택하는 동안 게임 정지
    }

    void Update()
    {
        if (!choosing) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Choose(1);
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Choose(2);
        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Choose(3);
    }

    void Choose(int pick)
    {
        if (pick == 1)
        {
            if (attack != null)
                attack.fireInterval = Mathf.Max(0.2f, attack.fireInterval * 0.8f);

            // 근거리(늑대인간) 공격 속도에도 적용
            MeleeAttack melee = GetComponent<MeleeAttack>();
            if (melee != null)
                melee.swingInterval = Mathf.Max(0.2f, melee.swingInterval * 0.8f);
        }
        else if (pick == 2 && movement != null)
            movement.moveSpeed += 1f;
        else if (pick == 3 && health != null)
            health.IncreaseMaxHp(1);

        choosing = false;
        Time.timeScale = 1f;

        // 남은 경험치로 또 레벨업이 가능하면 연속 레벨업
        if (xp >= xpToNext) LevelUp();
    }

    // 임시 UI — 강화 선택은 가로 3장 카드 (ChoiceCardUI 공용)
    static readonly string[] upgradeNames = { "빠른 공격", "날랜 발", "강인한 육체" };
    static readonly string[] upgradeDescs = { "공격 속도 +25%", "이동 속도 +1", "최대 체력 +1\n전체 회복" };

    void OnGUI()
    {
        GUIStyle style = new GUIStyle { fontSize = 22, normal = { textColor = Color.cyan } };
        GUI.Label(new Rect(20, 56, 400, 30), $"Lv.{level}  XP: {xp} / {xpToNext}", style);

        if (choosing)
        {
            int clicked = ChoiceCardUI.Draw($"LEVEL UP!  Lv.{level}", upgradeNames, upgradeDescs);
            if (clicked >= 0) Choose(clicked + 1);
        }
    }
}
