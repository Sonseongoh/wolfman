using NUnit.Framework;

/// <summary>
/// 페이즈 하나가 플레이어의 형태를 결정한다 (#53).
///
/// 이 규칙이 순수 함수라서 여기서 검사할 수 있다 — 씬을 열지 않고도
/// "마을에 들어가면 인간, 사냥에 나가면 늑대인간"이 지켜지는지 알 수 있다.
/// </summary>
[TestFixture]
public class PlayerFormRuleTests
{
    [Test]
    public void 마을_페이즈면_인간이다()
    {
        Assert.That(PlayerFormRule.For(RoundPhase.Village), Is.EqualTo(PlayerForm.Human));
    }

    [TestCase(RoundPhase.MoonReveal)]
    [TestCase(RoundPhase.ActionSelect)]
    [TestCase(RoundPhase.Hunt)]
    [TestCase(RoundPhase.Reward)]
    public void 마을이_아닌_페이즈는_모두_늑대인간이다(RoundPhase phase)
    {
        // 규칙을 "Hunt 면 늑대"로 뒤집어 쓰면 여기서 잡힌다.
        // 실제로 사냥 중 페이즈는 Hunt 가 아니라 라운드 시작이 남긴 MoonReveal 이다.
        Assert.That(PlayerFormRule.For(phase), Is.EqualTo(PlayerForm.Werewolf),
            $"{phase} 는 마을이 아니므로 늑대인간이어야 한다");
    }

    [Test]
    public void 페이즈를_모르면_인간이다()
    {
        // 공용 게임 상태 없이 마을 씬을 단독 재생하는 개발 중 상황.
        // 씬 이름으로 갈라 판단하지 않고, "페이즈 미상일 때의 기본 형태"로 답한다.
        Assert.That(PlayerFormRule.For(null), Is.EqualTo(PlayerForm.Human));
    }
}
