using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 웨이브 클리어 시 3택 1 스킬 선택 (#10).
/// 스킬 목록(pool)은 Inspector에서 편집 — 회의 확정 후 내용만 교체하면 됨.
/// 죽으면 씬 리로드로 자연 초기화 (런 한정 성장).
/// </summary>
public class SkillSystem : MonoBehaviour
{
    public static SkillSystem Instance { get; private set; }

    public enum EffectType
    {
        Damage,          // 공격력 +value (근거리·원거리 공용)
        AttackSpeed,     // 공격 간격 -value% (0.2 = 20% 빨라짐, 근거리·원거리 공용)
        MoveSpeed,       // 이동 속도 +value
        MaxHp,           // 최대 체력 +value & 전체 회복
        ProjectileCount, // (보류) 투사체 개수 — 원거리 무기 스킬 부활 시 사용
        Range,           // (보류) 원거리 사거리
        MagnetRange,     // 보석·하트 획득 범위 +value
        MeleeArea,       // 발톱 판정 반경·감지 거리 +value
    }

    /// <summary>스킬로 늘어난 획득(자석) 범위 보너스 — XPGem·HealthPickup이 읽음. 씬 리로드 시 초기화</summary>
    public float magnetBonus;

    [System.Serializable]
    public class SkillOption
    {
        public string skillName;
        public string description;
        public EffectType effect;
        public float value;
    }

    [Header("스킬 풀 — 코드가 기준 (Awake에서 아래 목록으로 재구성됨)")]
    public List<SkillOption> pool = new List<SkillOption>();

    /// <summary>
    /// 근거리(늑대인간) 전투 기준 스킬 풀. 씬에 저장된 구버전 풀 대신 항상 이 목록을 쓴다.
    /// 회의에서 스킬 풀이 확정되면 ScriptableObject 데이터로 옮길 예정.
    /// </summary>
    static List<SkillOption> BuildDefaultPool()
    {
        return new List<SkillOption>
        {
            new SkillOption { skillName = "날카로운 발톱", description = "공격력 +1", effect = EffectType.Damage, value = 1 },
            new SkillOption { skillName = "빠른 앞발", description = "공격 속도 +20%", effect = EffectType.AttackSpeed, value = 0.2f },
            new SkillOption { skillName = "늑대의 질주", description = "이동 속도 +1", effect = EffectType.MoveSpeed, value = 1 },
            new SkillOption { skillName = "질긴 가죽", description = "최대 체력 +1, 전체 회복", effect = EffectType.MaxHp, value = 1 },
            new SkillOption { skillName = "넓은 휩쓸기", description = "발톱 범위 +0.25, 감지 +0.3", effect = EffectType.MeleeArea, value = 0.25f },
            new SkillOption { skillName = "달의 인력", description = "보석·하트 획득 범위 +0.5", effect = EffectType.MagnetRange, value = 0.5f },
        };
    }

    public List<SkillOption> acquired = new List<SkillOption>(); // 이번 런에 얻은 스킬

    SkillOption[] currentChoices;
    bool choosing;

    /// <summary>선택창이 떠 있는지 (HitStop 등이 시간 정지 유지 판단에 사용)</summary>
    public bool IsChoosing => choosing;

    PlayerMovement movement;
    PlayerAttack attack;
    MeleeAttack melee;
    PlayerHealth health;
    PlayerLevel level;

    void Awake()
    {
        Instance = this;
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        melee = GetComponent<MeleeAttack>();
        health = GetComponent<PlayerHealth>();
        level = GetComponent<PlayerLevel>();

        // 씬에 저장된 구버전 풀 무시하고 코드 기준으로 재구성 (근거리 전투 개편)
        pool = BuildDefaultPool();
    }

    /// <summary>WaveManager가 웨이브 클리어 시 호출</summary>
    public void OfferChoices()
    {
        if (choosing || pool.Count < 3) return;

        // 풀에서 서로 다른 3개 뽑기
        List<SkillOption> copy = new List<SkillOption>(pool);
        currentChoices = new SkillOption[3];
        for (int i = 0; i < 3; i++)
        {
            int idx = Random.Range(0, copy.Count);
            currentChoices[i] = copy[idx];
            copy.RemoveAt(idx);
        }

        choosing = true;
        Time.timeScale = 0f;
    }

    void Update()
    {
        if (!choosing) return;

        // 레벨업 선택창이 떠 있으면 그것부터 처리하게 대기
        if (level != null && level.IsChoosing) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Choose(0);
        else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Choose(1);
        else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Choose(2);
    }

    void Choose(int index)
    {
        SkillOption pick = currentChoices[index];
        Apply(pick);
        acquired.Add(pick);

        choosing = false;
        currentChoices = null;
        Time.timeScale = 1f;
    }

    void Apply(SkillOption s)
    {
        switch (s.effect)
        {
            case EffectType.Damage:
                if (attack != null) attack.bonusDamage += (int)s.value;
                break;
            case EffectType.AttackSpeed:
                if (attack != null) attack.fireInterval = Mathf.Max(0.15f, attack.fireInterval * (1f - s.value));
                if (melee != null) melee.swingInterval = Mathf.Max(0.2f, melee.swingInterval * (1f - s.value));
                break;
            case EffectType.MoveSpeed:
                if (movement != null) movement.moveSpeed += s.value;
                break;
            case EffectType.MaxHp:
                if (health != null) health.IncreaseMaxHp((int)s.value);
                break;
            case EffectType.ProjectileCount:
                if (attack != null) attack.projectilesPerShot += (int)s.value;
                break;
            case EffectType.Range:
                if (attack != null) attack.range += s.value;
                break;
            case EffectType.MagnetRange:
                magnetBonus += s.value;
                break;
            case EffectType.MeleeArea:
                if (melee != null)
                {
                    melee.hitRadius += s.value;
                    melee.triggerRange += s.value + 0.05f;
                }
                break;
        }
    }

    // 임시 UI — 가로 3장 카드 (ChoiceCardUI 공용). 1/2/3 키 또는 클릭
    void OnGUI()
    {
        if (!choosing || currentChoices == null) return;
        if (level != null && level.IsChoosing) return;

        string[] names = { currentChoices[0].skillName, currentChoices[1].skillName, currentChoices[2].skillName };
        string[] descs = { currentChoices[0].description, currentChoices[1].description, currentChoices[2].description };

        int clicked = ChoiceCardUI.Draw("웨이브 클리어!  스킬을 선택하세요", names, descs);
        if (clicked >= 0) Choose(clicked);
    }
}
