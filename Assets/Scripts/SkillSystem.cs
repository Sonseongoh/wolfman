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
        Damage,          // 공격력 +value% (0.15 = +15%, 합연산 스택)
        AttackSpeed,     // 공격 간격 -value% (0.2 = 20% 빨라짐, 근거리·원거리 공용)
        MoveSpeed,       // 이동 속도 +value
        MaxHp,           // 최대 체력 +value & 전체 회복
        ProjectileCount, // (보류) 투사체 개수 — 원거리 무기 스킬 부활 시 사용
        Range,           // (보류) 원거리 사거리
        MagnetRange,     // 코인·하트 획득 범위 +value
        MeleeArea,       // 발톱 판정 반경·감지 거리 +value
        WeaponRanged,    // 무기 전환: 근접 발톱 → 원거리 석궁 (위력 절반, 1회성)
    }

    /// <summary>스킬로 늘어난 획득(자석) 범위 보너스 — GoldCoin·HealthPickup이 읽음. 씬 리로드 시 초기화</summary>
    public float magnetBonus;

    /// <summary>스킬 공격력 %보너스 합 (0.15 = +15%) — MeleeAttack·PlayerAttack이 읽음. 합연산 스택</summary>
    public float damageBonus;

    /// <summary>원거리 모드인지 (달빛 참격 획득 후) — 발톱 스킬을 풀에서 제외하는 데 사용</summary>
    bool rangedMode;

    [Tooltip("달빛 참격 투사체 프리팹 (ClawWave) — 무기 전환 시 발사체를 이걸로 교체")]
    public GameObject clawWavePrefab;

    /// <summary>스킬 등급 — 높을수록 강하고 드물다. 카드 색도 이 등급을 따른다</summary>
    public enum SkillRarity { Common, Uncommon, Rare, Epic }

    [System.Serializable]
    public class SkillOption
    {
        public string skillName;
        public string description;
        public SkillRarity rarity;
        public EffectType effect;
        public float value;

        [Tooltip("보조 값 — MaxHp 스킬에서는 최대 체력 증가량 (value = 회복량, -1이면 전체 회복)")]
        public float value2;
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
            // 일반 (가중치 100)
            new SkillOption { skillName = "날카로운 발톱", description = "공격력 +15%", rarity = SkillRarity.Common, effect = EffectType.Damage, value = 0.15f },
            new SkillOption { skillName = "빠른 앞발", description = "공격 속도 +12%", rarity = SkillRarity.Common, effect = EffectType.AttackSpeed, value = 0.12f },
            new SkillOption { skillName = "늑대의 질주", description = "이동 속도 +1", rarity = SkillRarity.Common, effect = EffectType.MoveSpeed, value = 1 },
            new SkillOption { skillName = "상처 핥기", description = "체력 +1 회복", rarity = SkillRarity.Common, effect = EffectType.MaxHp, value = 1, value2 = 0 },
            new SkillOption { skillName = "달의 인력", description = "코인·하트 획득 범위 +0.5", rarity = SkillRarity.Common, effect = EffectType.MagnetRange, value = 0.5f },

            // 고급 (가중치 40)
            new SkillOption { skillName = "사냥꾼의 발톱", description = "공격력 +30%", rarity = SkillRarity.Uncommon, effect = EffectType.Damage, value = 0.3f },
            new SkillOption { skillName = "넓은 휩쓸기", description = "발톱 범위 +0.25, 감지 +0.3", rarity = SkillRarity.Uncommon, effect = EffectType.MeleeArea, value = 0.25f },
            new SkillOption { skillName = "질긴 가죽", description = "체력 +1 회복, 최대 체력 +1", rarity = SkillRarity.Uncommon, effect = EffectType.MaxHp, value = 1, value2 = 1 },

            // 희귀 (가중치 12)
            new SkillOption { skillName = "야수의 격노", description = "공격 속도 +25%", rarity = SkillRarity.Rare, effect = EffectType.AttackSpeed, value = 0.25f },
            new SkillOption { skillName = "거대한 발톱", description = "공격력 +50%", rarity = SkillRarity.Rare, effect = EffectType.Damage, value = 0.5f },
            new SkillOption { skillName = "폭풍 휩쓸기", description = "발톱 범위 +0.5, 감지 +0.55", rarity = SkillRarity.Rare, effect = EffectType.MeleeArea, value = 0.5f },
            new SkillOption { skillName = "야생의 활력", description = "체력 +2 회복, 최대 체력 +1", rarity = SkillRarity.Rare, effect = EffectType.MaxHp, value = 2, value2 = 1 },

            // 에픽 (가중치 3)
            new SkillOption { skillName = "보름달의 힘", description = "공격력 +100%", rarity = SkillRarity.Epic, effect = EffectType.Damage, value = 1f },
            new SkillOption { skillName = "초승달 베기", description = "발톱 범위 +0.9, 감지 +0.95", rarity = SkillRarity.Epic, effect = EffectType.MeleeArea, value = 0.9f },
            new SkillOption { skillName = "불굴의 심장", description = "체력 전체 회복, 최대 체력 +2", rarity = SkillRarity.Epic, effect = EffectType.MaxHp, value = -1, value2 = 2 },
            new SkillOption { skillName = "달빛 참격", description = "발톱 참격을 날려 보낸다\n(원거리 전환, 위력 절반)", rarity = SkillRarity.Epic, effect = EffectType.WeaponRanged, value = 0 },
        };
    }

    static float RarityWeight(SkillRarity r)
    {
        switch (r)
        {
            case SkillRarity.Uncommon: return 40f;
            case SkillRarity.Rare: return 12f;
            case SkillRarity.Epic: return 3f;
            default: return 100f;
        }
    }

    static Color RarityColor(SkillRarity r)
    {
        switch (r)
        {
            case SkillRarity.Uncommon: return new Color(0.4f, 1f, 0.4f);
            case SkillRarity.Rare: return new Color(0.4f, 0.7f, 1f);
            case SkillRarity.Epic: return new Color(0.8f, 0.4f, 1f);
            default: return new Color(0.75f, 0.75f, 0.75f);
        }
    }

    static string RarityLabel(SkillRarity r)
    {
        switch (r)
        {
            case SkillRarity.Uncommon: return "고급";
            case SkillRarity.Rare: return "희귀";
            case SkillRarity.Epic: return "에픽";
            default: return "일반";
        }
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

    void Awake()
    {
        Instance = this;
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        melee = GetComponent<MeleeAttack>();
        health = GetComponent<PlayerHealth>();

        // 씬에 저장된 구버전 풀 무시하고 코드 기준으로 재구성 (근거리 전투 개편)
        pool = BuildDefaultPool();
    }

    /// <summary>WaveManager가 웨이브 클리어 시 호출 — 등급 가중치 추첨으로 서로 다른 3개</summary>
    public void OfferChoices()
    {
        if (choosing || pool.Count < 3) return;

        // 블루문: 3장 중 1장은 희귀 이상 확정
        bool guaranteeRare = GameManager.Instance != null
            && GameManager.Instance.CurrentMoon != null
            && GameManager.Instance.CurrentMoon.guaranteeRareSkill;

        // 석궁 모드에선 발톱(근접) 스킬은 후보에서 제외
        List<SkillOption> copy = new List<SkillOption>();
        foreach (SkillOption s in pool)
        {
            if (rangedMode && s.effect == EffectType.MeleeArea) continue;
            copy.Add(s);
        }

        currentChoices = new SkillOption[3];
        for (int i = 0; i < 3; i++)
        {
            SkillOption pick = null;
            if (i == 0 && guaranteeRare)
                pick = WeightedDraw(copy, SkillRarity.Rare);
            if (pick == null)
                pick = WeightedDraw(copy, SkillRarity.Common);

            currentChoices[i] = pick;
            copy.Remove(pick);
        }

        choosing = true;
        Time.timeScale = 0f;
    }

    /// <summary>등급 가중치로 1개 추첨 (minRarity 미만은 후보 제외). 후보가 없으면 null</summary>
    static SkillOption WeightedDraw(List<SkillOption> from, SkillRarity minRarity)
    {
        float total = 0f;
        foreach (SkillOption s in from)
            if (s.rarity >= minRarity) total += RarityWeight(s.rarity);

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        SkillOption last = null;
        foreach (SkillOption s in from)
        {
            if (s.rarity < minRarity) continue;
            last = s;
            roll -= RarityWeight(s.rarity);
            if (roll <= 0f) return s;
        }
        return last;
    }

    void Update()
    {
        if (!choosing) return;

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
                damageBonus += s.value; // 합연산: +15% 둘 = +30%
                break;
            case EffectType.AttackSpeed:
                // 하한 0.25초: 공속 스택이 쌓여도 초당 4회를 넘지 않게 (밸런스)
                if (attack != null) attack.fireInterval = Mathf.Max(0.25f, attack.fireInterval * (1f - s.value));
                if (melee != null) melee.swingInterval = Mathf.Max(0.25f, melee.swingInterval * (1f - s.value));
                break;
            case EffectType.MoveSpeed:
                if (movement != null) movement.moveSpeed += s.value;
                break;
            case EffectType.MaxHp:
                if (health != null)
                {
                    // 최대치를 먼저 늘려야 회복이 새 최대치까지 찰 수 있다
                    if ((int)s.value2 > 0) health.IncreaseMaxHp((int)s.value2);
                    if (s.value < 0) health.FullHeal();
                    else if ((int)s.value > 0) health.Heal((int)s.value);
                }
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
            case EffectType.WeaponRanged:
                // 무기 전환: 발톱 끄고 참격 날리기 켜기. 이미 쌓인 공속 스택은
                // fireInterval에도 같이 적용돼 있었으므로 그대로 계승된다
                if (melee != null) melee.enabled = false;
                if (attack != null)
                {
                    if (clawWavePrefab != null) attack.projectilePrefab = clawWavePrefab;
                    attack.enabled = true;
                }
                rangedMode = true;
                pool.RemoveAll(p => p.effect == EffectType.WeaponRanged); // 다시 안 뜨게
                break;
        }
    }

    // 임시 UI — 가로 3장 카드 (ChoiceCardUI 공용). 1/2/3 키 또는 클릭
    void OnGUI()
    {
        if (!choosing || currentChoices == null) return;

        string[] names = { currentChoices[0].skillName, currentChoices[1].skillName, currentChoices[2].skillName };
        string[] descs = { currentChoices[0].description, currentChoices[1].description, currentChoices[2].description };
        Color[] accents = { RarityColor(currentChoices[0].rarity), RarityColor(currentChoices[1].rarity), RarityColor(currentChoices[2].rarity) };
        string[] tags = { RarityLabel(currentChoices[0].rarity), RarityLabel(currentChoices[1].rarity), RarityLabel(currentChoices[2].rarity) };

        int clicked = ChoiceCardUI.Draw("웨이브 클리어!  스킬을 선택하세요", names, descs, accents, tags);
        if (clicked >= 0) Choose(clicked);
    }
}
