using NUnit.Framework;

/// <summary>
/// 값 하나에 임계선 둘 (#117).
///
/// 축의 판정을 전부 순수 C# 으로 둔 이유가 여기서 드러난다 — 사냥에서 야성이 오르고
/// 마을에서 굶주림이 오르는 것, 단계마다 몸이 어떻게 나빠지는지, 한계를 넘었을 때 신호가
/// 소비자에게 닿는지를 씬 하나 열지 않고 검사할 수 있다.
///
/// 수치(-20/-50/-75, 0.85/0.70/0.50, 속도 0.5/1.0)는 전부 첫 조정 대상이다.
/// 밸런싱에서 바뀌면 이 테스트도 함께 바뀐다 — 여기 못 박은 것은 "값"이 아니라 "구조"다.
/// </summary>
[TestFixture]
public class WildAxisCoreTests
{
    WildAxisCore core;

    [SetUp]
    public void 새_런을_시작한다()
    {
        core = new WildAxisCore();
    }

    /// <summary>축을 그 위치까지 데려간다. 굶주림 쪽은 마을 체류로, 야성 쪽은 사냥으로.</summary>
    void 축을_여기까지(float target)
    {
        if (target > 0f) core.Advance(RoundPhase.Hunt, target, 1f);
        else if (target < 0f) core.Advance(RoundPhase.Village, -target / WildAxisCore.VillageDriftPerSecond, 0f);
    }

    [Test]
    public void 사냥_중에는_달이_정한_속도로_야성이_오른다()
    {
        // 속도의 주인은 달이다 — 코어는 상수로 들고 있지 않고 매번 받는다.
        core.Advance(RoundPhase.Hunt, 10f, 0.5f);

        Assert.That(core.Value, Is.EqualTo(5f).Within(0.001f));
    }

    [Test]
    public void 달이_빠르면_야성도_빨리_찬다()
    {
        // 보름달은 빨리, 초승달은 천천히. 같은 10초라도 달에 따라 도달점이 다르다.
        core.Advance(RoundPhase.Hunt, 10f, 3f);

        Assert.That(core.Value, Is.EqualTo(30f).Within(0.001f));
    }

    [Test]
    public void 마을에_머물면_야성이_내려가고_굶주림이_오른다()
    {
        // 마을 드리프트는 달과 무관하다 — 마을에 있는 동안은 하늘을 보지 않는다.
        // 그래서 달 속도로 100 을 넘겨줘도 결과가 달라지지 않아야 한다.
        core.Advance(RoundPhase.Village, 10f, 100f);

        Assert.That(core.Value, Is.EqualTo(-10f).Within(0.001f));
    }

    [TestCase(RoundPhase.Title)]
    [TestCase(RoundPhase.MoonReveal)]
    [TestCase(RoundPhase.ActionSelect)]
    [TestCase(RoundPhase.Reward)]
    public void 사냥도_마을도_아니면_축은_움직이지_않는다(RoundPhase phase)
    {
        // 축을 미는 것은 "사냥하고 있다"와 "마을에 머물고 있다" 둘뿐이다.
        // 정산 화면을 오래 켜뒀다고 굶주릴 이유가 없다.
        core.Advance(phase, 100f, 5f);

        Assert.That(core.Value, Is.EqualTo(0f));
    }

