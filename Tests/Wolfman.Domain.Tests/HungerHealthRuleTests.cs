using NUnit.Framework;

/// <summary>
/// 굶주림이 최대 체력을 눌렀다 풀 때 현재 체력이 어떻게 되는가 (#117).
///
/// 원래는 <c>PlayerHealth.Update</c> 가 넘치는 체력을 그냥 잘라내기만 했다.
/// 그러면 단계가 오르내릴 때마다 맞지도 않은 체력이 계단식으로 사라진다 —
/// 마을과 사냥을 오갈수록 아무 이유 없이 약해지는 래칫이다.
/// </summary>
[TestFixture]
public class HungerHealthRuleTests
{
    [Test]
    public void 최대치_안이면_아무것도_하지_않는다()
    {
        var r = HungerHealthRule.Settle(hp: 3, held: 0, cap: 5);

        Assert.That(r.Hp, Is.EqualTo(3));
        Assert.That(r.Held, Is.EqualTo(0));
    }

    [Test]
    public void 최대치가_내려가면_넘치는_만큼_눌러둔다()
    {
        // 잃은 게 아니라 맡아둔 것이다.
        var r = HungerHealthRule.Settle(hp: 5, held: 0, cap: 3);

        Assert.That(r.Hp, Is.EqualTo(3));
        Assert.That(r.Held, Is.EqualTo(2));
    }

    [Test]
    public void 굶주림이_풀리면_눌러둔_만큼_돌려준다()
    {
        var r = HungerHealthRule.Settle(hp: 3, held: 2, cap: 5);

        Assert.That(r.Hp, Is.EqualTo(5));
        Assert.That(r.Held, Is.EqualTo(0));
    }

    [Test]
    public void 맞아서_잃은_것은_돌려주지_않는다()
    {
        // 5/5 → 아사로 3/3(2 맡김) → 1 맞아 2/3 → 굶주림 풀림.
        // 돌아갈 곳은 4 다. 5 로 돌려주면 굶었다 풀리는 것이 공짜 회복이 된다.
        var r = HungerHealthRule.Settle(hp: 2, held: 2, cap: 5);

        Assert.That(r.Hp, Is.EqualTo(4));
        Assert.That(r.Held, Is.EqualTo(0));
    }

    [Test]
    public void 한_번에_다_못_돌려주면_남겨둔다()
    {
        // 주림(−1)까지만 풀렸다면 아직 1 은 눌린 채다.
        var r = HungerHealthRule.Settle(hp: 3, held: 2, cap: 4);

        Assert.That(r.Hp, Is.EqualTo(4));
        Assert.That(r.Held, Is.EqualTo(1));
    }

    [Test]
    public void 오르내림을_반복해도_체력이_새지_않는다()
    {
        // 래칫이 있었을 때 정확히 여기서 깎여 나갔다.
        int hp = 5, held = 0;

        for (int i = 0; i < 20; i++)
        {
            var down = HungerHealthRule.Settle(hp, held, 3); // 아사
            hp = down.Hp; held = down.Held;

            var up = HungerHealthRule.Settle(hp, held, 5);   // 포식
            hp = up.Hp; held = up.Held;
        }

        Assert.That(hp, Is.EqualTo(5), "맞지도 않았는데 체력이 줄었다");
        Assert.That(held, Is.EqualTo(0));
    }

    [Test]
    public void 맡아둔_양은_음수가_되지_않는다()
    {
        var r = HungerHealthRule.Settle(hp: 3, held: -5, cap: 5);

        Assert.That(r.Held, Is.EqualTo(0));
        Assert.That(r.Hp, Is.EqualTo(3));
    }
}
