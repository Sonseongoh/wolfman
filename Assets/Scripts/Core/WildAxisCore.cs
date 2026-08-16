using System;

/// <summary>굶주림이 얼마나 깊은가 (#117). 축의 음수 쪽을 다섯 칸으로 끊은 것이다.</summary>
public enum HungerStage
{
    /// <summary>포식 — 배가 부르다. 페널티 없음.</summary>
    Sated,

    /// <summary>허기 — 공격력이 처음 깎인다.</summary>
    Hungry,

    /// <summary>주림 — 공격력이 더 깎이고 최대 체력도 줄어든다.</summary>
    Famished,

    /// <summary>아사 — 주민이 피한다. 상점이 닫힌다.</summary>
    Starving,

    /// <summary>한계 — 굶주림 쪽 끝. 폭주.</summary>
    Limit,
}

/// <summary>
/// 굶주림 단계 하나가 거는 것 전부 (#117). **한 줄이 한 단계다.**
///
/// 원래 이 값들은 <c>switch</c> 다섯 군데에 흩어져 있었다 — 공격 배율, 최대 체력, 한 입,
/// 상점, 이름. 단계를 하나 늘리려면 다섯 곳을 고쳐야 했고, 그중 하나를 빠뜨려도
/// 컴파일은 통과한다. 표로 모으면 빠뜨릴 곳이 없다.
///
/// 단계 이름을 여기 둔 것은 UI 문구여서가 아니라 <c>CONTEXT.md</c> 용어집의 낱말이기 때문이다 —
/// 화면과 문서와 코드가 같은 단어를 쓰게 하려면 출처가 하나여야 한다.
/// </summary>
public static class HungerRule
{
    readonly struct Effect
    {
        public readonly string Name;
        public readonly float Attack;
        public readonly int MaxHpPenalty;
        public readonly int Bite;
        public readonly bool ShopOpen;

        public Effect(string name, float attack, int maxHpPenalty, int bite, bool shopOpen)
        {
            Name = name;
            Attack = attack;
            MaxHpPenalty = maxHpPenalty;
            Bite = bite;
            ShopOpen = shopOpen;
        }
    }

    // 순서가 HungerStage 와 같아야 한다 — 인덱스로 찾는다.
    // (표와 enum 이 어긋나지 않는지는 WildAxisCoreTests 가 단계마다 확인한다.)
    static readonly Effect[] Table =
    {
        //          이름     공격배율  최대체력  한 입  상점
        new Effect("포식", 1.00f, 0, 0, true),
        new Effect("허기", 0.85f, 0, 1, true),
        new Effect("주림", 0.70f, 1, 2, true),
        new Effect("아사", 0.50f, 2, 3, false),
        new Effect("한계", 0.50f, 2, 3, false),
    };

    /// <summary>단계 수 — 표와 enum 이 같은 크기인지 확인하는 데 쓴다.</summary>
    public static int Count => Table.Length;

    /// <summary>단계의 한국어 이름 (CONTEXT.md 용어집).</summary>
    public static string Name(HungerStage stage) => Table[(int)stage].Name;

    /// <summary>이 단계가 공격에 거는 배율.</summary>
    public static float AttackMultiplier(HungerStage stage) => Table[(int)stage].Attack;

    /// <summary>이 단계가 깎는 최대 체력. 빼는 양이지 새 최대치가 아니다.</summary>
    public static int MaxHpPenalty(HungerStage stage) => Table[(int)stage].MaxHpPenalty;

    /// <summary>이 단계에서 처치 한 번이 돌려주는 체력. 굶주릴수록 한 입이 크다.</summary>
    public static int Bite(HungerStage stage) => Table[(int)stage].Bite;

    /// <summary>상점을 열 수 있는가. 아사부터는 주민이 피해서 닫힌다 (#13 이 소비).</summary>
    public static bool ShopOpen(HungerStage stage) => Table[(int)stage].ShopOpen;
}

/// <summary>어느 끝을 넘어 폭주했는가 (#117). 양끝 모두 폭주지만 원인이 반대다.</summary>
public enum BreakoutSide
{
    /// <summary>야성 쪽 — 몸이 앞서 나가다 몸을 빼앗겼다.</summary>
    Wild,

    /// <summary>굶주림 쪽 — 이성을 잃었다.</summary>
    Starve,
}

/// <summary>
/// 야성과 굶주림을 하나의 값으로 다루는 축 (#117 — 축을 확정한 것은 #102, ADR 0004).
///
/// 굶주림 ◀──── 안전(0) ────▶ 야성. **값 하나에 임계선 둘**이고, 어느 끝에 닿아도 결과는 폭주다.
/// 축을 둘로 나누지 않은 이유가 이것이다 — 나누면 "야성 90 · 굶주림 90" 같은 상태가 표현되는데
/// 그건 설계상 존재하지 않는다. 사냥은 야성 쪽으로, 마을 체류는 굶주림 쪽으로 **같은 값을** 민다.
///
/// 판정을 전부 여기 모아 UnityEngine 에 의존하지 않게 했다 — 씬을 열지 않고 WSL 에서 검사한다.
/// 씬 수명과 배율 수집은 <c>WildAxisManager</c> 가 맡는다.
///
/// 여기 있는 수치는 **전부 첫 조정 대상**이다. 밸런싱 티켓에서 갈아엎힐 것을 전제로 둔 값이지,
/// 검증된 값이 아니다.
/// </summary>
public class WildAxisCore
{
    /// <summary>야성 쪽 한계. 여기 닿으면 폭주.</summary>
    public const float WildLimit = 100f;