    [Test]
    public void 페이즈를_모르면_축은_움직이지_않는다()
    {
        // 공용 게임 상태 없이 씬을 단독 재생하는 개발 중 상황.
        // 씬 이름으로 짐작하지 않고 그냥 멈춘다 — 모르는 채로 미는 것보다 안 미는 게 낫다.
        core.Advance(null, 100f, 5f);

        Assert.That(core.Value, Is.EqualTo(0f));
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    public void dt가_0이하면_무시한다(float dt)
    {
        // 일시정지·히트스톱으로 dt 가 0 이 되는 프레임이 실제로 있다.
        // 음수 dt 로 축이 되감기면 시간을 되돌려 굶주림을 지울 수 있게 된다.
        core.Advance(RoundPhase.Village, dt, 1f);

        Assert.That(core.Value, Is.EqualTo(0f));
    }

    [Test]
    public void 처치는_굶주림을_내리고_먹기_전_단계로_회복량을_정한다()
    {
        축을_여기까지(-80f); // 아사

        int bite = core.OnKill();

        // 한 입을 먹으면 −79 로 올라가 아사를 벗어나기 직전이 되지만,
        // 회복량은 "먹기 전에 얼마나 굶었는가"로 정해진다. 순서를 뒤집으면
        // 경계에 걸친 한 마리에서 회복이 한 단계 작아진다.
        Assert.That(bite, Is.EqualTo(3));
        Assert.That(core.Value, Is.EqualTo(-79f).Within(0.001f));
    }

    [TestCase(0f, 0, TestName = "굶주릴수록_한_입이_크다_포식은_0")]
    [TestCase(-30f, 1, TestName = "굶주릴수록_한_입이_크다_허기는_1")]
    [TestCase(-60f, 2, TestName = "굶주릴수록_한_입이_크다_주림은_2")]
    [TestCase(-80f, 3, TestName = "굶주릴수록_한_입이_크다_아사는_3")]
    [TestCase(-100f, 3, TestName = "굶주릴수록_한_입이_크다_한계도_3")]
    public void 굶주릴수록_한_입이_크다(float start, int expectedBite)
    {
        // 이게 없으면 아사는 데드락이다 — 약하고, 상점도 못 쓰고, 나가야 푸는데 나가면 죽는다.
        // 첫 먹이로 숨통이 트여야 절망이 아니라 도박이 된다.
        축을_여기까지(start);

        Assert.That(core.OnKill(), Is.EqualTo(expectedBite));
    }

    [TestCase(50f, HungerStage.Sated, TestName = "단계_경계_야성_쪽은_포식이다")]
    [TestCase(0f, HungerStage.Sated, TestName = "단계_경계_안전은_포식이다")]
    [TestCase(-19.9f, HungerStage.Sated, TestName = "단계_경계_허기_직전은_포식이다")]
    [TestCase(-20f, HungerStage.Hungry, TestName = "단계_경계_20에서_허기다")]
    [TestCase(-49.9f, HungerStage.Hungry, TestName = "단계_경계_주림_직전은_허기다")]
    [TestCase(-50f, HungerStage.Famished, TestName = "단계_경계_50에서_주림이다")]
    [TestCase(-74.9f, HungerStage.Famished, TestName = "단계_경계_아사_직전은_주림이다")]
    [TestCase(-75f, HungerStage.Starving, TestName = "단계_경계_75에서_아사다")]
    [TestCase(-99.9f, HungerStage.Starving, TestName = "단계_경계_한계_직전은_아사다")]
    [TestCase(-100f, HungerStage.Limit, TestName = "단계_경계_100에서_한계다")]
    public void 굶주림_단계_경계_판정(float value, HungerStage expected)
    {
        // 경계는 "이상"이 아니라 "초과"로 갈린다 — 딱 −20 은 아직 포식이 아니라 이미 허기다.
        // 부등호 하나가 뒤집히면 단계 전체가 한 칸씩 밀리므로 양쪽을 다 못 박는다.
        축을_여기까지(value);

        Assert.That(core.Stage, Is.EqualTo(expected));
    }

    [TestCase(0f, 1f, 0, TestName = "포식은_페널티가_없다")]
    [TestCase(-30f, 0.85f, 0, TestName = "허기는_공격력만_깎인다")]
    [TestCase(-60f, 0.70f, 1, TestName = "주림은_최대_체력도_1_깎인다")]
    [TestCase(-80f, 0.50f, 2, TestName = "아사는_최대_체력이_2_깎인다")]
    [TestCase(-100f, 0.50f, 2, TestName = "한계는_아사와_같은_페널티다")]
    public void 단계별_공격_배율과_최대체력_페널티(float value, float expectedMult, int expectedHpPenalty)
    {
        축을_여기까지(value);

        Assert.That(core.AttackMultiplier, Is.EqualTo(expectedMult).Within(0.0001f));
        Assert.That(core.MaxHpPenalty, Is.EqualTo(expectedHpPenalty));
    }

    [TestCase(0f, true, TestName = "상점_포식에서_열린다")]
    [TestCase(-30f, true, TestName = "상점_허기에서_열린다")]
    [TestCase(-60f, true, TestName = "상점_주림에서도_열린다")]
    [TestCase(-80f, false, TestName = "상점_아사에서_닫힌다")]
    [TestCase(-100f, false, TestName = "상점_한계에서_닫힌다")]
    public void 아사에서_상점이_닫힌다(float value, bool expectedOpen)
    {
        // 마을에 남는 유일한 이유가 상점이다. 굶주릴수록 그게 닫혀 등이 떠밀린다 —
        // 규칙으로 "나가라"고 하지 않고 머물 이유를 없앤다.
        축을_여기까지(value);

        Assert.That(core.ShopOpen, Is.EqualTo(expectedOpen));
    }

    [Test]
    public void 야성_한계를_넘으면_폭주가_무장된다()
    {
        core.Advance(RoundPhase.Hunt, 100f, 2f); // +200 → 클램프 +100

        Assert.That(core.TryConsumeBreakout(out BreakoutSide side), Is.True);
        Assert.That(side, Is.EqualTo(BreakoutSide.Wild));
    }

    [Test]
    public void 굶주림_한계를_넘으면_폭주가_무장된다()
    {
        // 양끝 모두 폭주지만 원인이 반대다 — 소비자(#12)는 어느 쪽인지로 연출을 가른다.
        축을_여기까지(-150f); // 클램프 −100

        Assert.That(core.TryConsumeBreakout(out BreakoutSide side), Is.True);
        Assert.That(side, Is.EqualTo(BreakoutSide.Starve));
    }

    [Test]
    public void 한계에_닿기_전에는_무장되지_않는다()
    {
        축을_여기까지(-99.9f);

        Assert.That(core.TryConsumeBreakout(out _), Is.False);
    }

    [Test]
    public void 폭주_신호는_소비자가_올_때까지_남는다()
    {
        // 래치의 존재 이유다. 폭주는 사냥터에서 넘고 소비는 마을에서 일어난다 —
        // 이벤트로 쏘면 마을 씬이 로드되기 전에 신호가 허공으로 사라진다.
        축을_여기까지(-100f);

        for (int i = 0; i < 100; i++) core.Advance(RoundPhase.Village, 0.016f, 1f);

        Assert.That(core.TryConsumeBreakout(out BreakoutSide side), Is.True);
        Assert.That(side, Is.EqualTo(BreakoutSide.Starve));
    }

    [Test]
    public void 폭주_신호는_한_번만_소비된다()
    {
        축을_여기까지(-100f);

        Assert.That(core.TryConsumeBreakout(out _), Is.True);
        Assert.That(core.TryConsumeBreakout(out _), Is.False);
    }

    [Test]
    public void 한계에_머무는_동안_재무장하지_않는다()
    {
        // 클램프 때문에 값은 계속 −100 이다. 이 가드가 없으면 매 프레임 재무장해서
        // 소비자가 아무리 가져가도 신호가 마르지 않고 폭주가 무한히 반복된다.
        축을_여기까지(-100f);
        core.TryConsumeBreakout(out _);

        for (int i = 0; i < 100; i++) core.Advance(RoundPhase.Village, 0.016f, 1f);

        Assert.That(core.TryConsumeBreakout(out _), Is.False);
    }

    [Test]
    public void 안쪽으로_돌아갔다_다시_넘으면_재무장한다()
    {
        축을_여기까지(-100f);
        core.TryConsumeBreakout(out _);

        core.Advance(RoundPhase.Hunt, 20f, 1f); // −80 — 한계 안쪽으로
        축을_여기까지(-100f);               // 다시 한계로

        Assert.That(core.TryConsumeBreakout(out BreakoutSide side), Is.True);
        Assert.That(side, Is.EqualTo(BreakoutSide.Starve));
    }

    [Test]
    public void 값은_양끝에서_클램프된다()
    {
        core.Advance(RoundPhase.Hunt, 1000f, 10f);
        Assert.That(core.Value, Is.EqualTo(WildAxisCore.WildLimit));

        core.Advance(RoundPhase.Village, 1000f, 0f);
        Assert.That(core.Value, Is.EqualTo(WildAxisCore.StarveLimit));
    }

    [Test]
    public void 밤을_넘어_누적된다()
    {
        // 축은 런 전체에 누적된다 — 밤이 바뀐다고 저절로 0 으로 돌아가지 않는다.
        // 자동 리셋이 있으면 "어제 사냥을 나갔다"가 오늘에 아무 영향을 주지 않아
        // 매 밤 같은 판단이 반복되고, 축이 만드는 긴장이 통째로 사라진다.
        core.Advance(RoundPhase.Hunt, 40f, 1f);   // 그 밤: +40
        core.Advance(RoundPhase.Village, 25f, 1f); // 다음 낮: −25

        Assert.That(core.Value, Is.EqualTo(15f).Within(0.001f));
    }

    [Test]
    public void 리셋하면_안전_중앙으로_돌아오고_래치도_풀린다()
    {
        축을_여기까지(-100f);

        core.Reset();

        Assert.That(core.Value, Is.EqualTo(0f));
        Assert.That(core.Stage, Is.EqualTo(HungerStage.Sated));
        Assert.That(core.TryConsumeBreakout(out _), Is.False);
    }

    [Test]
    public void 리셋한_뒤에도_다시_무장할_수_있다()
    {
        // Reset 이 한계 플래그를 함께 풀지 않으면, 새 런에서 처음 한계를 넘었을 때
        // "이미 한계에 있었다"로 판단해 폭주가 영영 오지 않는다.
        축을_여기까지(-100f);
        core.Reset();

        축을_여기까지(-100f);

        Assert.That(core.TryConsumeBreakout(out _), Is.True);
    }
}
