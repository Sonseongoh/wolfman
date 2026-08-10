using NUnit.Framework;

/// <summary>
/// 라운드 한 바퀴가 어떻게 맞물리는가 (#78).
///
/// 이 규칙들이 조건문으로 흩어져 있던 동안 두 가지가 오래 눈에 띄지 않았다 —
/// 사냥으로 끝낸 라운드가 정산되지 않았고, 라운드가 한 바퀴에 두 번 시작됐다.
/// 여기가 그 감시탑이다.
/// </summary>
[TestFixture]
public class RoundFlowRuleTests
{
    [TestCase(RoundPhase.Hunt)]
    [TestCase(RoundPhase.Village)]
    public void 라운드를_치르고_온_페이즈는_정산한다(RoundPhase phase)
    {
        Assert.That(RoundFlowRule.ShouldSettle(phase), Is.True);
    }

    /// <summary>
    /// 사냥이 빠져 있던 것이 #78 의 정체다. 사냥은 임시 골드를 버는 유일한 경로인데
    /// 정산 대상이 아니어서, 마을을 한 번 다녀오기 전까지 번 골드가 금고로 넘어가지
    /// 않았고 그 전에 죽으면 전부 사라졌다.
    /// </summary>
    [Test]
    public void 사냥은_반드시_정산_대상이다()
    {
        Assert.That(RoundFlowRule.ShouldSettle(RoundPhase.Hunt), Is.True);
    }

    [TestCase(RoundPhase.Title)]
    [TestCase(RoundPhase.MoonReveal)]
    [TestCase(RoundPhase.ActionSelect)]
    [TestCase(RoundPhase.Reward)]
    public void 아직_라운드를_치르지_않은_페이즈는_정산하지_않는다(RoundPhase phase)
    {
        Assert.That(RoundFlowRule.ShouldSettle(phase), Is.False);
    }

    [Test]
    public void 라운드가_하나도_시작되지_않았으면_시작이_필요하다()
    {
        Assert.That(RoundFlowRule.NeedsRoundStart(0), Is.True);
    }

    /// <summary>
    /// 이미 시작된 라운드를 또 시작하면 달이 다시 뽑히고 라운드 번호가 건너뛴다.
    /// 사냥 씬은 웨이브마다 이 판정을 지나므로 첫 웨이브 이후로는 반드시 막혀야 한다.
    /// </summary>
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(37)]
    public void 이미_시작된_라운드는_다시_시작하지_않는다(int roundNumber)
    {
        Assert.That(RoundFlowRule.NeedsRoundStart(roundNumber), Is.False);
    }
}