    /// <summary>굶주림 쪽 한계. 여기 닿으면 폭주.</summary>
    public const float StarveLimit = -100f;

    /// <summary>
    /// 달이 정하지 않을 때의 야성 상승 속도 (축 단위/초). 0.5 = 분당 +30.
    /// <c>MoonData.wildRisePerSecond</c> 필드 초기값의 단일 출처다 —
    /// 달 에셋 9개가 전부 이 값으로 로드되므로 여기 하나만 고치면 전부 따라온다.
    /// </summary>
    public const float DefaultWildRisePerSecond = 0.5f;

    /// <summary>
    /// 마을에 머무는 동안 축이 굶주림 쪽으로 내려가는 속도 (축 단위/초).
    /// 달과 무관하다 — 마을에 있는 동안은 하늘을 보지 않는다.
    /// 창고(#55)가 늦출 자리가 여기다.
    /// </summary>
    public const float VillageDriftPerSecond = 1f;

    /// <summary>처치 한 번이 축을 야성 쪽으로 미는 양. 먹었으니 굶주림이 조금 풀린다.</summary>
    public const float EatNudgePerKill = 1f;

    // 단계 경계 (음수 쪽). 부등호로만 비교하므로 float 정밀도 문제가 없다 —
    // 곱셈 없이 값 하나를 상수와 견주기만 한다.
    const float HungryThreshold = -20f;
    const float FamishedThreshold = -50f;
    const float StarvingThreshold = -75f;

    /// <summary>축의 현재 위치. 음수면 굶주림 쪽, 양수면 야성 쪽. 시작은 안전한 0.</summary>
    public float Value { get; private set; }

    /// <summary>지금 굶주림이 어느 단계인가. 값에서 파생되므로 따로 들고 있지 않는다.</summary>
    public HungerStage Stage
    {
        get
        {
            if (Value > HungryThreshold) return HungerStage.Sated;
            if (Value > FamishedThreshold) return HungerStage.Hungry;
            if (Value > StarvingThreshold) return HungerStage.Famished;
            if (Value > StarveLimit) return HungerStage.Starving;
            return HungerStage.Limit;
        }
    }

    /// <summary>
    /// 굶주림이 공격에 거는 배율. <c>AttackPower.ForHit</c> 한 곳에서만 곱해진다.
    ///
    /// 정수 데미지라 단계가 넷이어도 체감은 셋일 수 있다 —
    /// 달빛 참격(base 5)에서는 0.85 와 0.70 이 같은 4 로 붙는다
    /// (<c>AttackPowerRuleTests.기본_데미지가_작으면_배율이_계단으로_뭉갠다</c>). 버그가 아니다.
    /// </summary>
    public float AttackMultiplier => HungerRule.AttackMultiplier(Stage);

    /// <summary>굶주림이 깎는 최대 체력. 빼는 양이지 새 최대치가 아니다 — 단계가 풀리면 걷힌다.</summary>
    public int MaxHpPenalty => HungerRule.MaxHpPenalty(Stage);

    /// <summary>
    /// 상점을 열 수 있는가 (#13 이 소비). 아사부터는 주민이 피해서 닫힌다.
    ///
    /// 마을에 남는 유일한 이유가 상점인데 굶주릴수록 그게 닫히므로,
    /// **머물 이유를 하나씩 없애는 방식으로** 등을 떠민다. 규칙으로 "나가라"고 하지 않는다.
    /// </summary>
    public bool ShopOpen => HungerRule.ShopOpen(Stage);

    // 폭주 신호 래치. bool 둘이 아니라 "무장됐는가 + 어느 쪽인가"로 들고 있다.
    bool breakoutArmed;
    BreakoutSide breakoutSide;

    // 이미 한계에 붙어 있는가. 한계에 머무는 동안 매 프레임 재무장하는 것을 막는다 —
    // 클램프 때문에 값은 계속 −100 인데 신호만 무한히 쌓이면 소비 자체가 의미를 잃는다.
    bool atLimit;

    /// <summary>
    /// 시간이 흐른 만큼 축을 민다. 방향은 페이즈가 정한다 —
    /// 호출자(그 씬의 흐름 소유자)는 dt 만 알면 되고, "사냥이면 오른다"는 판정은 여기 있다.
    ///
    /// 사냥은 그 밤의 달이 정한 속도로 야성 쪽, 마을 체류는 <see cref="VillageDriftPerSecond"/> 로
    /// 굶주림 쪽. 그 밖의 페이즈(타이틀·달 공개·정산)와 페이즈 미상(null)에서는 멈춘다 —
    /// 축을 미는 것은 "사냥하고 있다"와 "마을에 머물고 있다" 둘뿐이기 때문이다.
    /// </summary>
    /// <param name="phase">지금 페이즈. null 이면 공용 게임 상태 없이 씬을 단독 재생하는 개발 상황.</param>
    /// <param name="dt">흐른 시간(초). 0 이하는 무시한다 — 일시정지·히트스톱이 축을 되감으면 안 된다.</param>
    /// <param name="wildRisePerSecond">그 밤의 달이 정한 야성 상승 속도. 사냥이 아니면 쓰이지 않는다.</param>
    public void Advance(RoundPhase? phase, float dt, float wildRisePerSecond)
    {
        if (dt <= 0f) return;
        if (phase == null) return;

        float delta;
        if (phase.Value == RoundPhase.Hunt) delta = wildRisePerSecond * dt;
        else if (phase.Value == RoundPhase.Village) delta = -VillageDriftPerSecond * dt;
        else return;

        Move(delta);
    }

    /// <summary>
    /// 처치 = 먹기. 굶주림이 조금 풀리고, 그 한 입만큼 체력이 돌아온다.
    ///
    /// **회복량은 먹기 전 단계로 정한다.** 굶주릴수록 한 입이 크다 —
    /// 이게 없으면 아사는 데드락이다. 약하고, 상점도 못 쓰고, 나가야 푸는데 나가면 죽는다.
    /// 첫 먹이로 숨통이 트여야 절망이 아니라 도박이 된다.
    /// </summary>
    /// <returns>이번 한 입의 회복량. 0 이면 회복 없음(포식 상태라 배가 부르다).</returns>
    public int OnKill()
    {
        int bite = HungerRule.Bite(Stage);

        // **먹기는 굶주림을 풀 뿐, 야성을 밀지 않는다.** 안전(0)을 넘어가지 않게 자른다.
        //
        // 부호와 무관하게 더하면 처치 수가 야성을 지배한다 — 웨이브 하나가 15초에 10~22마리인데
        // 같은 15초의 시간 기여는 달 속도 0.5 × 15 = 7.5 뿐이라, 한계 100 의 대부분을 처치가 민다.
        // 그러면 "달이 야성이 차오르는 속도를 정한다"가 이름만 남는다.
        // 야성을 올리는 것은 그 밤의 달 아래 머무는 시간이고, 처치는 굶주림 쪽만 건드린다.
        if (Value < 0f) Move(Math.Min(EatNudgePerKill, -Value));

        return bite;
    }

    /// <summary>
    /// 폭주 신호를 한 번 꺼내 간다. 이벤트가 아니라 **래치**인 것이 핵심이다.
    ///
    /// 폭주는 사냥터에서 임계를 넘고 소비는 마을에서 일어난다 — 이벤트로 쏘면
    /// 마을 씬이 아직 로드되지 않은 사이에 신호가 허공으로 사라진다.
    /// (<c>MoonEffects</c> 의 "구독만으로는 늦다"와 같은 사고다. 거기서는 달이 사라졌다.)
    /// 그래서 신호를 남겨두고, 소비자가 도착해서 가져갈 때까지 기다린다.
    /// </summary>
    /// <param name="side">폭주한 쪽. 반환값이 false 면 의미 없다.</param>
    /// <returns>가져갈 신호가 있었으면 true (그리고 신호는 소비되어 사라진다).</returns>
    public bool TryConsumeBreakout(out BreakoutSide side)
    {
        side = breakoutSide;
        if (!breakoutArmed) return false;

        breakoutArmed = false;
        return true;
    }

    /// <summary>
    /// 런이 끝나 새로 시작할 때 — 안전한 중앙으로 돌아오고 남은 신호도 버린다.
    /// 밤이 바뀔 때가 아니다. 축은 런 전체에 누적된다.
    /// </summary>
    public void Reset()
    {
        Value = 0f;
        breakoutArmed = false;
        atLimit = false;
    }

    /// <summary>축을 옮기고 양끝에서 자른 뒤, 이번에 한계를 새로 넘었으면 폭주를 무장한다.</summary>
    void Move(float delta)
    {
        Value += delta;

        if (Value >= WildLimit) Value = WildLimit;
        else if (Value <= StarveLimit) Value = StarveLimit;

        bool nowAtLimit = Value >= WildLimit || Value <= StarveLimit;

        // 한계에 "머무는" 동안이 아니라 "들어서는" 순간에만 무장한다.
        // 클램프 때문에 값이 −100 에 붙어 있는 동안은 계속 nowAtLimit 이라, 이 가드가 없으면
        // 매 프레임 재무장해서 소비자가 아무리 가져가도 신호가 마르지 않는다.
        if (nowAtLimit && !atLimit)
        {
            breakoutArmed = true;
            breakoutSide = Value >= WildLimit ? BreakoutSide.Wild : BreakoutSide.Starve;
        }
        atLimit = nowAtLimit;
    }
}
